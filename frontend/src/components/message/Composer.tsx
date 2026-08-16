import { clsx } from "clsx";
import { Paperclip, Send, ShieldAlert, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Spinner } from "../feedback";
import { Button } from "../ui";
import { ApiError } from "../../services/api";
import { formatBytes } from "../../lib/format";

/**
 * The message composer, shared by rooms and direct messages.
 *
 * Two behaviours are worth calling out:
 *
 *  - A moderation rejection (422) is rendered *inline, above the box, with the draft
 *    intact*. It is not a page error and not a toast: the user has to be able to edit
 *    what they wrote. The previous UI cleared the input and showed a red banner, so the
 *    message was lost.
 *  - Attachments upload immediately and are sent by id. An upload that fails therefore
 *    never blocks the text, and a message is never sent referencing a file that is not
 *    stored.
 */

export interface PendingAttachment {
  localId: string;
  file: File;
  /** Set once the upload completes; this is what goes in `attachmentIds`. */
  id?: string;
  progress: number;
  error?: string;
}

/** Mirrors the server allowlist in `FileStorageOptions`. */
const ACCEPT =
  ".png,.jpg,.jpeg,.gif,.webp,.pdf,.txt,.csv,.doc,.docx,.xls,.xlsx,.ppt,.pptx";
const MAX_BYTES = 10 * 1024 * 1024;
const MAX_LENGTH = 2000;

export interface ReplyTarget {
  messageId: string;
  authorName: string;
  snippet: string;
}

