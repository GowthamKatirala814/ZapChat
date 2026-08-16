import { useEffect, useRef, useState } from "react";
import type { HubName } from "../../config";
import {
  hubStatus, joinGroup, leaveGroup, onHubEvent, onHubReconnected, onHubStatus, startHub,
  type ConnectionStatus,
} from "./connection";
import type { HubPayloadMap } from "./events";

/**
 * React bindings for the connection manager.
 *
 * These exist so components never touch SignalR directly. A component says "while I am
 * mounted, run this when that event arrives" and the hook handles subscription,
 * cleanup and the stale-closure trap.
 */

/**
 * Subscribes to a hub event for the lifetime of the component.
 *
 * The handler is held in a ref and the effect depends only on the event name, so a
 * handler that closes over changing state does not resubscribe on every render — and
 * never fires with a stale closure either.
 */
export function useHubEvent<H extends HubName, E extends keyof HubPayloadMap[H]>(
  hub: H,
  event: E,
  handler: (payload: HubPayloadMap[H][E]) => void,
  enabled = true,
) {
  const handlerRef = useLatest(handler);

  useEffect(() => {
    if (!enabled) return;

    return onHubEvent(hub, event, (payload) => handlerRef.current(payload));
  }, [hub, event, enabled, handlerRef]);
}

/**
 * Keeps a ref pointing at the newest value of a callback.
 *
 * Updated in an effect rather than during render — a ref write during render is not
 * safe under concurrent rendering, where a render can be discarded. The subscription
 * effects below are declared after this one, so by the time an event can fire the ref
 * already holds the current handler.
 */
function useLatest<T>(value: T) {
  const ref = useRef(value);

  useEffect(() => {
    ref.current = value;
  });

  return ref;
}

/** Ensures a hub is connected while the component is mounted. */
export function useHubConnection(hub: HubName, enabled = true): ConnectionStatus {
  const [status, setStatus] = useState<ConnectionStatus>(() => hubStatus(hub));

  useEffect(() => {
    if (!enabled) return;

    const unsubscribe = onHubStatus(hub, setStatus);

    // Failure is reflected in the status the banner reads; nothing to throw here.
    void startHub(hub).catch(() => undefined);

    return unsubscribe;
  }, [hub, enabled]);

  return status;
}

/**
 * Joins a hub group while mounted, and leaves on unmount or when the group changes.
 *
 * The cleanup leaves the *same* group it joined, because `group` is captured per-effect
 * — so switching rooms quickly cannot leave the room it has just entered.
 */
export function useHubGroup(hub: HubName, group: string | null | undefined, enabled = true) {
  useEffect(() => {
    if (!enabled || !group) return;

    void joinGroup(hub, group).catch(() => undefined);

    return () => {
      void leaveGroup(hub, group).catch(() => undefined);
    };
  }, [hub, group, enabled]);
}

/**
 * Runs after a reconnect.
 *
 * Realtime events that arrived while the socket was down are gone, so the only correct
 * response is to reconcile with the server. Callers pass a refetch.
 */
export function useHubReconnect(hub: HubName, onReconnect: () => void, enabled = true) {
  const handlerRef = useLatest(onReconnect);

  useEffect(() => {
    if (!enabled) return;

    return onHubReconnected(hub, () => handlerRef.current());
  }, [hub, enabled, handlerRef]);
}

/**
 * Tracks who is typing in a scope, expiring each name after a timeout.
 *
 * The old implementation held a single string, so concurrent typers overwrote each
 * other and one person stopping cleared everyone's indicator.
 */
export function useTypingIndicator(
  // Only these two hubs emit typing events; polls and notifications have no such concept.
  hub: "chat" | "privateChat",
  scopeId: string | null,
  scopeKey: "roomId" | "conversationId",
): string[] {
  /**
   * The scope is stored alongside the names rather than being cleared by an effect on
   * change. Reading a name set that belongs to a different room is then impossible by
   * construction, and there is no render where the previous room's indicator is briefly
   * still on screen.
   */
  const [typing, setTyping] = useState<{ scope: string | null; names: string[] }>({
    scope: scopeId,
    names: [],
  });

  const timersRef = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

  const forget = (scope: string, name: string) =>
    setTyping((current) =>
      current.scope !== scope || !current.names.includes(name)
        ? current
        : { scope, names: current.names.filter((existing) => existing !== name) },
    );

  useHubEvent(
    hub,
    "UserTyping",
    (payload) => {
      if (!scopeId || payload[scopeKey] !== scopeId) return;

      const name = payload.anonymousName;

      clearTimeout(timersRef.current[name]);

      // Self-expiring: a client that disconnects mid-typing never sends StopTyping.
      timersRef.current[name] = setTimeout(() => forget(scopeId, name), 4_000);

      setTyping((current) =>
        current.scope === scopeId
          ? current.names.includes(name)
            ? current
            : { scope: scopeId, names: [...current.names, name] }
          : // First event after a scope change: the previous room's names are dropped.
            { scope: scopeId, names: [name] },
      );
    },
    Boolean(scopeId),
  );

  useHubEvent(
    hub,
    "UserStoppedTyping",
    (payload) => {
      if (!scopeId || payload[scopeKey] !== scopeId) return;

      clearTimeout(timersRef.current[payload.anonymousName]);
      forget(scopeId, payload.anonymousName);
    },
    Boolean(scopeId),
  );

  useEffect(
    () => () => {
      Object.values(timersRef.current).forEach(clearTimeout);
    },
    [],
  );

  return typing.scope === scopeId ? typing.names : [];
}

/** Throttles typing notifications so a keystroke does not become a hub call. */
export function useTypingSignal(
  send: (typing: boolean) => void,
  intervalMs = 2_500,
) {
  const lastSentRef = useRef(0);
  const stopTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const notifyTyping = () => {
    const now = Date.now();

    if (now - lastSentRef.current > intervalMs) {
      lastSentRef.current = now;
      send(true);
    }

    if (stopTimerRef.current) clearTimeout(stopTimerRef.current);

    stopTimerRef.current = setTimeout(() => {
      lastSentRef.current = 0;
      send(false);
    }, intervalMs);
  };

  const notifyStopped = () => {
    if (stopTimerRef.current) clearTimeout(stopTimerRef.current);
    lastSentRef.current = 0;
    send(false);
  };

  useEffect(
    () => () => {
      if (stopTimerRef.current) clearTimeout(stopTimerRef.current);
    },
    [],
  );

  return { notifyTyping, notifyStopped };
}
