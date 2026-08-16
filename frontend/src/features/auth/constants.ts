/**
 * Offices a user can belong to.
 *
 * Branch is free text on the server, but it is what gates access to the branch channels,
 * and the platform bootstraps exactly these two (see `RoomService.SystemRooms`). Offering
 * a free-text box here would let someone register into an office with no channel behind
 * it — a dead-end that looks like a bug. Adding an office means adding its room.
 */
export const BRANCHES = ["Hyderabad", "Bangalore"] as const;

/** Suggestions only; department is free text and does not affect access. */
export const DEPARTMENTS = [
  "Engineering",
  "Product",
  "Design",
  "Quality Assurance",
  "Human Resources",
  "Finance",
  "Sales",
  "Marketing",
  "Operations",
  "Support",
] as const;

/** Mirrors the server's `StringLength(128, MinimumLength = 8)` policy. */
export const MIN_PASSWORD_LENGTH = 8;