export function Composer({
  placeholder,
  disabled,
  disabledReason,
  replyTo,
  onCancelReply,
  onSend,
  onTyping,
  onUpload,
  allowAttachments = true,
}: {
  placeholder: string;
  disabled?: boolean;
  disabledReason?: string;
  replyTo?: ReplyTarget | null;
  onCancelReply?: () => void;
  onSend: (payload: {
    content: string;
    replyToMessageId?: string;
    attachmentIds: string[];
  }) => Promise<void>;
  onTyping?: () => void;
  onUpload?: (file: File, onProgress: (n: number) => void) => Promise<{ id: string }>;
  allowAttachments?: boolean;
}) {
  const [value, setValue] = useState("");
  const [attachments, setAttachments] = useState<PendingAttachment[]>([]);
  const [sending, setSending] = useState(false);
  const [blocked, setBlocked] = useState<{ reason: string; category?: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  // Focus the box when a reply is started, so the user can type straight away.
  useEffect(() => {
    if (replyTo) textareaRef.current?.focus();
  }, [replyTo]);

  const uploading = attachments.some((a) => !a.id && !a.error);
  const ready = attachments.filter((a) => a.id).map((a) => a.id!);
  const canSend = (value.trim().length > 0 || ready.length > 0) && !uploading && !disabled;

  function resize() {
    const element = textareaRef.current;
    if (!element) return;

    element.style.height = "auto";
    element.style.height = `${Math.min(element.scrollHeight, 160)}px`;
  }

  async function submit() {
    if (!canSend || sending) return;

    setSending(true);
    setBlocked(null);
    setError(null);

    try {
      await onSend({
        content: value.trim(),
        replyToMessageId: replyTo?.messageId,
        attachmentIds: ready,
      });

      setValue("");
      setAttachments([]);
      onCancelReply?.();

      requestAnimationFrame(resize);
    } catch (caught) {
      const api = ApiError.from(caught);

      if (api?.isRejectedByModeration) {
        // Draft deliberately preserved.
        setBlocked({ reason: api.message, category: api.category });
      } else {
        setError(api?.message ?? "The message could not be sent.");
      }
    } finally {
      setSending(false);
    }
  }

  async function addFiles(files: FileList | null) {
    if (!files || !onUpload) return;

    for (const file of Array.from(files)) {
      const localId = `${file.name}-${file.size}-${attachments.length}-${performance.now()}`;

      if (file.size > MAX_BYTES) {
        setAttachments((current) => [
          ...current,
          { localId, file, progress: 0, error: `Larger than ${formatBytes(MAX_BYTES)}` },
        ]);
        continue;
      }

      setAttachments((current) => [...current, { localId, file, progress: 0 }]);

      try {
        const uploaded = await onUpload(file, (progress) =>
          setAttachments((current) =>
            current.map((a) => (a.localId === localId ? { ...a, progress } : a)),
          ),
        );

        setAttachments((current) =>
          current.map((a) => (a.localId === localId ? { ...a, id: uploaded.id, progress: 100 } : a)),
        );
      } catch (caught) {
        // The server's message names the actual problem — a disallowed type, a
        // signature mismatch — which the user can act on.
        const message = ApiError.from(caught)?.message ?? "Upload failed";
        setAttachments((current) =>
          current.map((a) => (a.localId === localId ? { ...a, error: message } : a)),
        );
      }
    }

    if (fileRef.current) fileRef.current.value = "";
  }

  if (disabled && disabledReason) {
    return (
      <div className="px-4 py-3 border-t border-line bg-surface">
        <p className="text-[13px] text-faint text-center">{disabledReason}</p>
      </div>
    );
  }

  return (
    <div className="border-t border-line bg-surface shrink-0">
      {blocked && (
        <div className="flex items-start gap-2.5 px-4 py-2.5 bg-warning-soft border-b border-warning/25">
          <ShieldAlert size={16} className="text-warning shrink-0 mt-0.5" />
          <div className="min-w-0 flex-1">
            <p className="text-[13px] font-medium text-body">
              This message was not sent
              {blocked.category && blocked.category !== "None" && ` — ${blocked.category}`}
            </p>
            <p className="text-[12.5px] text-muted mt-0.5">{blocked.reason}</p>
          </div>
          <button
            type="button"
            onClick={() => setBlocked(null)}
            className="text-muted hover:text-body p-0.5"
            aria-label="Dismiss"
          >
            <X size={15} />
          </button>
        </div>
      )}

      {error && (
        <div className="flex items-center gap-2 px-4 py-2 bg-danger-soft border-b border-danger/25 text-[13px] text-danger">
          {error}
        </div>
      )}

      {replyTo && (
        <div className="flex items-center gap-2 px-4 py-2 border-b border-line-subtle bg-surface-2">
          <div className="min-w-0 flex-1 border-l-2 border-accent pl-2">
            <p className="text-[11.5px] font-medium text-accent-text">
              Replying to {replyTo.authorName}
            </p>
            <p className="text-[12.5px] text-muted truncate">{replyTo.snippet}</p>
          </div>
          <button
            type="button"
            onClick={onCancelReply}
            className="text-muted hover:text-body p-1"
            aria-label="Cancel reply"
          >
            <X size={15} />
          </button>
        </div>
      )}

      {attachments.length > 0 && (
        <div className="flex flex-wrap gap-2 px-4 pt-3">
          {attachments.map((attachment) => (
            <AttachmentChip
              key={attachment.localId}
              attachment={attachment}
              onRemove={() =>
                setAttachments((current) =>
                  current.filter((a) => a.localId !== attachment.localId),
                )
              }
            />
          ))}
        </div>
      )}

      <div className="flex items-end gap-2 p-3">
        {allowAttachments && onUpload && (
          <>
            <input
              ref={fileRef}
              type="file"
              multiple
              accept={ACCEPT}
              className="hidden"
              onChange={(e) => void addFiles(e.target.files)}
            />
            <button
              type="button"
              onClick={() => fileRef.current?.click()}
              className="w-9 h-9 shrink-0 flex items-center justify-center rounded-[--radius-DEFAULT] text-muted hover:bg-surface-2 hover:text-body transition-colors"
              aria-label="Attach a file"
              title="Attach a file"
            >
              <Paperclip size={17} />
            </button>
          </>
        )}

        <textarea
          ref={textareaRef}
          value={value}
          rows={1}
          maxLength={MAX_LENGTH}
          placeholder={placeholder}
          onChange={(e) => {
            setValue(e.target.value);
            resize();
            onTyping?.();
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              void submit();
            }
          }}
          className={clsx(
            "flex-1 min-w-0 resize-none bg-surface-2 border border-line rounded-[--radius-DEFAULT]",
            "px-3 py-2 text-[14px] text-body placeholder:text-faint leading-[1.5]",
            "focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20",
          )}
        />

        <Button
          size="icon"
          className="h-9 w-9 shrink-0"
          loading={sending}
          disabled={!canSend}
          onClick={() => void submit()}
          aria-label="Send message"
          title="Send (Enter)"
        >
          {!sending && <Send size={16} />}
        </Button>
      </div>

      {value.length > MAX_LENGTH - 200 && (
        <p className="px-4 pb-2 text-[11.5px] text-faint text-right zc-tabular">
          {value.length} / {MAX_LENGTH}
        </p>
      )}
    </div>
  );
}

function AttachmentChip({
  attachment,
  onRemove,
}: {
  attachment: PendingAttachment;
  onRemove: () => void;
}) {
  const pending = !attachment.id && !attachment.error;

  return (
    <span
      className={clsx(
        "inline-flex items-center gap-2 pl-2.5 pr-1 py-1 rounded-[--radius-DEFAULT] border text-[12.5px] max-w-[240px]",
        attachment.error
          ? "bg-danger-soft border-danger/25 text-danger"
          : "bg-surface-2 border-line text-body",
      )}
    >
      {pending && <Spinner size={12} />}
      <span className="min-w-0 truncate">{attachment.file.name}</span>
      <span className="text-faint shrink-0">
        {attachment.error ?? (pending ? `${attachment.progress}%` : formatBytes(attachment.file.size))}
      </span>
      <button
        type="button"
        onClick={onRemove}
        className="p-0.5 hover:opacity-70 shrink-0"
        aria-label={`Remove ${attachment.file.name}`}
      >
        <X size={13} />
      </button>
    </span>
  );
}
