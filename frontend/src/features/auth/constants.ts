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

/**
 * How long a one-time code stays valid, matching `OtpLifetime` in RegistrationService
 * and PasswordResetService. Shown to the user so an expired code is an explanation
 * rather than a surprise.
 */
export const OTP_EXPIRY_MINUTES = 10;

/**
 * Default wait before a code can be resent, matching `Email:ResendCooldownSeconds`.
 * The server is authoritative and answers a too-early request with Retry-After; this is
 * the starting value for the local countdown.
 */
export const RESEND_COOLDOWN_SECONDS = 60;
