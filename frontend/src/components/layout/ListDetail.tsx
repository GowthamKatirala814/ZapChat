import { clsx } from "clsx";
import type { ReactNode } from "react";

/**
 * Two-pane list/detail layout, used by channels and direct messages.
 *
 * On a wide screen both panes are visible. On a phone there is only room for one, so the
 * URL decides: `/chat` shows the list, `/chat/:id` shows the conversation. That keeps the
 * back button meaningful instead of trapping the user in a detail view with no way out.
 */
export function ListDetail({
  list,
  detail,
  hasDetail,
  listLabel,
}: {
  list: ReactNode;
  detail: ReactNode;
  hasDetail: boolean;
  listLabel: string;
}) {
  return (
    <div className="flex-1 flex min-w-0 min-h-0">
      <aside
        aria-label={listLabel}
        className={clsx(
          "w-full lg:w-[var(--zc-aside-width)] shrink-0 flex-col bg-surface border-r border-line min-h-0",
          hasDetail ? "hidden lg:flex" : "flex",
        )}
      >
        {list}
      </aside>

      <section
        className={clsx(
          "flex-1 min-w-0 min-h-0 flex-col bg-bg",
          hasDetail ? "flex" : "hidden lg:flex",
        )}
      >
        {detail}
      </section>
    </div>
  );
}

/** Header for a single-pane page (polls, activity, profile, admin). */
export function PageHeader({
  title,
  description,
  action,
}: {
  title: ReactNode;
  description?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <header className="flex items-start justify-between gap-4 px-4 sm:px-6 py-4 border-b border-line bg-surface shrink-0">
      <div className="min-w-0">
        <h1 className="font-display text-[17px] font-semibold text-body truncate">{title}</h1>
        {description && <p className="text-[13px] text-faint mt-0.5">{description}</p>}
      </div>
      {action && <div className="shrink-0 flex items-center gap-2">{action}</div>}
    </header>
  );
}

/** Scrolling body of a single-pane page. */
export function PageBody({
  children,
  className,
  width = "wide",
}: {
  children: ReactNode;
  className?: string;
  width?: "narrow" | "wide" | "full";
}) {
  return (
    <div className="flex-1 min-h-0 overflow-y-auto">
      <div
        className={clsx(
          "px-4 sm:px-6 py-5",
          width === "narrow" && "max-w-2xl mx-auto",
          width === "wide" && "max-w-6xl mx-auto",
          className,
        )}
      >
        {children}
      </div>
    </div>
  );
}

/** A single-pane page: header plus scrolling body, sized to the shell. */
export function Page({ children }: { children: ReactNode }) {
  return <div className="flex-1 flex flex-col min-w-0 min-h-0">{children}</div>;
}
