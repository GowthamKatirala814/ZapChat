import { clsx } from "clsx";
import { useState } from "react";
import { Ban, Check, CheckCheck, FileText, ImageIcon, Trash2 } from "lucide-react";
import { filesApi } from "../../services/api";
import { formatBytes, formatDateTime, formatTime } from "../../lib/format";
import type { Attachment, DeletionKind, Reaction, ReplyReference } from "../../types/api";

/**
 * Pieces shared by room messages and direct messages.
 *
 * Both message types carry the same reply, reaction, attachment and deletion shapes, so
 * they render through the same components — the previous UI had `MessageBubble` and
 * `PrivateMessageBubble` as near-duplicates that had already drifted apart.
 */

// ── Deletion ──────────────────────────────────────────────────────────────────

/**
 * The tombstone left in place of removed content.
 *
 * User deletion and moderation removal read differently on purpose: "this person changed
 * their mind" and "this was taken down" are not the same event, and collapsing them would
 * either accuse someone unfairly or hide that moderation happened. The acting moderator
 * is never named.
 */
export function DeletedMessage({ deletedBy }: { deletedBy: DeletionKind }) {
  const moderated = deletedBy === "Moderation";

  return (
    <span
      className={clsx(
        "inline-flex items-center gap-1.5 text-[13px] italic",
        moderated ? "text-warning" : "text-faint",
      )}
    >
      {moderated ? <Ban size={13} /> : <Trash2 size={13} />}
      {moderated ? "Removed by moderation" : "This message was deleted"}
    </span>
  );
}

// ── Reply ─────────────────────────────────────────────────────────────────────

/**
 * The quoted parent.
 *
 * `snippet` is a snapshot taken when the reply was written, so editing the parent does
 * not silently rewrite history in the reply.
 */
export function ReplyQuote({
  reply,
  onJump,
}: {
  reply: ReplyReference;
  onJump?: (messageId: string) => void;
}) {
  const content = (
    <>
      <span className="block text-[11.5px] font-medium text-accent-text">{reply.authorName}</span>
      <span className="block text-[12.5px] text-muted truncate">{reply.snippet}</span>
    </>
  );

  const className =
    "block w-full text-left border-l-2 border-accent/50 pl-2 py-0.5 mb-1.5 max-w-full min-w-0";

  return onJump ? (
    <button
      type="button"
      onClick={() => onJump(reply.messageId)}
      className={clsx(className, "hover:border-accent transition-colors cursor-pointer")}
    >
      {content}
    </button>
  ) : (
    <span className={className}>{content}</span>
  );
}

// ── Reactions ─────────────────────────────────────────────────────────────────

/**
 * Reaction pills.
 *
 * `mine` and `names` both come from the server, so the pressed state survives a reload
 * and the tooltip lists real reactors rather than a client-side guess. Only anonymous
 * names are ever available here — the API has no field for anything else.
 *
 * Collapses past a threshold. A message that attracts every available reaction would
 * otherwise push a row of pills wider than the bubble and wrap into a block taller than
 * the message itself.
 */
export function ReactionRow({
  reactions,
  onToggle,
  disabled,
}: {
  reactions: Reaction[];
  onToggle: (emoji: string) => void;
  disabled?: boolean;
}) {
  const [expanded, setExpanded] = useState(false);

  if (reactions.length === 0) return null;

  const LIMIT = 6;
  const overflowing = !expanded && reactions.length > LIMIT;
  const shown = overflowing ? reactions.slice(0, LIMIT) : reactions;
  const hidden = reactions.length - shown.length;

  return (
    <div className="flex flex-wrap items-center gap-1 mt-1.5">
      {shown.map((reaction) => (
        <button
          key={reaction.emoji}
          type="button"
          disabled={disabled}
          onClick={() => onToggle(reaction.emoji)}
          title={describeReactors(reaction)}
          aria-label={describeReactors(reaction)}
          aria-pressed={reaction.mine}
          className={clsx(
            "inline-flex items-center gap-1 h-6 pl-1.5 pr-2 rounded-[--radius-full] border",
            "text-[12px] leading-none transition-colors disabled:opacity-60",
            reaction.mine
              ? "bg-accent-soft border-accent/45 text-accent-text font-semibold"
              : "bg-surface-2 border-line hover:border-line-strong text-muted",
          )}
        >
          <span className="text-[13px]" aria-hidden>{reaction.emoji}</span>
          <span className="zc-tabular">{reaction.count}</span>
        </button>
      ))}

      {overflowing && (
        <button
          type="button"
          onClick={() => setExpanded(true)}
          className="h-6 px-2 rounded-[--radius-full] border border-line bg-surface-2 text-[11.5px] text-muted hover:text-body hover:border-line-strong transition-colors"
          title={`Show ${hidden} more reaction${hidden === 1 ? "" : "s"}`}
        >
          +{hidden}
        </button>
      )}
    </div>
  );
}

