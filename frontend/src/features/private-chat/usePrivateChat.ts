import {
  useInfiniteQuery, useMutation, useQuery, useQueryClient, type InfiniteData,
} from "@tanstack/react-query";
import { useCallback } from "react";
import { keys } from "../../app/queryKeys";
import { useAuth } from "../../app/providers";
import { appendToNewestPage, patchInPages } from "../../lib/paging";
import { authApi, blocksApi, conversationsApi } from "../../services/api";
import { invokeHub } from "../../services/realtime/connection";
import { HubEvent } from "../../services/realtime/events";
import type {
  ConversationUpdated, MessageDeleted, PrivateMessageRead, ReactionsChanged,
} from "../../services/realtime/events";
import {
  useHubConnection, useHubEvent, useHubGroup, useHubReconnect,
} from "../../services/realtime/hooks";
import type { Conversation, CursorPage, DirectMessage } from "../../types/api";

/**
 * Direct messages.
 *
 * Every read here is scoped to the caller by the server: `GET /api/conversations` returns
 * only the caller's own, and opening one by id checks participation first. That is the
 * property that matters most in this feature — a conversation id in the URL is not a
 * capability, so editing it produces a 403 rather than someone else's private messages.
 */

const PAGE_SIZE = 50;

type MessagePages = InfiniteData<CursorPage<DirectMessage>, string | undefined>;

// ── Conversations ─────────────────────────────────────────────────────────────

export function useConversations() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.conversations.list(),
    queryFn: () => conversationsApi.list(),
    enabled: isAuthenticated,
  });
}

export function useConversation(conversationId: string | undefined) {
  return useQuery({
    queryKey: keys.conversations.detail(conversationId ?? ""),
    queryFn: () => conversationsApi.byId(conversationId!),
    enabled: Boolean(conversationId),
  });
}

export function useDirectMessages(conversationId: string | undefined) {
  return useInfiniteQuery({
    queryKey: keys.conversations.messages(conversationId ?? ""),
    queryFn: ({ pageParam }) =>
      conversationsApi.history(conversationId!, pageParam, PAGE_SIZE),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    enabled: Boolean(conversationId),
    staleTime: 5 * 60_000,
  });
}

/** The people directory, for starting a new conversation. Anonymous names only. */
export function useDirectory() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.directory,
    queryFn: () => authApi.directory(),
    enabled: isAuthenticated,
    staleTime: 5 * 60_000,
  });
}

export function useBlockedUsers() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.blocks,
    queryFn: () => blocksApi.list(),
    enabled: isAuthenticated,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function usePrivateChatMutations(conversationId?: string) {
  const queryClient = useQueryClient();

  const patchMessage = useCallback(
    (messageId: string, update: (message: DirectMessage) => DirectMessage) => {
      if (!conversationId) return;

      queryClient.setQueryData<MessagePages>(
        keys.conversations.messages(conversationId),
        (current) => patchInPages(current, messageId, update) as MessagePages | undefined,
      );
    },
    [queryClient, conversationId],
  );

  return {
    /** Idempotent server-side: the same pair always resolves to the same conversation. */
    start: useMutation({
      mutationFn: (otherUserId: string) => conversationsApi.start(otherUserId),
      onSuccess: (conversation) => {
        queryClient.setQueryData<Conversation>(
          keys.conversations.detail(conversation.id),
          conversation,
        );
        void queryClient.invalidateQueries({ queryKey: keys.conversations.list() });
      },
    }),

    send: useMutation({
      mutationFn: (payload: { content: string; replyToMessageId?: string }) =>
        conversationsApi.send(conversationId!, payload),
      onSuccess: (message) => {
        queryClient.setQueryData<MessagePages>(
          keys.conversations.messages(message.conversationId),
          (current) => appendToNewestPage(current, message) as MessagePages | undefined,
        );
      },
    }),

    edit: useMutation({
      mutationFn: ({ messageId, content }: { messageId: string; content: string }) =>
        conversationsApi.edit(messageId, content),
      onSuccess: (message) => patchMessage(message.id, () => message),
    }),

    remove: useMutation({
      mutationFn: (messageId: string) => conversationsApi.remove(messageId),
    }),

    toggleReaction: useMutation({
      mutationFn: ({ messageId, emoji }: { messageId: string; emoji: string }) =>
        conversationsApi.toggleReaction(messageId, emoji),
      onSuccess: (message) => patchMessage(message.id, () => message),
    }),

    block: useMutation({
      mutationFn: (userId: string) => blocksApi.block(userId),
      onSuccess: () => {
        void queryClient.invalidateQueries({ queryKey: keys.blocks });
        void queryClient.invalidateQueries({ queryKey: keys.conversations.all });
      },
    }),

    unblock: useMutation({
      mutationFn: (userId: string) => blocksApi.unblock(userId),
      onSuccess: () => {
        void queryClient.invalidateQueries({ queryKey: keys.blocks });
        void queryClient.invalidateQueries({ queryKey: keys.conversations.all });
      },
    }),
  };
}

