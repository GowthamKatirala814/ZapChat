import { useCallback, useEffect, useState, useSyncExternalStore } from "react";

/**
 * Small hooks shared across features.
 *
 * The two browser-state hooks use `useSyncExternalStore` rather than an effect that calls
 * `setState`: that is the API built for exactly this — subscribing to something outside
 * React — and it avoids the render-then-correct flash an effect produces on mount.
 */

function subscribeToWindow(events: string[]) {
  return (onChange: () => void) => {
    events.forEach((event) => window.addEventListener(event, onChange));

    return () => events.forEach((event) => window.removeEventListener(event, onChange));
  };
}

const onlineSubscribe = subscribeToWindow(["online", "offline"]);

/**
 * Whether the browser itself has a network connection.
 *
 * Distinct from SignalR connection state: the socket can be down while the network is
 * fine, and the two need different wording in the UI ("Reconnecting…" versus
 * "You are offline").
 */
export function useOnlineStatus(): boolean {
  return useSyncExternalStore(
    onlineSubscribe,
    () => navigator.onLine,
    // Server snapshot: nothing is rendered on a server here, but the argument keeps the
    // hook safe if the app is ever prerendered.
    () => true,
  );
}

/** Matches a media query, so layout decisions CSS cannot express stay in sync. */
export function useMediaQuery(query: string): boolean {
  const subscribe = useCallback(
    (onChange: () => void) => {
      const list = window.matchMedia(query);
      list.addEventListener("change", onChange);
      return () => list.removeEventListener("change", onChange);
    },
    [query],
  );

  return useSyncExternalStore(
    subscribe,
    () => window.matchMedia(query).matches,
    () => false,
  );
}

/** True below the tablet breakpoint, where the sidebar becomes a drawer. */
export function useIsMobile(): boolean {
  return useMediaQuery("(max-width: 1023px)");
}

/** Delays a rapidly-changing value — used for search boxes that hit the API. */
export function useDebounced<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

/** Closes a menu or popover when the user clicks outside it or presses Escape. */
export function useDismissable(open: boolean, onDismiss: () => void) {
  useEffect(() => {
    if (!open) return;

    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onDismiss();
    };

    // Deferred so the click that opened the menu does not immediately close it.
    const timer = setTimeout(() => document.addEventListener("click", onDismiss), 0);
    document.addEventListener("keydown", onKey);

    return () => {
      clearTimeout(timer);
      document.removeEventListener("click", onDismiss);
      document.removeEventListener("keydown", onKey);
    };
  }, [open, onDismiss]);
}
