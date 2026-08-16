import { clsx } from "clsx";
import { Bell, Hash, MessageSquare, ShieldCheck, User, Vote } from "lucide-react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "../../app/providers";
import { paths } from "../../config";
import { useUnreadNotifications } from "../../features/notifications/useNotifications";

/**
 * Bottom navigation for phones and small tablets.
 *
 * The rail's destinations, minus the ones that do not warrant a thumb-sized target.
 * It sits above the safe-area inset so it clears the iOS home indicator.
 */
export function MobileNav() {
  const { pathname } = useLocation();
  const { isAdmin } = useAuth();
  const unread = useUnreadNotifications();

  const items = [
    { to: paths.chat, label: "Channels", icon: Hash, active: pathname.startsWith("/chat") },
    { to: paths.messages, label: "Direct", icon: MessageSquare, active: pathname.startsWith("/messages") },
    { to: paths.polls, label: "Polls", icon: Vote, active: pathname.startsWith("/polls") },
    {
      to: paths.notifications,
      label: "Activity",
      icon: Bell,
      active: pathname.startsWith("/notifications"),
      badge: unread,
    },
    isAdmin
      ? { to: paths.admin.root, label: "Admin", icon: ShieldCheck, active: pathname.startsWith("/admin") }
      : { to: paths.profile, label: "You", icon: User, active: pathname.startsWith("/profile") },
  ];

  return (
    <nav
      className="lg:hidden shrink-0 bg-surface border-t border-line flex"
      style={{ paddingBottom: "env(safe-area-inset-bottom)" }}
      aria-label="Main"
    >
      {items.map((item) => {
        const Icon = item.icon;

        return (
          <NavLink
            key={item.to}
            to={item.to}
            aria-current={item.active ? "page" : undefined}
            className={clsx(
              "flex-1 flex flex-col items-center gap-0.5 py-2 text-[10.5px] font-medium transition-colors",
              item.active ? "text-accent" : "text-faint",
            )}
          >
            <span className="relative">
              <Icon size={19} />
              {"badge" in item && (item.badge ?? 0) > 0 && (
                <span
                  className="absolute -top-1 -right-1.5 min-w-[15px] h-[15px] px-1 rounded-[--radius-full] bg-accent text-accent-contrast text-[9.5px] font-bold leading-[15px] text-center"
                  aria-label={`${item.badge} unread`}
                >
                  {(item.badge ?? 0) > 9 ? "9+" : item.badge}
                </span>
              )}
            </span>
            {item.label}
          </NavLink>
        );
      })}
    </nav>
  );
}
