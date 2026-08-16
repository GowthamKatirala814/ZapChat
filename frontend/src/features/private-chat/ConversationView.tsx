import { Ban, ChevronLeft, MoreVertical, ShieldOff } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { Link } from "react-router-dom";
import {
  ConnectionBanner, EmptyState, ErrorState, MessageSkeleton,
} from "../../components/feedback";
import { Composer, type ReplyTarget } from "../../components/message/Composer";
import { DayDivider, MessageScroller } from "../../components/message/MessageScroller";
import { Avatar, Badge, Button } from "../../components/ui";
import { paths } from "../../config";
import { isSameDay } from "../../lib/format";
import { useDismissable } from "../../lib/hooks";
import { flattenPages } from "../../lib/paging";
import { errorMessage } from "../../services/api";
import type { ConnectionStatus } from "../../services/realtime/connection";
import { useTypingIndicator, useTypingSignal } from "../../services/realtime/hooks";
import type { DirectMessage } from "../../types/api";
import { TypingLine } from "../chat/TypingLine";
import { ReportDialog } from "../moderation/ReportDialog";
import { DirectMessageItem } from "./DirectMessageItem";
import {
  privateChatCommands, useConversation, useDirectMessages, usePrivateChatMutations,
} from "./usePrivateChat";

/** One open conversation. */
export function ConversationView({
  conversationId,
  connection,
}: {
  conversationId: string;
  connection: ConnectionStatus;
}) {
  const conversation = useConversation(conversationId);
  const messages = useDirectMessages(conversationId);
  const { send, edit, remove, toggleReaction, block, unblock } =
    usePrivateChatMutations(conversationId);

  const [replyTo, setReplyTo] = useState<ReplyTarget | null>(null);
  const [reporting, setReporting] = useState<DirectMessage | null>(null);
  const [menuOpen, setMenuOpen] = useState(false);

  useDismissable(menuOpen, useCallback(() => setMenuOpen(false), []));

  const typing = useTypingIndicator("privateChat", conversationId, "conversationId");

  const { notifyTyping, notifyStopped } = useTypingSignal(
    useCallback(
      (isTyping: boolean) => {
        if (isTyping) void privateChatCommands.startTyping(conversationId);
        else void privateChatCommands.stopTyping(conversationId);
      },
      [conversationId],
    ),
  );

  const items = flattenPages(messages.data);

  useEffect(() => {
    if (items.length === 0) return;

    const timer = setTimeout(() => void privateChatCommands.markRead(conversationId), 400);
    return () => clearTimeout(timer);
  }, [conversationId, items.length]);

  // No reset effect: PrivateChatPage mounts this with key={conversationId}, so
  // switching conversations remounts and clears drafts by construction.

  if (conversation.isLoading) {
    return (
      <div className="flex-1 flex flex-col">
        <div className="h-[var(--zc-header-height)] border-b border-line bg-surface shrink-0" />
        <MessageSkeleton />
      </div>
    );
  }

  // A conversation id the caller is not part of returns 403, and this is what they see —
  // not a blank pane and not someone else's messages.
  if (conversation.error) {
    return (
      <div className="flex-1 flex flex-col">
        <header className="h-[var(--zc-header-height)] flex items-center px-3 border-b border-line bg-surface shrink-0 lg:hidden">
          <Link to={paths.messages}>
            <Button variant="ghost" size="sm" icon={<ChevronLeft size={16} />}>
              Messages
            </Button>
          </Link>
        </header>
        <ErrorState
          error={conversation.error}
          onRetry={() => void conversation.refetch()}
          className="flex-1"
        />
      </div>
    );
  }

  if (!conversation.data) return null;

  const other = conversation.data;
  const blockedByMe = other.isBlockedByMe;
  const blockedMe = other.hasBlockedMe;

  return (
    <div className="flex-1 flex flex-col min-w-0 min-h-0">
      <header className="h-[var(--zc-header-height)] flex items-center gap-2.5 px-3 sm:px-4 border-b border-line bg-surface shrink-0">
        <Link
          to={paths.messages}
          className="lg:hidden -ml-1 p-1.5 rounded-[--radius-sm] text-muted hover:bg-surface-2"
          aria-label="Back to conversations"
        >
          <ChevronLeft size={19} />
        </Link>

        <Avatar name={other.otherAnonymousName} size={32} />

        <div className="min-w-0 flex-1">
          <h1 className="font-display text-[15px] font-semibold text-body truncate">
            {other.otherAnonymousName}
          </h1>
          <p className="text-[11.5px] text-faint">Anonymous · private conversation</p>
        </div>

        {blockedByMe && <Badge tone="danger">Blocked</Badge>}

        <div className="relative">
          <Button
            size="icon"
            variant="ghost"
            aria-label="Conversation options"
            onClick={(e) => {
              e.stopPropagation();
              setMenuOpen((v) => !v);
            }}
          >
            <MoreVertical size={17} />
          </Button>

          {menuOpen && (
            <div
              className="absolute right-0 top-full mt-1 z-20 min-w-[190px] py-1 rounded-[--radius-DEFAULT] bg-surface border border-line shadow-lg zc-enter"
              onClick={(e) => e.stopPropagation()}
              role="menu"
            >
              <button
                type="button"
                role="menuitem"
                onClick={() => {
                  setMenuOpen(false);

                  if (blockedByMe) {
                    unblock.mutate(other.otherUserId, {
                      onSuccess: () => toast.success("Unblocked."),
                      onError: (error) => toast.error(errorMessage(error)),
                    });
                    return;
                  }

                  if (
                    window.confirm(
                      `Block ${other.otherAnonymousName}? Neither of you will be able to send messages to the other.`,
                    )
                  ) {
                    block.mutate(other.otherUserId, {
                      onSuccess: () => toast.success("Blocked."),
                      onError: (error) => toast.error(errorMessage(error)),
                    });
                  }
                }}
                className="w-full flex items-center gap-2.5 px-3 py-1.5 text-[13px] text-left text-body hover:bg-surface-2"
              >
                {blockedByMe ? <ShieldOff size={14} /> : <Ban size={14} />}
                {blockedByMe ? "Unblock this person" : "Block this person"}
              </button>
            </div>
          )}
        </div>
      </header>

      {connection !== "connected" && (
        <ConnectionBanner
          state={
            connection === "reconnecting"
              ? "reconnecting"
              : connection === "connecting"
                ? "connecting"
                : "offline"
          }
        />
      )}

      {messages.isLoading ? (
        <MessageSkeleton count={6} />
      ) : messages.error ? (
        <ErrorState
          error={messages.error}
          onRetry={() => void messages.refetch()}
          className="flex-1"
        />
      ) : items.length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <EmptyState
            title={`This is the start of your conversation with ${other.otherAnonymousName}`}
            description="Only the two of you can read this. Neither of you sees the other's real name."
          />
        </div>
      ) : (
        <MessageScroller
          scopeKey={conversationId}
          itemCount={items.length}
          hasMore={Boolean(messages.hasNextPage)}
          isLoadingMore={messages.isFetchingNextPage}
          onLoadMore={() => void messages.fetchNextPage()}
        >
          {items.map((message, index) => {
            const previous = index > 0 ? items[index - 1] : null;
            const next = index < items.length - 1 ? items[index + 1] : null;

            const newDay =
              !previous || !isSameDay(new Date(message.sentAt), new Date(previous.sentAt));

            // The tail carries the timestamp and read tick, so a run of consecutive
            // messages from one person is dated once rather than on every bubble.
            const showTail =
              !next ||
              next.isMine !== message.isMine ||
              new Date(next.sentAt).getTime() - new Date(message.sentAt).getTime() > 5 * 60_000;

            return (
              <div key={message.id}>
                {newDay && <DayDivider date={message.sentAt} />}
                <DirectMessageItem
                  message={message}
                  showTail={showTail}
                  onReply={(target) =>
                    setReplyTo({
                      messageId: target.id,
                      authorName: target.senderName,
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
            );
          })}
        </MessageScroller>
      )}

      <TypingLine names={typing} />

      <Composer
        placeholder={`Message ${other.otherAnonymousName}`}
        // Direct messages carry no attachments: SendDirectMessageRequest has no
        // attachment field, so a paperclip here would be a control with nothing behind it.
        allowAttachments={false}
        disabled={blockedByMe || blockedMe}
        disabledReason={
          blockedByMe
            ? "You have blocked this person. Unblock them to send messages."
            : blockedMe
              ? "You can no longer send messages in this conversation."
              : undefined
        }
        replyTo={replyTo}
        onCancelReply={() => setReplyTo(null)}
        onTyping={notifyTyping}
        onSend={async ({ content, replyToMessageId }) => {
          await send.mutateAsync({ content, replyToMessageId });
          notifyStopped();
        }}
      />

      {reporting && (
        <ReportDialog
          open
          onClose={() => setReporting(null)}
          kind="DirectMessage"
          messageId={reporting.id}
          authorName={reporting.senderName}
          contentPreview={reporting.content}
        />
      )}
    </div>
  );
}
