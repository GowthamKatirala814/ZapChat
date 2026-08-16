import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import toast from "react-hot-toast";
import { ErrorState } from "../../components/feedback";
import { Button, Field, Modal, Select, Textarea } from "../../components/ui";
import { ApiError, reportsApi } from "../../services/api";
import type { ReportTargetKind } from "../../types/api";

/**
 * Reporting a message.
 *
 * The reporter is taken from the session — there is no user id in the payload, which is
 * what made the old endpoint abusable: it was anonymous and trusted a
 * `reportedByUserId` field, so five forged reports could get any account deleted.
 *
 * The dialog shows the reported text back to the user so they can confirm they picked
 * the right message, and nothing else about its author beyond the pseudonym they can
 * already see in the room.
 */

/** Categories, sent as the free-text reason the server stores (3–500 characters). */
const REASONS = [
  "Harassment or bullying",
  "Hate speech or discrimination",
  "Sexually explicit content",
  "Violence or threats",
  "Spam or advertising",
  "Confidential information",
  "Other",
] as const;

export function ReportDialog({
  open,
  onClose,
  kind,
  messageId,
  authorName,
  contentPreview,
}: {
  open: boolean;
  onClose: () => void;
  kind: ReportTargetKind;
  messageId: string;
  authorName: string;
  contentPreview: string;
}) {
  const [category, setCategory] = useState<string>(REASONS[0]);
  const [details, setDetails] = useState("");

  const submit = useMutation({
    mutationFn: () => {
      const reason = details.trim() ? `${category}: ${details.trim()}` : category;
      return reportsApi.submit(kind, messageId, reason.slice(0, 500));
    },
    onSuccess: () => {
      toast.success("Report submitted. A moderator will review it.");
      reset();
      onClose();
    },
  });

  function reset() {
    setCategory(REASONS[0]);
    setDetails("");
    submit.reset();
  }

  function handleClose() {
    reset();
    onClose();
  }

  // A duplicate report is a unique-index conflict, not a failure the user caused.
  const conflict = ApiError.from(submit.error)?.isConflict;

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title="Report this message"
      description="Reports go to the moderation team. Your identity is not shown to the person you report."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={handleClose}>
            Cancel
          </Button>
          <Button
            variant="danger"
            size="sm"
            loading={submit.isPending}
            onClick={() => submit.mutate()}
          >
            Submit report
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <div className="p-3 rounded-[--radius-DEFAULT] bg-surface-2 border border-line">
          <p className="text-[11.5px] font-medium text-faint uppercase tracking-[0.06em]">
            {authorName}
          </p>
          <p className="text-[13px] text-body mt-1 zc-message-text line-clamp-4">
            {contentPreview || "(no text content)"}
          </p>
        </div>

        {conflict ? (
          <p className="text-[13px] text-warning">You have already reported this message.</p>
        ) : (
          submit.error != null && <ErrorState error={submit.error} compact />
        )}

        <Field label="Reason" htmlFor="reportReason" required>
          <Select
            id="reportReason"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          >
            {REASONS.map((reason) => (
              <option key={reason} value={reason}>
                {reason}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="Additional detail"
          htmlFor="reportDetails"
          hint="Optional. Helps a moderator understand the context."
        >
          <Textarea
            id="reportDetails"
            rows={3}
            maxLength={400}
            value={details}
            onChange={(e) => setDetails(e.target.value)}
            placeholder="What is the problem with this message?"
          />
        </Field>
      </div>
    </Modal>
  );
}
