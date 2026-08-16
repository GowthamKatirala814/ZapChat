import { useCallback, useEffect, useState } from "react";
import { config, pushEnabled } from "../../config";
import { notificationsApi } from "../../services/api";

/**
 * Web push.
 *
 * Offered only when the server has VAPID keys configured — without them the backend
 * registers a no-op dispatcher, so a subscribe button would be a control that silently
 * does nothing. `pushEnabled` is that check, made at build time from the same key the
 * service worker needs.
 *
 * Everything else is browser capability: a page served over plain HTTP, or a browser
 * without the Push API, cannot subscribe at all, and the UI says so rather than failing
 * on click.
 */

export type PushState =
  | "unsupported"
  | "unconfigured"
  | "denied"
  | "subscribed"
  | "unsubscribed"
  | "checking";

const supported =
  typeof navigator !== "undefined" &&
  "serviceWorker" in navigator &&
  typeof window !== "undefined" &&
  "PushManager" in window;

/**
 * VAPID keys are base64url; `applicationServerKey` wants raw bytes.
 *
 * Backed by an explicit `ArrayBuffer` rather than the default allocation, because the
 * DOM signature accepts only `ArrayBufferView<ArrayBuffer>` — a plain `Uint8Array` is
 * typed as possibly sitting on a `SharedArrayBuffer`.
 */
function decodeVapidKey(base64: string): Uint8Array<ArrayBuffer> {
  const padded = (base64 + "=".repeat((4 - (base64.length % 4)) % 4))
    .replace(/-/g, "+")
    .replace(/_/g, "/");

  const raw = atob(padded);
  const bytes = new Uint8Array(new ArrayBuffer(raw.length));

  for (let i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);

  return bytes;
}

/** The browser hands these back as ArrayBuffers; the API wants base64url strings. */
function encodeKey(buffer: ArrayBuffer | null): string {
  if (!buffer) return "";

  return btoa(String.fromCharCode(...new Uint8Array(buffer)))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
}

function describe(subscription: PushSubscription) {
  return {
    endpoint: subscription.endpoint,
    p256dh: encodeKey(subscription.getKey("p256dh")),
    auth: encodeKey(subscription.getKey("auth")),
  };
}

export function usePushNotifications() {
  const [state, setState] = useState<PushState>("checking");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const resolve = async (): Promise<PushState> => {
      if (!supported) return "unsupported";
      if (!pushEnabled) return "unconfigured";
      if (Notification.permission === "denied") return "denied";

      const registration = await navigator.serviceWorker.getRegistration("/sw.js");
      const subscription = await registration?.pushManager.getSubscription();

      return subscription ? "subscribed" : "unsubscribed";
    };

    void resolve()
      .then((next) => {
        if (!cancelled) setState(next);
      })
      .catch(() => {
        if (!cancelled) setState("unsupported");
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const subscribe = useCallback(async () => {
    setBusy(true);
    setError(null);

    try {
      const permission = await Notification.requestPermission();

      if (permission !== "granted") {
        setState(permission === "denied" ? "denied" : "unsubscribed");
        return;
      }

      const registration = await navigator.serviceWorker.register("/sw.js");
      await navigator.serviceWorker.ready;

      const subscription = await registration.pushManager.subscribe({
        // Required by Chrome: a push that cannot be shown to the user is rejected.
        userVisibleOnly: true,
        applicationServerKey: decodeVapidKey(config.vapidPublicKey),
      });

      await notificationsApi.subscribePush(describe(subscription));
      setState("subscribed");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not enable push notifications.");
    } finally {
      setBusy(false);
    }
  }, []);

  const unsubscribe = useCallback(async () => {
    setBusy(true);
    setError(null);

    try {
      const registration = await navigator.serviceWorker.getRegistration("/sw.js");
      const subscription = await registration?.pushManager.getSubscription();

      if (subscription) {
        // Tell the server first: if the browser-side unsubscribe succeeded but the call
        // failed, the server would keep pushing to a dead endpoint.
        await notificationsApi.unsubscribePush(describe(subscription));
        await subscription.unsubscribe();
      }

      setState("unsubscribed");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not turn off push notifications.");
    } finally {
      setBusy(false);
    }
  }, []);

  return { state, busy, error, subscribe, unsubscribe };
}
