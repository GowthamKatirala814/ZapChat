import * as signalR from "@microsoft/signalr";
import { config, hubPaths, type HubName } from "../../config";
import { authApi } from "../api";
import type { HubPayloadMap } from "./events";

/**
 * SignalR connection management.
 *
 * One manager for all four hubs. The previous frontend had four hand-rolled connection
 * files, each inlining its own `accessTokenFactory` — five copies of the token fetch in
 * total, none sharing a cache — and one of them created a module-level connection at
 * import time that could never be rebuilt after `stop()`.
 *
 * It also adds the thing all four were missing: an `onreconnected` handler. SignalR
 * group membership is per-connection, so after a reconnect a client stayed live but
 * silently stopped receiving room messages until the user switched rooms.
 */

export type ConnectionStatus = "idle" | "connecting" | "connected" | "reconnecting" | "disconnected";

type StatusListener = (status: ConnectionStatus) => void;

interface Managed {
  connection: signalR.HubConnection;
  /** Groups joined on this hub, replayed after a reconnect. */
  groups: Set<string>;
  status: ConnectionStatus;
  statusListeners: Set<StatusListener>;
  /** Run after a successful reconnect, so callers can backfill what they missed. */
  reconnectListeners: Set<() => void>;
  /** Deduplicates concurrent start() calls from several components mounting at once. */
  starting: Promise<void> | null;
}

const hubs = new Map<HubName, Managed>();

/**
 * The JWT for the WebSocket handshake.
 *
 * A WebSocket upgrade cannot carry an Authorization header, so SignalR appends the token
 * as a query parameter; each service accepts that only on its own hub path. The endpoint
 * requires authentication, so this succeeds via the HttpOnly cookie and never hands a
 * token to an unauthenticated caller.
 */
async function accessTokenFactory(): Promise<string> {
  try {
    return await authApi.hubToken();
  } catch {
    // Empty makes the handshake fail cleanly with a 401 rather than retrying forever
    // with no credential.
    return "";
  }
}

function setStatus(entry: Managed, status: ConnectionStatus) {
  if (entry.status === status) return;
  entry.status = status;
  entry.statusListeners.forEach((listener) => listener(status));
}

/** Join method per hub. Polls and notifications have no groups. */
function joinMethod(hub: HubName): string | null {
  if (hub === "chat") return "JoinRoom";
  if (hub === "privateChat") return "JoinConversation";
  return null;
}

function leaveMethod(hub: HubName): string | null {
  if (hub === "chat") return "LeaveRoom";
  if (hub === "privateChat") return "LeaveConversation";
  return null;
}

function ensure(hub: HubName): Managed {
  const existing = hubs.get(hub);

  if (existing && existing.connection.state !== signalR.HubConnectionState.Disconnected) {
    return existing;
  }

  // A disconnected HubConnection cannot be restarted reliably, so build a fresh one but
  // carry group membership and listeners across.
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${config.hubUrl}${hubPaths[hub]}`, { accessTokenFactory })
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
    .configureLogging(import.meta.env.DEV ? signalR.LogLevel.Warning : signalR.LogLevel.Error)
    .build();

  const entry: Managed = {
    connection,
    groups: existing?.groups ?? new Set<string>(),
    status: "idle",
    statusListeners: existing?.statusListeners ?? new Set(),
    reconnectListeners: existing?.reconnectListeners ?? new Set(),
    starting: null,
  };

  connection.onreconnecting(() => setStatus(entry, "reconnecting"));
  connection.onclose(() => setStatus(entry, "disconnected"));

  connection.onreconnected(async () => {
    setStatus(entry, "connected");

    // Re-join every group. Without this the socket is live but deaf.
    const method = joinMethod(hub);

    if (method) {
      for (const group of [...entry.groups]) {
        try {
          await connection.invoke(method, group);
        } catch {
          // Access may have been revoked while disconnected; stop tracking it.
          entry.groups.delete(group);
        }
      }
    }

    // Tell subscribers to refetch — realtime events that arrived while we were down
    // are gone, so the only correct move is to reconcile with the server.
    entry.reconnectListeners.forEach((listener) => listener());
  });

  hubs.set(hub, entry);
  return entry;
}

/** Starts the hub if needed. Concurrent callers share one start. */
export async function startHub(hub: HubName): Promise<signalR.HubConnection> {
  const entry = ensure(hub);

  if (entry.connection.state === signalR.HubConnectionState.Connected) {
    return entry.connection;
  }

  entry.starting ??= (async () => {
    setStatus(entry, "connecting");
    try {
      await entry.connection.start();
      setStatus(entry, "connected");
    } catch (error) {
      setStatus(entry, "disconnected");
      throw error;
    } finally {
      entry.starting = null;
    }
  })();

  await entry.starting;
  return entry.connection;
}

export async function stopHub(hub: HubName): Promise<void> {
  const entry = hubs.get(hub);
  if (!entry) return;

  entry.groups.clear();
  await entry.connection.stop().catch(() => undefined);
  setStatus(entry, "disconnected");
}

/** Stops every hub. Called on sign-out so a new session never inherits a live socket. */
export async function stopAllHubs(): Promise<void> {
  await Promise.all([...hubs.keys()].map(stopHub));
  hubs.clear();
}

/**
 * Subscribes to an event. Returns an unsubscribe function, so a React effect cleans up
 * by returning it directly and cannot leak the handler — which the old code did by
 * calling `.off(name, freshHandler)` before `.on()`, a no-op because `off` needs the
 * same reference.
 */
export function onHubEvent<H extends HubName, E extends keyof HubPayloadMap[H]>(
  hub: H,
  event: E,
  handler: (payload: HubPayloadMap[H][E]) => void,
): () => void {
  const entry = ensure(hub);
  const wrapped = handler as (...args: unknown[]) => void;

  entry.connection.on(event as string, wrapped);
  return () => entry.connection.off(event as string, wrapped);
}

/** Notified after every successful reconnect so callers can refetch. */
export function onHubReconnected(hub: HubName, handler: () => void): () => void {
  const entry = ensure(hub);
  entry.reconnectListeners.add(handler);
  return () => entry.reconnectListeners.delete(handler);
}

export function onHubStatus(hub: HubName, handler: StatusListener): () => void {
  const entry = ensure(hub);
  entry.statusListeners.add(handler);
  handler(entry.status);
  return () => entry.statusListeners.delete(handler);
}

export function hubStatus(hub: HubName): ConnectionStatus {
  return hubs.get(hub)?.status ?? "idle";
}

/** Joins a group and remembers it, so a reconnect restores membership. */
export async function joinGroup(hub: HubName, group: string): Promise<void> {
  const connection = await startHub(hub);
  const method = joinMethod(hub);

  if (method) await connection.invoke(method, group);

  ensure(hub).groups.add(group);
}

export async function leaveGroup(hub: HubName, group: string): Promise<void> {
  const entry = ensure(hub);
  entry.groups.delete(group);

  const method = leaveMethod(hub);

  if (method && entry.connection.state === signalR.HubConnectionState.Connected) {
    await entry.connection.invoke(method, group).catch(() => undefined);
  }
}

/** Invokes a hub method, starting the connection first if necessary. */
export async function invokeHub<T = void>(
  hub: HubName,
  method: string,
  ...args: unknown[]
): Promise<T> {
  const connection = await startHub(hub);
  return connection.invoke<T>(method, ...args);
}
