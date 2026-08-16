import { clsx } from "clsx";
import {
  BarChart3, Bell, Hash, LogOut, MessageSquare, Moon, Monitor, ShieldCheck, Sun, Vote,
} from "lucide-react";
import { useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth, useTheme } from "../../app/providers";
import { paths } from "../../config";
import { Avatar, CountBadge } from "../ui";
import { useUnreadNotifications } from "../../features/notifications/useNotifications";

/**
 * Primary navigation.
 *
 * Every destination in the product is reachable from here, and the count badges come
 * from the server rather than being derived client-side — so the number on the bell is
 * the same number the notifications page shows.
 */

interface NavItem {
  to: string;
  label: string;
  icon: typeof Hash;
  /** Matches nested routes: /chat/:id keeps Channels highlighted. */
  match: (pathname: string) => boolean;
}

const items: NavItem[] = [
  {
    to: paths.chat,
    label: "Channels",
    icon: Hash,
    match: (p) => p.startsWith("/chat"),
  },
  {
    to: paths.messages,
    label: "Direct",
    icon: MessageSquare,
    match: (p) => p.startsWith("/messages"),
  },
  { to: paths.polls, label: "Polls", icon: Vote, match: (p) => p.startsWith("/polls") },
  {
    to: paths.notifications,
    label: "Activity",
    icon: Bell,
    match: (p) => p.startsWith("/notifications"),
  },
];

export function NavRail() {
  const { user, isAdmin, signOut } = useAuth();
  const location = useLocation();
  const unread = useUnreadNotifications();

  return (
    <nav
      className="hidden lg:flex flex-col w-[var(--zc-nav-width)] shrink-0 bg-surface border-r border-line"
      aria-label="Main"
    >
      <Brand />

      <div className="flex-1 overflow-y-auto px-2 py-2 flex flex-col gap-0.5">
        {items.map((item) => (
          <RailLink
            key={item.to}
            item={item}
            active={item.match(location.pathname)}
            badge={item.to === paths.notifications ? unread : 0}
          />
        ))}

        {isAdmin && (
          <>
            <div className="h-px bg-line-subtle my-2 mx-2" />
            <RailLink
              item={{
                to: paths.admin.root,
                label: "Admin",
                icon: ShieldCheck,
                match: (p) => p === paths.admin.root,
              }}
              active={location.pathname === paths.admin.root}
            />
            <RailLink
              item={{
                to: paths.admin.moderation,
                label: "Moderation",
                icon: ShieldCheck,
                match: (p) => p.startsWith(paths.admin.moderation),
              }}
              active={location.pathname.startsWith(paths.admin.moderation)}
            />
            <RailLink
              item={{
                to: paths.admin.analytics,
                label: "Analytics",
                icon: BarChart3,
                match: (p) => p.startsWith(paths.admin.analytics),
              }}
              active={location.pathname.startsWith(paths.admin.analytics)}
            />
          </>
        )}
      </div>

      <div className="p-2 border-t border-line-subtle flex flex-col gap-1">
        <ThemeToggle />

        <NavLink
          to={paths.profile}
          className={({ isActive }) =>
            clsx(
              "flex items-center gap-2.5 px-2 py-2 rounded-[--radius-DEFAULT] transition-colors min-w-0",
              isActive ? "bg-accent-soft" : "hover:bg-surface-2",
            )
          }
        >
          <Avatar name={user?.anonymousName ?? "?"} size={30} />
          <span className="min-w-0 flex-1 text-left">
            {/* The anonymous name is what everyone else sees, so it leads. */}
            <span className="block text-[13px] font-medium text-body truncate">
              {user?.anonymousName}
            </span>
            <span className="block text-[11px] text-faint truncate">You · {user?.branch}</span>
          </span>
        </NavLink>

        <button
          type="button"
          onClick={() => void signOut()}
          className="flex items-center gap-2.5 px-3 py-2 rounded-[--radius-DEFAULT] text-[13px] text-muted hover:bg-surface-2 hover:text-body transition-colors"
        >
          <LogOut size={16} />
          Sign out
        </button>
      </div>
    </nav>
  );
}

function RailLink({
  item,
  active,
  badge = 0,
}: {
  item: NavItem;
  active: boolean;
  badge?: number;
}) {
  const Icon = item.icon;

  return (
    <NavLink
      to={item.to}
      aria-current={active ? "page" : undefined}
      className={clsx(
        "flex items-center gap-2.5 px-3 py-2 rounded-[--radius-DEFAULT] text-[13.5px] transition-colors",
        active
          ? "bg-accent-soft text-accent-text font-medium"
          : "text-muted hover:bg-surface-2 hover:text-body",
      )}
    >
      <Icon size={17} className="shrink-0" />
      <span className="flex-1 truncate">{item.label}</span>
      <CountBadge count={badge} />
    </NavLink>
  );
}

function Brand() {
  return (
    <div className="h-[var(--zc-header-height)] flex items-center gap-2.5 px-4 border-b border-line-subtle shrink-0">
      <span
        className="w-7 h-7 rounded-[--radius-sm] flex items-center justify-center text-accent-contrast font-bold text-[15px]"
        style={{ background: "linear-gradient(135deg, var(--zc-accent), var(--zc-room-branch))" }}
        aria-hidden
      >
        Z
      </span>
      <span className="font-display font-semibold text-[15px] text-body">ZapChat</span>
    </div>
  );
}

/** Cycles light → dark → system, showing which mode is active. */
function ThemeToggle() {
  const { preference, setPreference } = useTheme();
  const [order] = useState<Array<"light" | "dark" | "system">>(["light", "dark", "system"]);

  const meta = {
    light: { icon: Sun, label: "Light" },
    dark: { icon: Moon, label: "Dark" },
    system: { icon: Monitor, label: "System" },
  }[preference];

  const Icon = meta.icon;

  return (
    <button
      type="button"
      onClick={() => setPreference(order[(order.indexOf(preference) + 1) % order.length])}
      className="flex items-center gap-2.5 px-3 py-2 rounded-[--radius-DEFAULT] text-[13px] text-muted hover:bg-surface-2 hover:text-body transition-colors"
      aria-label={`Theme: ${meta.label}. Click to change.`}
    >
      <Icon size={16} />
      {meta.label}
    </button>
  );
}
