import {
  useInfiniteQuery, useMutation, useQuery, useQueryClient, type InfiniteData,
} from "@tanstack/react-query";
import { useCallback } from "react";
import { keys } from "../../app/queryKeys";
import { useAuth } from "../../app/providers";
import { filesApi, messagesApi, roomsApi } from "../../services/api";
import { invokeHub } from "../../services/realtime/connection";
import { HubEvent } from "../../services/realtime/events";
import type {
  MessageDeleted, ReactionsChanged, RoomPresenceChanged, RoomUpdated,
} from "../../services/realtime/events";
import {
  useHubConnection, useHubEvent, useHubGroup, useHubReconnect,
} from "../../services/realtime/hooks";
import { appendToNewestPage, patchInPages } from "../../lib/paging";
import type { CursorPage, Message, Room, RoomMember } from "../../types/api";

/**
 * Room chat data.
 *
 * Transport split, deliberately:
 *
 *  - **REST** for every mutation (send, edit, delete, react). The service broadcasts the
 *    same SignalR events either way, so nothing is lost, and REST gives a real status
 *    code — which matters most for a moderation rejection, where the 422 body carries
 *    the category and reason the composer needs to show.
 *  - **The hub** for presence, typing, group membership and read markers. These are
 *    connection-scoped and have no REST equivalent.
 *
 * The old client did the opposite: it sent through the hub and got back an opaque
 * "An unexpected error occurred in the hub method" for every rejection.
 */

const PAGE_SIZE = 40;

type MessagePages = InfiniteData<CursorPage<Message>, string | undefined>;

// ── Rooms ─────────────────────────────────────────────────────────────────────

export function useRooms() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.rooms.list(),
    queryFn: () => roomsApi.list(),
    enabled: isAuthenticated,
  });
}

export function useRoom(roomId: string | undefined) {
  return useQuery({
    queryKey: keys.rooms.detail(roomId ?? ""),
    queryFn: () => roomsApi.byId(roomId!),
    enabled: Boolean(roomId),
  });
}

/** Room members with their online state. Refreshed live by presence events. */
export function useRoomMembers(roomId: string | undefined) {
  return useQuery({
    queryKey: keys.rooms.members(roomId ?? ""),
    queryFn: () => roomsApi.members(roomId!),
    enabled: Boolean(roomId),
  });
}

// ── History ───────────────────────────────────────────────────────────────────

/**
 * Message history, newest page first.
 *
 * Each page is ordered oldest→newest internally and `pages[0]` is the newest block, so
 * chronological order is `[...pages].reverse().flatMap(p => p.items)`. Cursor paging
 * rather than offsets: with offsets, every message that arrives mid-scroll shifts the
 * window and the user sees the same message twice or misses one entirely.
 */
export function useMessages(roomId: string | undefined) {
  return useInfiniteQuery({
    queryKey: keys.rooms.messages(roomId ?? ""),
    queryFn: ({ pageParam }) => roomsApi.history(roomId!, pageParam, PAGE_SIZE),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    enabled: Boolean(roomId),
    // History is immutable except through events we already handle, so refetching on
    // mount would only discard the user's scroll position.
    staleTime: 5 * 60_000,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useChatMutations(roomId: string | undefined) {
  const queryClient = useQueryClient();

  /** Replaces one message wherever it sits in the paged cache. */
  const patchMessage = useCallback(
    (messageId: string, update: (message: Message) => Message) => {
      if (!roomId) return;

      queryClient.setQueryData<MessagePages>(keys.rooms.messages(roomId), (current) =>
        patchInPages(current, messageId, update) as MessagePages | undefined,
      );
    },
    [queryClient, roomId],
  );

  const send = useMutation({
    mutationFn: (payload: {
      content: string;
      replyToMessageId?: string;
      attachmentIds?: string[];
    }) => roomsApi.send(roomId!, payload),

    onSuccess: (message) => {
      // The sender's authoritative copy — the group broadcast that follows carries
      // isMine=false, so this must win. appendMessage dedupes on id.
      appendMessage(queryClient, message.roomId, message);
    },
  });

  const edit = useMutation({
    mutationFn: ({ messageId, content }: { messageId: string; content: string }) =>
      messagesApi.edit(messageId, content),
    onSuccess: (message) => patchMessage(message.id, () => message),
  });

  const remove = useMutation({
    mutationFn: (messageId: string) => messagesApi.remove(messageId),
    // No optimistic patch: the MessageDeleted broadcast arrives for the deleter too and
    // carries the authoritative deletedBy, which decides which placeholder is shown.
  });

  const toggleReaction = useMutation({
    mutationFn: ({ messageId, emoji }: { messageId: string; emoji: string }) =>
      messagesApi.toggleReaction(messageId, emoji),
    onSuccess: (message) => patchMessage(message.id, () => message),
  });

  const upload = useMutation({
    mutationFn: ({ file, onProgress }: { file: File; onProgress?: (n: number) => void }) =>
      filesApi.upload(file, onProgress),
  });

  return { send, edit, remove, toggleReaction, upload, patchMessage };
}

export function useRoomMembership() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: keys.rooms.all });
  };

  return {
    join: useMutation({ mutationFn: (roomId: string) => roomsApi.join(roomId), onSuccess: invalidate }),
    leave: useMutation({ mutationFn: (roomId: string) => roomsApi.leave(roomId), onSuccess: invalidate }),
  };
}