/**
 * The tooltip text for a pill.
 *
 * Truncated past a handful of names: the browser renders a title attribute as a single
 * unwrapped line, so fifty names become a tooltip wider than the screen.
 */
function describeReactors(reaction: Reaction): string {
  const { names, count, emoji, mine } = reaction;

  if (names.length === 0) {
    return `${count} reacted with ${emoji}`;
  }

  const MAX = 8;
  const listed = names.slice(0, MAX).join(", ");
  const rest = names.length - MAX;
  const who = rest > 0 ? `${listed} and ${rest} more` : listed;

  return mine
    ? `${who} reacted with ${emoji} — click to remove yours`
    : `${who} reacted with ${emoji}`;
}

// ── Attachments ───────────────────────────────────────────────────────────────

export function AttachmentList({ attachments }: { attachments: Attachment[] }) {
  if (attachments.length === 0) return null;

  return (
    <div className="flex flex-col gap-1.5 mt-2">
      {attachments.map((attachment) => (
        <AttachmentCard key={attachment.id} attachment={attachment} />
      ))}
    </div>
  );
}

function AttachmentCard({ attachment }: { attachment: Attachment }) {
  const isImage = attachment.contentType.startsWith("image/");
  // Downloads are authorized by room membership on the server, so the raw URL is safe
  // to link but useless to anyone outside the room.
  const href = filesApi.downloadUrl(attachment.id);

  if (isImage) {
    return (
      <a href={href} target="_blank" rel="noreferrer" className="block max-w-[280px]">
        <img
          src={href}
          alt={attachment.fileName}
          loading="lazy"
          className="rounded-[--radius-DEFAULT] border border-line max-h-[220px] w-auto object-cover"
        />
      </a>
    );
  }

  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      className="inline-flex items-center gap-2.5 p-2 pr-3 rounded-[--radius-DEFAULT] bg-surface-2 border border-line hover:border-line-strong transition-colors max-w-[300px]"
    >
      <span className="w-8 h-8 rounded-[--radius-sm] bg-surface flex items-center justify-center text-accent shrink-0">
        {attachment.contentType.startsWith("image/") ? <ImageIcon size={15} /> : <FileText size={15} />}
      </span>
      <span className="min-w-0">
        <span className="block text-[13px] text-body truncate">{attachment.fileName}</span>
        <span className="block text-[11.5px] text-faint">{formatBytes(attachment.sizeBytes)}</span>
      </span>
    </a>
  );
}

// ── Timestamp ─────────────────────────────────────────────────────────────────

export function Timestamp({
  value,
  edited,
  className,
}: {
  value: string;
  edited?: boolean;
  className?: string;
}) {
  return (
    <time
      dateTime={value}
      // The exact moment is one hover away; the line itself stays uncluttered.
      title={formatDateTime(value)}
      className={clsx("text-[11px] text-faint shrink-0 zc-tabular", className)}
    >
      {formatTime(value)}
      {edited && <span className="ml-1 not-italic">(edited)</span>}
    </time>
  );
}

/** Read state for a direct message. Rooms have no per-message receipt. */
export function ReadTick({ readAt }: { readAt?: string }) {
  return readAt ? (
    <CheckCheck size={13} className="text-accent shrink-0" aria-label="Read" />
  ) : (
    <Check size={13} className="text-faint shrink-0" aria-label="Sent" />
  );
}
