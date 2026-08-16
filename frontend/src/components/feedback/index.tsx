import { clsx } from "clsx";
import type { ReactNode } from "react";
import {
  AlertTriangle, Ban, CloudOff, Inbox, Loader2, Lock, RefreshCw, WifiOff,
} from "lucide-react";
import { Button } from "../ui";
import { ApiError } from "../../services/api/client";

/**
 * Loading, empty, error and permission states.
 *
 * Centralised so no screen can quietly render a blank div — which is what the previous
 * UI did whenever a request failed, because every failure was caught and turned into an
 * empty array.
 */

// ── Loading ───────────────────────────────────────────────────────────────────

export function Spinner({ size = 18, className }: { size?: number; className?: string }) {
  return <Loader2 size={size} className={clsx("zc-spin", className)} aria-hidden />;
}

export function LoadingState({
  label = "Loading…",
  className,
}: {
  label?: string;
  className?: string;
}) {
  return (
    <div
      className={clsx("flex flex-col items-center justify-center gap-3 py-12 text-faint", className)}
      role="status"
      aria-live="polite"
    >
      <Spinner size={22} />
      <p className="text-[13px]">{label}</p>
    </div>
  );
}

/** Shape-matched placeholder. Preferred over a spinner where the layout is known. */
export function Skeleton({
  className,
  count = 1,
}: {
  className?: string;
  count?: number;
}) {
  return (
    <>
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className={clsx("zc-skeleton", className)} aria-hidden />
      ))}
    </>
  );
}

export function MessageSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="flex flex-col gap-5 p-4" aria-hidden>
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className="flex gap-3">
          <Skeleton className="w-8 h-8 rounded-full shrink-0" />
          <div className="flex-1 flex flex-col gap-2">
            <Skeleton className="h-3 w-32" />
            <Skeleton className={clsx("h-4", i % 3 === 0 ? "w-3/5" : i % 3 === 1 ? "w-4/5" : "w-2/5")} />
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Empty ─────────────────────────────────────────────────────────────────────

export function EmptyState({
  icon,
  title,
  description,
  action,
  className,
}: {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={clsx(
        "flex flex-col items-center justify-center text-center gap-3 py-14 px-6",
        className,
      )}
    >
      <div className="w-11 h-11 rounded-[--radius-lg] bg-surface-2 flex items-center justify-center text-faint">
        {icon ?? <Inbox size={20} />}
      </div>
      <div className="max-w-sm">
        <p className="text-[15px] font-medium text-body">{title}</p>
        {description && <p className="text-[13px] text-faint mt-1 leading-relaxed">{description}</p>}
      </div>
      {action}
    </div>
  );
}

// ── Error ─────────────────────────────────────────────────────────────────────

/**
 * Renders a failure honestly.
 *
 * Chooses its wording from the actual status: a 403 is a permission problem, a 404 is a
 * missing resource, a network failure is a connectivity problem. Only an unexpected
 * error shows the generic message, and even then the trace id is surfaced so a user can
 * quote it.
 */
export function ErrorState({
  error,
  onRetry,
  className,
  compact,
}: {
  error: unknown;
  onRetry?: () => void;
  className?: string;
  compact?: boolean;
}) {
  const api = ApiError.from(error);

  const { icon, title, detail, retryable } = describe(api);

  if (compact) {
    return (
      <div
        className={clsx(
          "flex items-start gap-2.5 p-3 rounded-[--radius-DEFAULT]",
          "bg-danger-soft border border-danger/25 text-[13px]",
          className,
        )}
        role="alert"
      >
        <span className="text-danger shrink-0 mt-0.5">{icon}</span>
        <div className="min-w-0 flex-1">
          <p className="text-body font-medium">{title}</p>
          {detail && <p className="text-muted mt-0.5">{detail}</p>}
        </div>
        {retryable && onRetry && (
          <Button size="sm" variant="ghost" onClick={onRetry} icon={<RefreshCw size={13} />}>
            Retry
          </Button>
        )}
      </div>
    );
  }

  return (
    <div
      className={clsx("flex flex-col items-center justify-center text-center gap-3 py-14 px-6", className)}
      role="alert"
    >
      <div className="w-11 h-11 rounded-[--radius-lg] bg-danger-soft flex items-center justify-center text-danger">
        {icon}
      </div>
      <div className="max-w-md">
        <p className="text-[15px] font-medium text-body">{title}</p>
        {detail && <p className="text-[13px] text-faint mt-1 leading-relaxed">{detail}</p>}
        {api?.traceId && (
          <p className="text-[11px] text-faint mt-2 font-mono">Trace: {api.traceId}</p>
        )}
      </div>
      {retryable && onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry} icon={<RefreshCw size={14} />}>
          Try again
        </Button>
      )}
    </div>
  );
}

