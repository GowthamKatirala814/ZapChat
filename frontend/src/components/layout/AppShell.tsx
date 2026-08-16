import { WifiOff } from "lucide-react";
import { Outlet } from "react-router-dom";
import { useOnlineStatus } from "../../lib/hooks";
import { useNotificationStream } from "../../features/notifications/useNotifications";
import { MobileNav } from "./MobileNav";
import { NavRail } from "./NavRail";

/**
 * The authenticated frame.
 *
 * Fixed viewport height with the scroll owned by the inner pane, because a chat log that
 * scrolls the whole document takes the composer off screen on mobile as soon as the
 * keyboard opens. `100dvh` rather than `100vh` for the same reason.
 */
export function AppShell() {
  const online = useOnlineStatus();

  // One notification subscription for the whole application, mounted here so the badge
  // and toasts work regardless of which screen the user is on.
  useNotificationStream();

  return (
    <div className="h-dvh flex flex-col bg-bg text-body overflow-hidden">
      {!online && (
        <div
          className="flex items-center justify-center gap-2 px-3 py-1.5 bg-danger-soft text-danger border-b border-danger/25 text-[12.5px] font-medium"
          role="status"
        >
          <WifiOff size={13} />
          You are offline. Changes will not be saved until the connection returns.
        </div>
      )}

      <div className="flex-1 flex min-h-0">
        <NavRail />
        <main className="flex-1 flex min-w-0 min-h-0">
          <Outlet />
        </main>
      </div>

      <MobileNav />
    </div>
  );
}
