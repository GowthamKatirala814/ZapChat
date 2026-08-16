import { clsx } from "clsx";
import { useEffect, useRef, useState } from "react";
import { Button } from "../../components/ui";
import { Avatar } from "../../components/ui";
import { MessageActions } from "../../components/message/MessageActions";
import { canStillEdit } from "../../lib/messages";
import {
  AttachmentList, DeletedMessage, ReactionRow, ReplyQuote, Timestamp,
} from "../../components/message/MessageMeta";
import type { Message } from "../../types/api";

/**
 * One message in a room.
 *
 * Consecutive messages from the same pseudonym within five minutes are grouped: the
 * avatar and name appear once and the follow-ups indent under them. That is what makes a
 * busy channel readable, and it is why `showHeader` is computed by the list rather than
 * by the item.
 */
export function MessageItem({
  message,
  showHeader,
  highlighted,
  onReply,
  onEdit,
  onDelete,
  onReport,
  onReact,
  onJumpTo,
}: {
  message: Message;
  showHeader: boolean;
  highlighted?: boolean;
  onReply: (message: Message) => void;
  onEdit: (message: Message, content: string) => Promise<void>;
  onDelete: (message: Message) => void;
  onReport: (message: Message) => void;
  onReact: (message: Message, emoji: string) => void;
  onJumpTo?: (messageId: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const isDeleted = message.deletedBy !== "None";

  return (
    <div
      id={`message-${message.id}`}
      className={clsx(
        "group relative flex gap-2.5 px-3 sm:px-4 transition-colors",
        showHeader ? "mt-3" : "mt-0.5",
        highlighted && "bg-accent-soft/60 rounded-[--radius-DEFAULT]",
      )}
    >
      <div className="w-8 shrink-0 pt-0.5">
        {showHeader ? (
          <Avatar name={message.anonymousName} size={32} />
        ) : (
          // The timestamp fills the avatar gutter on hover, so a grouped message can
          // still be dated without adding a permanent second column.
          <span className="hidden group-hover:block text-[10px] text-faint text-right pr-1 leading-6 zc-tabular">
            {new Date(message.sentAt).toLocaleTimeString(undefined, {
              hour: "numeric",
              minute: "2-digit",
            })}
          </span>
        )}
      </div>

      <div className="min-w-0 flex-1 pb-0.5">
        {showHeader && (
          <div className="flex items-baseline gap-2 mb-0.5">
            <span className="text-[13.5px] font-semibold text-body truncate">
              {message.anonymousName}
            </span>
            {message.isMine && (
              <span className="text-[10.5px] font-medium text-accent-text bg-accent-soft px-1.5 rounded-[--radius-sm]">
                You
              </span>
            )}
            <Timestamp value={message.sentAt} />
          </div>
        )}

        {message.replyTo && <ReplyQuote reply={message.replyTo} onJump={onJumpTo} />}

        {isDeleted ? (
          <DeletedMessage deletedBy={message.deletedBy} />
        ) : editing ? (
          <InlineEditor
            initial={message.content}
            onCancel={() => setEditing(false)}
            onSave={async (content) => {
              await onEdit(message, content);
              setEditing(false);
            }}
          />
        ) : (
          <>
            <p className="zc-message-text text-[14px] text-body leading-[1.5]">
              {message.content}
              {message.isEdited && (
                <span className="ml-1.5 text-[11px] text-faint align-baseline">(edited)</span>
              )}
            </p>

            <AttachmentList attachments={message.attachments} />
          </>
        )}

        <ReactionRow
          reactions={message.reactions}
          onToggle={(emoji) => onReact(message, emoji)}
          disabled={isDeleted}
        />
      </div>

      {!editing && (
        <MessageActions
          isMine={message.isMine}
          canEdit={canStillEdit(message.sentAt)}
          isDeleted={isDeleted}
          onReply={() => onReply(message)}
          onEdit={() => setEditing(true)}
          onDelete={() => onDelete(message)}
          onReport={() => onReport(message)}
          onReact={(emoji) => onReact(message, emoji)}
        />
      )}
    </div>
  );
}

/** Edit in place. Enter saves, Escape cancels — the shortcuts people already expect. */
function InlineEditor({
  initial,
  onSave,
  onCancel,
}: {
  initial: string;
  onSave: (content: string) => Promise<void>;
  onCancel: () => void;
}) {
  const [value, setValue] = useState(initial);
  const [saving, setSaving] = useState(false);
  const ref = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;

    element.focus();
    element.setSelectionRange(element.value.length, element.value.length);
    element.style.height = `${element.scrollHeight}px`;
  }, []);

  const changed = value.trim() !== initial.trim();

  async function save() {
    if (!value.trim() || !changed) return onCancel();

    setSaving(true);
    try {
      await onSave(value.trim());
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-2 mt-1">
      <textarea
        ref={ref}
        value={value}
        maxLength={2000}
        rows={1}
        onChange={(e) => {
          setValue(e.target.value);
          e.target.style.height = "auto";
          e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`;
        }}
        onKeyDown={(e) => {
          if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            void save();
          }
          if (e.key === "Escape") onCancel();
        }}
        className="w-full bg-surface border border-accent rounded-[--radius-DEFAULT] px-3 py-2 text-[14px] resize-none focus:outline-none focus:ring-2 focus:ring-accent/20"
      />

      <div className="flex items-center gap-2">
        <Button size="sm" loading={saving} disabled={!value.trim()} onClick={() => void save()}>
          Save
        </Button>
        <Button size="sm" variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
        <span className="text-[11.5px] text-faint">Enter to save · Escape to cancel</span>
      </div>
    </div>
  );
}
