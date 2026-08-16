import { useRef, type ClipboardEvent, type KeyboardEvent } from "react";
import { clsx } from "clsx";

/**
 * Six-digit code entry.
 *
 * Split boxes rather than one text field, because the code arrives by email and is
 * usually pasted — this handles a full paste into any box, which a plain input with
 * `maxLength={6}` also does, but it additionally makes "how many digits" visible before
 * the user starts typing.
 */
export function OtpInput({
  value,
  onChange,
  disabled,
  onComplete,
}: {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  onComplete?: (value: string) => void;
}) {
  const refs = useRef<Array<HTMLInputElement | null>>([]);
  const digits = value.padEnd(6, " ").slice(0, 6).split("");

  function set(index: number, digit: string) {
    const next = digits.map((d, i) => (i === index ? digit : d)).join("").trimEnd();
    onChange(next);

    if (digit && index < 5) refs.current[index + 1]?.focus();
    if (next.length === 6 && !next.includes(" ")) onComplete?.(next);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>, index: number) {
    if (event.key === "Backspace" && !digits[index].trim() && index > 0) {
      refs.current[index - 1]?.focus();
    }
    if (event.key === "ArrowLeft" && index > 0) refs.current[index - 1]?.focus();
    if (event.key === "ArrowRight" && index < 5) refs.current[index + 1]?.focus();
  }

  function handlePaste(event: ClipboardEvent<HTMLInputElement>) {
    const pasted = event.clipboardData.getData("text").replace(/\D/g, "").slice(0, 6);
    if (!pasted) return;

    event.preventDefault();
    onChange(pasted);
    refs.current[Math.min(pasted.length, 5)]?.focus();

    if (pasted.length === 6) onComplete?.(pasted);
  }

  return (
    <div className="flex gap-2 justify-between" role="group" aria-label="Verification code">
      {digits.map((digit, index) => (
        <input
          key={index}
          ref={(element) => {
            refs.current[index] = element;
          }}
          type="text"
          inputMode="numeric"
          autoComplete={index === 0 ? "one-time-code" : "off"}
          maxLength={1}
          disabled={disabled}
          value={digit.trim()}
          aria-label={`Digit ${index + 1}`}
          onChange={(e) => set(index, e.target.value.replace(/\D/g, "").slice(-1))}
          onKeyDown={(e) => handleKeyDown(e, index)}
          onPaste={handlePaste}
          onFocus={(e) => e.target.select()}
          className={clsx(
            "w-full h-13 min-w-0 flex-1 text-center text-[20px] font-semibold zc-tabular",
            "bg-surface border border-line rounded-[--radius-DEFAULT] py-2.5",
            "focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20",
            "disabled:bg-surface-2 disabled:cursor-not-allowed",
          )}
        />
      ))}
    </div>
  );
}
