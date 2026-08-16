import { GripVertical, Plus, X } from "lucide-react";
import { useState } from "react";
import { ErrorState } from "../../components/feedback";
import { Button, Field, Input, Modal, Textarea } from "../../components/ui";

/**
 * Creating a poll.
 *
 * The limits here mirror `CreatePollRequest` exactly — 5–300 characters for the
 * question, 2–10 options — so the form refuses what the server would refuse, rather than
 * submitting it and translating a validation error afterwards.
 */

const MIN_OPTIONS = 2;
const MAX_OPTIONS = 10;
const MIN_QUESTION = 5;
const MAX_QUESTION = 300;

export function CreatePollDialog({
  open,
  onClose,
  onCreate,
  isPending,
  error,
}: {
  open: boolean;
  onClose: () => void;
  onCreate: (question: string, options: string[]) => void;
  isPending: boolean;
  error: unknown;
}) {
  const [question, setQuestion] = useState("");
  const [options, setOptions] = useState<string[]>(["", ""]);

  const filled = options.map((option) => option.trim()).filter(Boolean);
  const duplicates = new Set(filled.map((o) => o.toLowerCase())).size !== filled.length;

  const problem =
    question.trim().length > 0 && question.trim().length < MIN_QUESTION
      ? `The question needs at least ${MIN_QUESTION} characters.`
      : duplicates
        ? "Two options are the same."
        : undefined;

  const valid = question.trim().length >= MIN_QUESTION && filled.length >= MIN_OPTIONS && !duplicates;

  function reset() {
    setQuestion("");
    setOptions(["", ""]);
  }

  function handleClose() {
    reset();
    onClose();
  }

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title="Create a poll"
      description="Everyone can vote once, and can change or withdraw their vote while the poll is open."
      width={520}
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={handleClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            loading={isPending}
            disabled={!valid}
            onClick={() => onCreate(question.trim(), filled)}
          >
            Create poll
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {error != null && <ErrorState error={error} compact />}

        <Field
          label="Question"
          htmlFor="pollQuestion"
          required
          error={problem && problem.includes("question") ? problem : undefined}
          hint={`${question.length} / ${MAX_QUESTION}`}
        >
          <Textarea
            id="pollQuestion"
            rows={2}
            autoFocus
            required
            maxLength={MAX_QUESTION}
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="What would you like to ask?"
          />
        </Field>

        <Field
          label="Options"
          required
          error={duplicates ? "Two options are the same." : undefined}
          hint={`${MIN_OPTIONS}–${MAX_OPTIONS} options.`}
        >
          <div className="flex flex-col gap-2">
            {options.map((option, index) => (
              <div key={index} className="flex items-center gap-2">
                <GripVertical size={15} className="text-faint shrink-0" aria-hidden />

                <Input
                  value={option}
                  maxLength={200}
                  placeholder={`Option ${index + 1}`}
                  aria-label={`Option ${index + 1}`}
                  onChange={(e) =>
                    setOptions((current) =>
                      current.map((value, i) => (i === index ? e.target.value : value)),
                    )
                  }
                />

                <button
                  type="button"
                  // Below the minimum there is nothing to remove without making the
                  // poll invalid, so the control disappears rather than erroring.
                  disabled={options.length <= MIN_OPTIONS}
                  onClick={() =>
                    setOptions((current) => current.filter((_, i) => i !== index))
                  }
                  className="p-1.5 rounded-[--radius-sm] text-faint hover:text-danger hover:bg-danger-soft transition-colors disabled:opacity-0 disabled:pointer-events-none shrink-0"
                  aria-label={`Remove option ${index + 1}`}
                >
                  <X size={15} />
                </button>
              </div>
            ))}
          </div>
        </Field>

        {options.length < MAX_OPTIONS && (
          <Button
            variant="ghost"
            size="sm"
            icon={<Plus size={14} />}
            className="self-start"
            onClick={() => setOptions((current) => [...current, ""])}
          >
            Add option
          </Button>
        )}
      </div>
    </Modal>
  );
}
