import { ChevronLeft, Hash, Users } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { Link } from "react-router-dom";
import {
  ConnectionBanner, EmptyState, ErrorState, MessageSkeleton,
} from "../../components/feedback";
import { Composer, type ReplyTarget } from "../../components/message/Composer";
import { DayDivider, MessageScroller } from "../../components/message/MessageScroller";
import { Badge, Button } from "../../components/ui";
import { paths } from "../../config";
import { useTypingIndicator, useTypingSignal } from "../../services/realtime/hooks";
import { errorMessage } from "../../services/api";
import type { Message } from "../../types/api";
import { ReportDialog } from "../moderation/ReportDialog";
import { MessageItem } from "./MessageItem";
import { MembersPanel } from "./MembersPanel";
import { TypingLine } from "./TypingLine";
import { flattenPages } from "../../lib/paging";
import { groupMessages, roomAccent } from "../../lib/messages";
import {
  chatCommands, useChatMutations, useMessages, useRoom, useRoomMembers,
} from "./useChat";
import type { ConnectionStatus } from "../../services/realtime/connection";

/** One open channel: header, message log, composer. */
export function RoomView({
  roomId,
  connection,
}: {
  roomId: string;
  connection: ConnectionStatus;
}) {
  const room = useRoom(roomId);
  const members = useRoomMembers(roomId);
  const messages = useMessages(roomId);
  const { send, edit, remove, toggleReaction, upload } = useChatMutations(roomId);

  const [replyTo, setReplyTo] = useState<ReplyTarget | null>(null);
  const [reporting, setReporting] = useState<Message | null>(null);
  const [highlighted, setHighlighted] = useState<string | null>(null);
  const [showMembers, setShowMembers] = useState(false);

  const typing = useTypingIndicator("chat", roomId, "roomId");

  const { notifyTyping, notifyStopped } = useTypingSignal(
    useCallback(
      (isTyping: boolean) => {
        if (isTyping) void chatCommands.startTyping(roomId);
        else void chatCommands.stopTyping(roomId);
      },
      [roomId],
    ),
  );

  const items = flattenPages(messages.data);

  // Opening a room clears its unread count. Fires once per room, and again only when
  // new messages arrive while the room is open.
  useEffect(() => {
    if (!roomId || items.length === 0) return;

    const timer = setTimeout(() => void chatCommands.markRead(roomId), 400);
    return () => clearTimeout(timer);
  }, [roomId, items.length]);

  // No per-room reset effect is needed: ChatPage mounts this with key={roomId}, so a
  // room change remounts the component and every piece of draft state starts fresh.

  const jumpTo = useCallback((messageId: string) => {
    const element = document.getElementById(`message-${messageId}`);

    if (!element) {
      // The parent is in an older page that has not been loaded yet.
      toast("Scroll up to load that message.");
      return;
    }

    element.scrollIntoView({ behavior: "smooth", block: "center" });
    setHighlighted(messageId);
    setTimeout(() => setHighlighted(null), 1_600);
  }, []);

  if (room.isLoading) {
    return (
      <div className="flex-1 flex flex-col">
        <div className="h-[var(--zc-header-height)] border-b border-line bg-surface shrink-0" />
        <MessageSkeleton />
      </div>
    );
  }

  // A 403 here is the branch rule doing its job, and the server's message names the
  // office — which is exactly what the user needs to understand why.
  if (room.error) {
    return (
      <div className="flex-1 flex flex-col">
        <MobileBackBar />
        <ErrorState error={room.error} onRetry={() => void room.refetch()} className="flex-1" />
      </div>
    );
  }

  if (!room.data) return null;

  const online = members.data?.filter((m) => m.isOnline).length ?? 0;
  const archived = room.data.isArchived;

  return (
    <div className="flex-1 flex min-w-0 min-h-0">
      <div className="flex-1 flex flex-col min-w-0 min-h-0">
        <header className="h-[var(--zc-header-height)] flex items-center gap-2 px-3 sm:px-4 border-b border-line bg-surface shrink-0">
          <Link
            to={paths.chat}
            className="lg:hidden -ml-1 p-1.5 rounded-[--radius-sm] text-muted hover:bg-surface-2"
            aria-label="Back to channels"
          >
            <ChevronLeft size={19} />
          </Link>

          <span
            className="w-7 h-7 rounded-[--radius-sm] flex items-center justify-center shrink-0"
            style={{
              background: `color-mix(in srgb, ${roomAccent(room.data.type)} 16%, transparent)`,
              color: roomAccent(room.data.type),
            }}
            aria-hidden
          >
            <Hash size={15} />
          </span>

          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <h1 className="font-display text-[15px] font-semibold text-body truncate">
                {room.data.name}
              </h1>
              {room.data.type === "Branch" && room.data.branch && (
                <Badge tone="accent">{room.data.branch}</Badge>
              )}
              {archived && <Badge tone="warning">Archived</Badge>}
            </div>
            <p className="text-[12px] text-faint truncate hidden sm:block">
              {room.data.description}
            </p>
          </div>

          <button
            type="button"
            onClick={() => setShowMembers((v) => !v)}
            aria-pressed={showMembers}
            className="inline-flex items-center gap-1.5 h-8 px-2.5 rounded-[--radius-sm] text-[12.5px] text-muted hover:bg-surface-2 hover:text-body transition-colors shrink-0"
            title="Show who is here"
          >
            <Users size={15} />
            <span className="zc-tabular">
              {online}
              <span className="text-faint">/{room.data.memberCount}</span>
            </span>
          </button>
        </header>

        {connection !== "connected" && (
          <ConnectionBanner
            state={connection === "reconnecting" ? "reconnecting" : connection === "connecting" ? "connecting" : "offline"}
          />
        )}

        {messages.isLoading ? (
          <MessageSkeleton count={7} />
        ) : messages.error ? (
          <ErrorState
            error={messages.error}
            onRetry={() => void messages.refetch()}
            className="flex-1"
          />
        ) : items.length === 0 ? (
          <div className="flex-1 flex items-center justify-center">
            <EmptyState
              icon={<Hash size={20} />}
              title={`This is the start of ${room.data.name}`}
              description={
                room.data.type === "Hr"
                  ? "Raise HR and policy matters here. Messages are checked before they are posted, and you are still anonymous to everyone else."
                  : "Say something to get the conversation going. Everyone here sees your anonymous name, never your real one."
              }
            />
          </div>
        ) : (
          <MessageScroller
            scopeKey={roomId}
            itemCount={items.length}
            hasMore={Boolean(messages.hasNextPage)}
            isLoadingMore={messages.isFetchingNextPage}
            onLoadMore={() => void messages.fetchNextPage()}
          >
            {groupMessages(items, (m) => m.anonymousName).map(
              ({ message, showHeader, dayDivider }) => (
                <div key={message.id}>
                  {dayDivider && <DayDivider date={message.sentAt} />}
                  <MessageItem
                    message={message}
                    showHeader={showHeader || dayDivider}
                    highlighted={highlighted === message.id}
                    onJumpTo={jumpTo}
                    onReply={(target) =>
                      setReplyTo({
                        messageId: target.id,
                        authorName: target.anonymousName,
                        snippet: target.content.slice(0, 120),
                      })
                    }
                    onEdit={async (target, content) => {
                      try {
                        await edit.mutateAsync({ messageId: target.id, content });
                      } catch (error) {
                        toast.error(errorMessage(error, "The message could not be edited."));
                        throw error;
                      }
                    }}
                    onDelete={(target) => {
                      if (!window.confirm("Delete this message? This cannot be undone.")) return;

                      remove.mutate(target.id, {
                        onError: (error) =>
                          toast.error(errorMessage(error, "The message could not be deleted.")),
                      });
                    }}
                    onReport={setReporting}
                    onReact={(target, emoji) =>
                      toggleReaction.mutate(
                        { messageId: target.id, emoji },
                        {
                          onError: (error) =>
                            toast.error(errorMessage(error, "The reaction could not be saved.")),
                        },
                      )
                    }
                  />
                </div>
              ),
            )}
          </MessageScroller>
        )}

        <TypingLine names={typing} />

        <Composer
          placeholder={`Message ${room.data.name}`}
          disabled={archived}
          disabledReason={
            archived ? "This channel is archived. You can read it, but not post." : undefined
          }
          replyTo={replyTo}
          onCancelReply={() => setReplyTo(null)}
          onTyping={notifyTyping}
          onUpload={(file, onProgress) => upload.mutateAsync({ file, onProgress })}
          onSend={async (payload) => {
            await send.mutateAsync(payload);
            notifyStopped();
          }}
        />
      </div>

      {showMembers && (
        <MembersPanel
          members={members.data}
          isLoading={members.isLoading}
          error={members.error}
          onRetry={() => void members.refetch()}
          onClose={() => setShowMembers(false)}
        />
      )}

      {reporting && (
        <ReportDialog
          open
          onClose={() => setReporting(null)}
          kind="RoomMessage"
          messageId={reporting.id}
          authorName={reporting.anonymousName}
          contentPreview={reporting.content}
        />
      )}
    </div>
  );
}

function MobileBackBar() {
  return (
    <div className="h-[var(--zc-header-height)] flex items-center px-3 border-b border-line bg-surface shrink-0 lg:hidden">
      <Link to={paths.chat}>
        <Button variant="ghost" size="sm" icon={<ChevronLeft size={16} />}>
          Channels
        </Button>
      </Link>
    </div>
  );
}
