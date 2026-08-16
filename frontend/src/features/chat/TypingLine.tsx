/**
 * "X is typing…".
 *
 * Reserves no space when nobody is typing — an always-present empty row shifts the
 * composer up and down as people type, which is more distracting than the indicator is
 * useful. Names are anonymous names, the only identity the server publishes.
 */
export function TypingLine({ names }: { names: string[] }) {
  if (names.length === 0) return null;

  const text =
    names.length === 1
      ? `${names[0]} is typing`
      : names.length === 2
        ? `${names[0]} and ${names[1]} are typing`
        : `${names[0]} and ${names.length - 1} others are typing`;

  return (
    <div
      className="px-4 py-1 text-[12px] text-faint flex items-center gap-1.5 shrink-0"
      aria-live="polite"
    >
      <span className="flex gap-0.5" aria-hidden>
        {[0, 1, 2].map((index) => (
          <span
            key={index}
            className="w-1 h-1 rounded-[--radius-full] bg-faint"
            style={{
              animation: "zc-pulse 1.1s ease-in-out infinite",
              animationDelay: `${index * 0.18}s`,
            }}
          />
        ))}
      </span>
      {text}
    </div>
  );
}