// ── Cache helpers ─────────────────────────────────────────────────────────────

/**
 * Appends a message to the newest page, ignoring one already present.
 *
 * The dedupe is what makes the sender's own message correct: the REST response and the
 * group broadcast both deliver it, and only the REST copy carries `isMine: true`.
 */
function appendMessage(
  queryClient: ReturnType<typeof useQueryClient>,
  roomId: string,
  message: Message,
) {
  queryClient.setQueryData<MessagePages>(
    keys.rooms.messages(roomId),
    (current) => appendToNewestPage(current, message) as MessagePages | undefined,
  );
}

// ── Realtime ──────────────────────────────────────────────────────────────────

/**
 * Binds the chat hub to the cache for the open room.
 *
 * Group membership follows the open room, so a user in three tabs receives each room's
 * traffic only in the tab showing it. Reconnects refetch, because events that arrived
 * while the socket was down are simply gone.
 */
export function useChatRealtime(roomId: string | undefined) {
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();

  const status = useHubConnection("chat", isAuthenticated);
  useHubGroup("chat", roomId, isAuthenticated);

  useHubEvent(
    "chat",
    HubEvent.MessageReceived,
    (message: Message) => {
      appendMessage(queryClient, message.roomId, message);

      // Keep the sidebar preview honest even for a room that is not open.
      void queryClient.invalidateQueries({ queryKey: keys.rooms.list() });
    },
    isAuthenticated,
  );

  useHubEvent(
    "chat",
    HubEvent.MessageEdited,
    (message: Message) => {
      queryClient.setQueryData<MessagePages>(
        keys.rooms.messages(message.roomId),
        (current) =>
          patchInPages(current, message.id, (item) =>
            // The broadcast is viewer-neutral, so isMine would be false even for the
            // author. Preserve the flag the client already established.
            ({ ...message, isMine: item.isMine }),
          ) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "chat",
    HubEvent.MessageDeleted,
    (event: MessageDeleted) => {
      if (!event.roomId) return;

      queryClient.setQueryData<MessagePages>(
        keys.rooms.messages(event.roomId),
        (current) =>
          patchInPages(current, event.messageId, (item) => ({
            // The message stays in place as a tombstone: removing the row would make
            // the replies above and below it read as a conversation with itself.
            ...item,
            content: "",
            attachments: [],
            deletedBy: event.deletedBy,
            deletedAt: event.deletedAt,
          })) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "chat",
    HubEvent.ReactionsChanged,
    (event: ReactionsChanged) => {
      if (!event.roomId) return;

      queryClient.setQueryData<MessagePages>(
        keys.rooms.messages(event.roomId),
        (current) =>
          patchInPages(current, event.messageId, (item) => ({
            ...item,
            // The server sends the resulting list, not a delta to apply — so a missed
            // event self-corrects on the next one instead of drifting.
            reactions: event.reactions,
          })) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "chat",
    HubEvent.RoomPresenceChanged,
    (event: RoomPresenceChanged) => {
      queryClient.setQueryData<RoomMember[]>(keys.rooms.members(event.roomId), event.members);
    },
    isAuthenticated,
  );

  // Per-user sidebar update carrying the server's own unread count — never derived here.
  useHubEvent(
    "chat",
    HubEvent.RoomUpdated,
    (event: RoomUpdated) => {
      queryClient.setQueryData<Room[]>(keys.rooms.list(), (rooms) =>
        rooms?.map((room) =>
          room.id === event.roomId
            ? { ...room, unreadCount: event.unreadCount, lastMessage: event.lastMessage }
            : room,
        ),
      );
    },
    isAuthenticated,
  );

  useHubReconnect(
    "chat",
    () => {
      void queryClient.invalidateQueries({ queryKey: keys.rooms.all });
    },
    isAuthenticated,
  );

  return status;
}

// ── Hub commands ──────────────────────────────────────────────────────────────

/** Typing and read markers. Failures are non-fatal — these are cosmetic signals. */
export const chatCommands = {
  startTyping: (roomId: string) =>
    invokeHub("chat", "StartTyping", roomId).catch(() => undefined),
  stopTyping: (roomId: string) => invokeHub("chat", "StopTyping", roomId).catch(() => undefined),
  markRead: (roomId: string) => invokeHub("chat", "MarkRead", roomId).catch(() => undefined),
};
