import { clsx } from "clsx";
import { useEffect, useRef, useState } from "react";
import { MessageActions } from "../../components/message/MessageActions";
import { canStillEdit } from "../../lib/messages";
import {
  DeletedMessage, ReactionRow, ReadTick, ReplyQuote, Timestamp,
} from "../../components/message/MessageMeta";
import { Button } from "../../components/ui";
import type { DirectMessage } from "../../types/api";

/**
 * One direct message.
 *
 * Rendered as a two-sided bubble rather than the channel's flat list, because a
 * conversation with exactly one other person reads better that way — and because `isMine`
 * is genuinely per-recipient here: the private-chat hub sends each participant their own
 * payload rather than one broadcast to a group.
 */
export function DirectMessageItem({
  message,
  showTail,
  onReply,
  onEdit,
  onDelete,
  onReport,
  onReact,
}: {
  message: DirectMessage;
  /** Last in a run from the same sender — carries the timestamp and read tick. */
  showTail: boolean;
  onReply: (message: DirectMessage) => void;
  onEdit: (message: DirectMessage, content: string) => Promise<void>;
  onDelete: (message: DirectMessage) => void;
  onReport: (message: DirectMessage) => void;
  onReact: (message: DirectMessage, emoji: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const isDeleted = message.deletedBy !== "None";
  const mine = message.isMine;

  return (
    <div
      id={`message-${message.id}`}
      className={clsx(
        "group relative flex px-3 sm:px-4",
        showTail ? "mb-2.5" : "mb-0.5",
        mine ? "justify-end" : "justify-start",
      )}
    >
      <div className={clsx("max-w-[min(560px,78%)] min-w-0 flex flex-col", mine && "items-end")}>
        <div
          className={clsx(
            "px-3 py-2 rounded-[--radius-lg] min-w-0",
            mine
              ? "bg-accent text-accent-contrast rounded-br-[--radius-sm]"
              : "bg-surface border border-line text-body rounded-bl-[--radius-sm]",
            isDeleted && "bg-surface-2 border border-line text-muted",
          )}
        >
          {message.replyTo && !isDeleted && (
            <div className={clsx(mine && !isDeleted && "[&_*]:!text-accent-contrast/85")}>
              <ReplyQuote reply={message.replyTo} />
            </div>
          )}

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
            <p className="zc-message-text text-[14px] leading-[1.5]">{message.content}</p>
          )}
        </div>

        <div className={clsx("flex", mine && "flex-row-reverse")}>
          <ReactionRow
            reactions={message.reactions}
            onToggle={(emoji) => onReact(message, emoji)}
            disabled={isDeleted}
          />
        </div>

        {showTail && (
          <div className="flex items-center gap-1.5 mt-0.5 px-1">
            <Timestamp value={message.sentAt} edited={message.isEdited} />
            {/* Read state is only meaningful for messages we sent. */}
            {mine && !isDeleted && <ReadTick readAt={message.readAt} />}
          </div>
        )}
      </div>

      {!editing && (
        <MessageActions
          isMine={mine}
          canEdit={canStillEdit(message.sentAt)}
          isDeleted={isDeleted}
          align={mine ? "left" : "right"}
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
  }, []);

  async function save() {
    if (!value.trim() || value.trim() === initial.trim()) return onCancel();

    setSaving(true);
    try {
      await onSave(value.trim());
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-2 min-w-[220px]">
      <textarea
        ref={ref}
        value={value}
        maxLength={2000}
        rows={2}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            void save();
          }
          if (e.key === "Escape") onCancel();
        }}
        className="w-full bg-surface text-body border border-line rounded-[--radius-sm] px-2 py-1.5 text-[14px] resize-none focus:outline-none focus:ring-2 focus:ring-accent/30"
      />
      <div className="flex gap-2">
        <Button size="sm" variant="subtle" loading={saving} onClick={() => void save()}>
          Save
        </Button>
        <Button size="sm" variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
