import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "../../services/api";

/**
 * The wait before another one-time code may be requested.
 *
 * The server enforces this per mailbox and returns 429 with a `Retry-After` header when
 * it is too soon. The countdown here mirrors that so the button is disabled rather than
 * pressed into an error — but the client is only the display: removing it would change
 * nothing about what the server allows.
 *
 * Password reset is the case that makes the local timer necessary rather than merely
 * nice. There the server answers a throttled request with the same success sentence as
 * an unthrottled one, because a 429 would reveal that the address has an account. So the
 * client cannot learn the remaining time from a rejection, and has to keep its own.
 */
export function useResendCountdown(defaultSeconds = 60) {
  const [secondsLeft, setSecondsLeft] = useState(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const clear = () => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
  };

  const start = useCallback((seconds = defaultSeconds) => {
    clear();
    setSecondsLeft(Math.max(0, Math.ceil(seconds)));

    timerRef.current = setInterval(() => {
      setSecondsLeft((remaining) => {
        if (remaining <= 1) {
          clear();
          return 0;
        }
        return remaining - 1;
      });
    }, 1000);
  }, [defaultSeconds]);

  /**
   * Restarts the countdown from a rejected request.
   *
   * Prefers the server's `Retry-After`, since only the server knows when the last code
   * actually went out — a client that has just been reloaded has no idea.
   */
  const startFromError = useCallback(
    (error: unknown) => {
      const seconds = ApiError.from(error)?.retryAfterSeconds;
      start(seconds && seconds > 0 ? seconds : defaultSeconds);
    },
    [start, defaultSeconds],
  );

  useEffect(() => clear, []);

  return { secondsLeft, canResend: secondsLeft === 0, start, startFromError };
}
