import { clsx } from "clsx";
import { SmilePlus } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useReactionOptions } from "../../features/chat/useReactions";
import type { Reaction } from "../../types/api";

/**
 * The reaction picker.
 *
 * Renders every reaction the server accepts, read from the API rather than a local list.
 * Reactions the caller has already applied are shown pressed, so the picker doubles as
 * the place to remove one — the server treats the call as a toggle, and the UI should not
 * pretend otherwise by offering only "add".
 */
export function ReactionPicker({
  reactions,
  onPick,
  align = "right",
  disabled,
}: {
  /** Current reactions on the message, used to show which are already mine. */
  reactions: Reaction[];
  onPick: (emoji: string) => void;
  align?: "left" | "right";
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const { options, isDegraded } = useReactionOptions();
  const rootRef = useRef<HTMLDivElement>(null);

  // Close on outside click or Escape. Bound only while open, so a page full of
  // messages does not carry hundreds of idle listeners.
  useEffect(() => {
    if (!open) return;

    const onDown = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };

    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);

    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const mine = new Set(reactions.filter((r) => r.mine).map((r) => r.emoji));

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        disabled={disabled}
        aria-label="Add reaction"
        aria-expanded={open}
        aria-haspopup="menu"
        title="Add reaction"
        onClick={() => setOpen((v) => !v)}
        className="w-7 h-7 flex items-center justify-center rounded-[--radius-sm] text-muted hover:bg-surface-2 hover:text-body transition-colors disabled:opacity-50"
      >
        <SmilePlus size={15} />
      </button>

      {open && (
        <div
          role="menu"
          aria-label="Reactions"
          className={clsx(
            "absolute bottom-full mb-1.5 z-30 p-1.5 rounded-[--radius-DEFAULT]",
            "bg-surface border border-line shadow-lg zc-enter",
            // Wraps rather than overflowing, so a longer catalogue stays usable.
            "grid grid-cols-4 gap-0.5 w-max",
            align === "right" ? "right-0" : "left-0",
          )}
        >
          {options.map((option) => {
            const active = mine.has(option.emoji);

            return (
              <button
                key={option.name}
                type="button"
                role="menuitem"
                aria-pressed={active}
                title={active ? `Remove ${option.label}` : option.label}
                onClick={() => {
                  onPick(option.emoji);
                  setOpen(false);
                }}
                className={clsx(
                  "w-8 h-8 rounded-[--radius-sm] text-[17px] leading-none",
                  "flex items-center justify-center transition-colors",
                  active
                    ? "bg-accent-soft ring-1 ring-accent/40"
                    : "hover:bg-surface-2",
                )}
              >
                <span aria-hidden>{option.emoji}</span>
                <span className="sr-only">{option.label}</span>
              </button>
            );
          })}

          {isDegraded && (
            <p className="col-span-4 px-1 pt-1 text-[10.5px] text-warning leading-tight">
              Showing a reduced set — the reaction list could not be loaded.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