function describe(api: ApiError | null): {
  icon: ReactNode;
  title: string;
  detail?: string;
  retryable: boolean;
} {
  if (!api) {
    return {
      icon: <AlertTriangle size={20} />,
      title: "Something went wrong",
      detail: "An unexpected error occurred.",
      retryable: true,
    };
  }

  if (api.isNetworkError) {
    return {
      icon: <CloudOff size={20} />,
      title: "Cannot reach the server",
      detail: "Check your connection. The app will keep trying.",
      retryable: true,
    };
  }

  switch (api.status) {
    case 401:
      return {
        icon: <Lock size={20} />,
        title: "Your session has expired",
        detail: "Sign in again to continue.",
        retryable: false,
      };
    case 403:
      return {
        icon: <Ban size={20} />,
        title: "You do not have access",
        // The server's message is specific and safe (e.g. which office a channel belongs to).
        detail: api.message,
        retryable: false,
      };
    case 404:
      return { icon: <Inbox size={20} />, title: "Not found", detail: api.message, retryable: false };
    case 409:
      return { icon: <AlertTriangle size={20} />, title: "Conflict", detail: api.message, retryable: false };
    case 429:
      return {
        icon: <AlertTriangle size={20} />,
        title: "Too many requests",
        detail: "Please slow down and try again shortly.",
        retryable: true,
      };
    case 503:
      return {
        icon: <CloudOff size={20} />,
        title: "Service unavailable",
        detail: api.message,
        retryable: true,
      };
    default:
      return {
        icon: <AlertTriangle size={20} />,
        title: api.status >= 500 ? "Server error" : "Something went wrong",
        detail: api.message,
        retryable: api.status >= 500,
      };
  }
}

// ── Availability ──────────────────────────────────────────────────────────────

/**
 * Renders a metric the backend could not compute.
 *
 * This exists because the admin dashboard used to show `0` whenever a service was
 * unreachable, so a dead service and a genuine zero were indistinguishable. The backend
 * now says which it is, and this makes that visible.
 */
export function UnavailableState({
  reason,
  compact,
}: {
  reason?: string;
  compact?: boolean;
}) {
  if (compact) {
    return (
      <span
        className="inline-flex items-center gap-1.5 text-faint text-[13px]"
        title={reason ?? "This figure could not be determined."}
      >
        <WifiOff size={13} />
        Unavailable
      </span>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center gap-2 py-10 text-center px-4">
      <WifiOff size={18} className="text-faint" />
      <p className="text-[13px] font-medium text-muted">Data unavailable</p>
      <p className="text-[12px] text-faint max-w-xs">
        {reason ?? "The service that provides this figure could not be reached."}
      </p>
    </div>
  );
}

// ── Connection banner ─────────────────────────────────────────────────────────

/** Surfaces realtime state so a silently-dead socket is never mistaken for a quiet room. */
export function ConnectionBanner({ state }: { state: "connecting" | "reconnecting" | "offline" }) {
  const copy = {
    connecting: { text: "Connecting…", tone: "bg-info-soft text-info border-info/25" },
    reconnecting: { text: "Reconnecting…", tone: "bg-warning-soft text-warning border-warning/25" },
    offline: {
      text: "Disconnected — messages may be delayed",
      tone: "bg-danger-soft text-danger border-danger/25",
    },
  }[state];

  return (
    <div
      className={clsx(
        "flex items-center justify-center gap-2 px-3 py-1.5 border-b text-[12.5px] font-medium",
        copy.tone,
      )}
      role="status"
      aria-live="polite"
    >
      {state === "offline" ? <WifiOff size={13} /> : <Spinner size={13} />}
      {copy.text}
    </div>
  );
}