// ── Realtime ──────────────────────────────────────────────────────────────────

/**
 * Binds the private-chat hub to the cache.
 *
 * Unlike room chat, this hub targets individual users rather than groups, so each
 * participant receives a payload built for them and `isMine` is already correct. Joining
 * the conversation group is still required for typing indicators, which are group events.
 */
export function usePrivateChatRealtime(conversationId: string | undefined) {
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();

  const status = useHubConnection("privateChat", isAuthenticated);
  useHubGroup("privateChat", conversationId, isAuthenticated);

  useHubEvent(
    "privateChat",
    HubEvent.PrivateMessageReceived,
    (message: DirectMessage) => {
      queryClient.setQueryData<MessagePages>(
        keys.conversations.messages(message.conversationId),
        (current) => appendToNewestPage(current, message) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "privateChat",
    HubEvent.MessageEdited,
    (message: DirectMessage) => {
      queryClient.setQueryData<MessagePages>(
        keys.conversations.messages(message.conversationId),
        (current) => patchInPages(current, message.id, () => message) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "privateChat",
    HubEvent.MessageDeleted,
    (event: MessageDeleted) => {
      if (!event.conversationId) return;

      queryClient.setQueryData<MessagePages>(
        keys.conversations.messages(event.conversationId),
        (current) =>
          patchInPages(current, event.messageId, (item) => ({
            ...item,
            content: "",
            deletedBy: event.deletedBy,
            deletedAt: event.deletedAt,
          })) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "privateChat",
    HubEvent.ReactionsChanged,
    (event: ReactionsChanged) => {
      if (!event.conversationId) return;

      queryClient.setQueryData<MessagePages>(
        keys.conversations.messages(event.conversationId),
        (current) =>
          patchInPages(current, event.messageId, (item) => ({
            ...item,
            reactions: event.reactions,
          })) as MessagePages | undefined,
      );
    },
    isAuthenticated,
  );

  // The recipient read our messages: flip the ticks without a refetch.
  useHubEvent(
    "privateChat",
    HubEvent.PrivateMessageRead,
    (event: PrivateMessageRead) => {
      queryClient.setQueryData<MessagePages>(
        keys.conversations.messages(event.conversationId),
        (current) => {
          let next = current;
          for (const messageId of event.messageIds) {
            next = patchInPages(next, messageId, (item) => ({
              ...item,
              readAt: item.readAt ?? event.readAt,
            })) as MessagePages | undefined;
          }
          return next;
        },
      );
    },
    isAuthenticated,
  );

  // Per-user sidebar update carrying the server's own unread count.
  useHubEvent(
    "privateChat",
    HubEvent.ConversationUpdated,
    (event: ConversationUpdated) => {
      queryClient.setQueryData<Conversation[]>(keys.conversations.list(), (conversations) => {
        if (!conversations) return conversations;

        const known = conversations.some((c) => c.id === event.conversationId);

        // A first message from someone new has no row to update yet.
        if (!known) {
          void queryClient.invalidateQueries({ queryKey: keys.conversations.list() });
          return conversations;
        }

        return conversations.map((conversation) =>
          conversation.id === event.conversationId
            ? {
                ...conversation,
                unreadCount: event.unreadCount,
                lastMessage: event.lastMessage,
              }
            : conversation,
        );
      });
    },
    isAuthenticated,
  );

  useHubReconnect(
    "privateChat",
    () => {
      void queryClient.invalidateQueries({ queryKey: keys.conversations.all });
    },
    isAuthenticated,
  );

  return status;
}

/** Hub-only commands. Typing is a group event; read marking also fans out a receipt. */
export const privateChatCommands = {
  startTyping: (conversationId: string) =>
    invokeHub("privateChat", "StartTyping", conversationId).catch(() => undefined),
  stopTyping: (conversationId: string) =>
    invokeHub("privateChat", "StopTyping", conversationId).catch(() => undefined),
  markRead: (conversationId: string) =>
    invokeHub("privateChat", "MarkRead", conversationId).catch(() => undefined),
};
