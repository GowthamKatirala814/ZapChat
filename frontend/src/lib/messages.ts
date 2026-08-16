import { isSameDay } from "./format";
import type { RoomType } from "../types/api";

/**
 * Message and room rules that the UI has to agree with the server about.
 *
 * These live outside the components that use them so a rule is stated once. The edit
 * window in particular is a server rule mirrored here — if it changes, it changes in two
 * places, and this file is the second one.
 */

/**
 * The server allows an edit for 15 minutes after sending, on both room messages and
 * direct messages (`MessageService.EditWindow` and `ConversationService.EditWindow`).
 * Showing the control past that point produces a rejection the user cannot act on.
 */
export const EDIT_WINDOW_MS = 15 * 60_000;

export function canStillEdit(sentAt: string): boolean {
  return Date.now() - new Date(sentAt).getTime() < EDIT_WINDOW_MS;
}

/** Server limit from `SendMessageRequest.Content`. */
export const MAX_MESSAGE_LENGTH = 2000;

/**
 * Decides where day dividers and author headers go in a room message list.
 *
 * Grouping rule: same author, same day, within five minutes.
 */
export function groupMessages<T extends { id: string; sentAt: string }>(
  messages: T[],
  authorOf: (message: T) => string,
): Array<{ message: T; showHeader: boolean; dayDivider: boolean }> {
  return messages.map((message, index) => {
    const previous = index > 0 ? messages[index - 1] : null;

    if (!previous) return { message, showHeader: true, dayDivider: true };

    const current = new Date(message.sentAt);
    const before = new Date(previous.sentAt);
    const sameDay = isSameDay(current, before);

    const grouped =
      sameDay &&
      authorOf(previous) === authorOf(message) &&
      current.getTime() - before.getTime() < 5 * 60_000;

    return { message, showHeader: !grouped, dayDivider: !sameDay };
  });
}

/** Room-type accent, so a channel's nature is legible before reading its name. */
export function roomAccent(type: RoomType): string {
  if (type === "Branch") return "var(--zc-room-branch)";
  if (type === "Hr") return "var(--zc-room-hr)";
  return "var(--zc-room-general)";
}

/** "RemoveMessage" → "Remove message". Audit actions are stored PascalCase. */
export function humaniseAction(action: string): string {
  const spaced = action.replace(/([a-z0-9])([A-Z])/g, "$1 $2").replace(/_/g, " ");
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}
