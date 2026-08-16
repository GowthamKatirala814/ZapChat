import { ShieldAlert } from "lucide-react";
import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { EmptyState, ErrorState, LoadingState } from "../components/feedback";
import { Button } from "../components/ui";
import { paths } from "../config";
import { useAuth } from "./providers";

/**
 * Route guards.
 *
 * These are a usability layer, not the security boundary. Every endpoint is protected
 * server-side by a deny-by-default policy, so hiding a route only saves the user a
 * pointless 403 — it never *creates* access. That distinction matters: the old frontend
 * gated the admin console on a `localStorage.role` string the user could edit, which
 * looked like authorization and was not.
 */

/** Requires a signed-in session. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, isLoading, probeError, retryProbe } = useAuth();
  const location = useLocation();

  if (isLoading) return <FullPage><LoadingState label="Restoring your session…" /></FullPage>;

  // The probe failed for a reason other than "not signed in" — showing the login page
  // here would be a lie, because the user's session may be perfectly valid.
  if (probeError) {
    return (
      <FullPage>
        <ErrorState error={probeError} onRetry={retryProbe} />
      </FullPage>
    );
  }

  if (!user) {
    // `state.from` is how the login screen returns the user to where they were headed.
    return <Navigate to={paths.login} replace state={{ from: location }} />;
  }

  return <>{children}</>;
}

/** Requires the admin role on top of a session. */
export function RequireAdmin({ children }: { children: ReactNode }) {
  const { isAdmin, isLoading } = useAuth();

  if (isLoading) return <FullPage><LoadingState /></FullPage>;

  if (!isAdmin) {
    return (
      <div className="flex-1 flex items-center justify-center p-6">
        <EmptyState
          icon={<ShieldAlert size={20} />}
          title="Administrator access required"
          description="This area is limited to administrators. If you believe you should have access, contact your workspace administrator."
          action={
            <Button variant="secondary" size="sm" onClick={() => window.history.back()}>
              Go back
            </Button>
          }
        />
      </div>
    );
  }

  return <>{children}</>;
}

/** Sends an already-signed-in user away from login and registration. */
export function RedirectIfAuthenticated({ children }: { children: ReactNode }) {
  const { user, isLoading } = useAuth();

  if (isLoading) return <FullPage><LoadingState /></FullPage>;
  if (user) return <Navigate to={paths.chat} replace />;

  return <>{children}</>;
}

function FullPage({ children }: { children: ReactNode }) {
  return <div className="min-h-dvh flex items-center justify-center bg-bg">{children}</div>;
}
