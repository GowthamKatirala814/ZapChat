/**
 * Formatting helpers.
 *
 * All timestamps from the backend are ISO-8601 UTC. Every one of them is rendered
 * through this file so the browser converts to local time exactly once — the old UI
 * mixed `new Date(x).toLocaleTimeString()` with hand-built strings and showed the same
 * message at two different times in two places.
 */

const time = new Intl.DateTimeFormat(undefined, { hour: "numeric", minute: "2-digit" });
const dayMonth = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "short" });
const fullDate = new Intl.DateTimeFormat(undefined, {
  day: "numeric",
  month: "short",
  year: "numeric",
});
const fullDateTime = new Intl.DateTimeFormat(undefined, {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "numeric",
  minute: "2-digit",
});

function parse(value: string | Date): Date {
  return value instanceof Date ? value : new Date(value);
}

/** Clock time — for a message inside a day group. */
export function formatTime(value: string | Date): string {
  return time.format(parse(value));
}

/** Full timestamp, for tooltips where the exact moment matters. */
export function formatDateTime(value: string | Date): string {
  return fullDateTime.format(parse(value));
}

export function formatDate(value: string | Date): string {
  return fullDate.format(parse(value));
}

/** "Today" / "Yesterday" / a date — the separator between message day groups. */
export function formatDayLabel(value: string | Date): string {
  const date = parse(value);
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);

  if (isSameDay(date, today)) return "Today";
  if (isSameDay(date, yesterday)) return "Yesterday";

  return date.getFullYear() === today.getFullYear() ? dayMonth.format(date) : fullDate.format(date);
}

export function isSameDay(a: Date, b: Date): boolean {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

/** Compact relative time for lists: "now", "4m", "3h", "2d", then a date. */
export function formatRelative(value: string | Date): string {
  const date = parse(value);
  const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

  if (seconds < 45) return "now";
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
  if (seconds < 86_400) return `${Math.floor(seconds / 3600)}h`;
  if (seconds < 604_800) return `${Math.floor(seconds / 86_400)}d`;

  return dayMonth.format(date);
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Thousands separators for dashboard figures. */
export function formatCount(value: number): string {
  return new Intl.NumberFormat().format(value);
}

/** Chart axis label from an ISO date. */
export function formatShortDate(value: string): string {
  return dayMonth.format(new Date(value));
}
