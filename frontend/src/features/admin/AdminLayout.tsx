import { clsx } from "clsx";
import { BarChart3, ClipboardList, Hash, LayoutDashboard, ShieldCheck, Users } from "lucide-react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { paths } from "../../config";

/**
 * The admin console frame.
 *
 * A horizontal tab strip rather than a second sidebar: the shell already owns the left
 * edge, and nesting two vertical navs makes it unclear which one you are in.
 */
const TABS = [
  { to: paths.admin.root, label: "Overview", icon: LayoutDashboard, exact: true },
  { to: paths.admin.moderation, label: "Moderation", icon: ShieldCheck },
  { to: paths.admin.analytics, label: "Analytics", icon: BarChart3 },
  { to: paths.admin.rooms, label: "Channels", icon: Hash },
  { to: paths.admin.users, label: "People", icon: Users },
  { to: paths.admin.audit, label: "Audit log", icon: ClipboardList },
];

export function AdminLayout() {
  const { pathname } = useLocation();

  return (
    <div className="flex-1 flex flex-col min-w-0 min-h-0">
      <header className="border-b border-line bg-surface shrink-0">
        <div className="px-4 sm:px-6 pt-4">
          <h1 className="font-display text-[17px] font-semibold text-body">Admin console</h1>
          <p className="text-[13px] text-faint mt-0.5">
            Every figure here is read from the platform's own databases.
          </p>
        </div>

        <nav
          className="flex gap-1 px-3 sm:px-5 mt-3 overflow-x-auto zc-scroll-x"
          aria-label="Admin sections"
        >
          {TABS.map((tab) => {
            const active = tab.exact ? pathname === tab.to : pathname.startsWith(tab.to);
            const Icon = tab.icon;

            return (
              <NavLink
                key={tab.to}
                to={tab.to}
                end={tab.exact}
                aria-current={active ? "page" : undefined}
                className={clsx(
                  "inline-flex items-center gap-1.5 px-3 py-2 text-[13px] whitespace-nowrap",
                  "border-b-2 -mb-px transition-colors",
                  active
                    ? "border-accent text-accent-text font-medium"
                    : "border-transparent text-muted hover:text-body",
                )}
              >
                <Icon size={15} />
                {tab.label}
              </NavLink>
            );
          })}
        </nav>
      </header>

      <div className="flex-1 min-h-0 overflow-y-auto">
        <div className="px-4 sm:px-6 py-5 max-w-[1180px] mx-auto">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
