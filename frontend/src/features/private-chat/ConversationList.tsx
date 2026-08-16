import { clsx } from "clsx";
import { Ban, MessageSquarePlus, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import { Avatar, Button, CountBadge, Input } from "../../components/ui";
import { paths } from "../../config";
import { formatRelative } from "../../lib/format";
import type { Conversation } from "../../types/api";

/** The conversation sidebar. Only the caller's own conversations are ever returned. */
export function ConversationList({
  conversations,
  isLoading,
  error,
  onRetry,
  activeId,
  onNew,
}: {
  conversations: Conversation[] | undefined;
  isLoading: boolean;
  error: unknown;
  onRetry: () => void;
  activeId?: string;
  onNew: () => void;
}) {
  const [search, setSearch] = useState("");

  const visible = useMemo(() => {
    const query = search.trim().toLowerCase();
    const all = conversations ?? [];

    return query
      ? all.filter((c) => c.otherAnonymousName.toLowerCase().includes(query))
      : all;
  }, [conversations, search]);

  return (
    <>
      <header className="h-[var(--zc-header-height)] flex items-center justify-between gap-2 px-3 border-b border-line-subtle shrink-0">
        <h1 className="font-display text-[15px] font-semibold text-body">Direct messages</h1>
        <Button
          size="icon"
          variant="ghost"
          onClick={onNew}
          aria-label="New conversation"
          title="New conversation"
        >
          <MessageSquarePlus size={17} />
        </Button>
      </header>

      <div className="p-2.5 border-b border-line-subtle shrink-0">
        <div className="relative">
          <Search
            size={15}
            className="absolute left-2.5 top-1/2 -translate-y-1/2 text-faint pointer-events-none"
          />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Find a conversation"
            aria-label="Find a conversation"
            className="h-9 pl-8 text-[13px]"
          />
        </div>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto p-1.5">
        {isLoading ? (
          <div className="flex flex-col gap-2 px-1.5">
            <Skeleton className="h-14 rounded-[--radius-DEFAULT]" count={5} />
          </div>
        ) : error ? (
          <ErrorState error={error} onRetry={onRetry} />
        ) : visible.length === 0 ? (
          <EmptyState
            icon={<MessageSquarePlus size={20} />}
            title={search ? "No conversations match" : "No conversations yet"}
            description={
              search
                ? "Try a different name."
                : "Start a private conversation with someone. You will both stay anonymous."
            }
            action={
              !search && (
                <Button size="sm" variant="secondary" onClick={onNew}>
                  Start a conversation
                </Button>
              )
            }
          />
        ) : (
          <ul className="flex flex-col gap-0.5">
            {visible.map((conversation) => (
              <li key={conversation.id}>
                <ConversationRow
                  conversation={conversation}
                  active={conversation.id === activeId}
                />
              </li>
            ))}
          </ul>
        )}
      </div>
    </>
  );
}

function ConversationRow({
  conversation,
  active,
}: {
  conversation: Conversation;
  active: boolean;
}) {
  const unread = conversation.unreadCount > 0;
  const blocked = conversation.isBlockedByMe || conversation.hasBlockedMe;

  return (
    <NavLink
      to={paths.conversation(conversation.id)}
      aria-current={active ? "page" : undefined}
      className={clsx(
        "flex items-center gap-2.5 p-2 rounded-[--radius-DEFAULT] transition-colors min-w-0",
        active ? "bg-accent-soft" : "hover:bg-surface-2",
      )}
    >
      <Avatar name={conversation.otherAnonymousName} size={36} />

      <span className="min-w-0 flex-1">
        <span className="flex items-baseline gap-2">
          <span
            className={clsx(
              "text-[13.5px] truncate flex-1",
              unread ? "font-semibold text-body" : "text-body",
            )}
          >
            {conversation.otherAnonymousName}
          </span>
          {conversation.lastMessage && (
            <span className="text-[10.5px] text-faint shrink-0 zc-tabular">
              {formatRelative(conversation.lastMessage.sentAt)}
            </span>
          )}
        </span>

        <span className="flex items-center gap-2 mt-0.5">
          <span className="text-[12px] text-faint truncate flex-1 flex items-center gap-1">
            {blocked && <Ban size={11} className="shrink-0" />}
            {conversation.lastMessage
              ? `${conversation.lastMessage.sentByMe ? "You: " : ""}${conversation.lastMessage.preview}`
              : "No messages yet"}
          </span>
          <CountBadge count={conversation.unreadCount} />
        </span>
      </span>
    </NavLink>
  );
}
