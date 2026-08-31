import { clsx } from "clsx";
import { Search, Users, X } from "lucide-react";
import { useMemo, useState } from "react";
import { ErrorState, Skeleton } from "../../components/feedback";
import { Avatar, Input } from "../../components/ui";
import { useCurrentUser } from "../../app/providers";
import type { RoomMember } from "../../types/api";

/**
 * Who is in this channel, and who is here right now.
 *
 * Presence is real: `isOnline` comes from the server's presence collection, which is keyed
 * by SignalR connection and carries a TTL — so a browser that dies without disconnecting
 * cleanly drops off by itself rather than showing as online forever. The list re-renders
 * from RoomPresenceChanged, so it updates as people come and go without polling.
 *
 * Anonymous names only. That is not a display choice: RoomMemberDto has no field for a
 * real name, so this panel cannot leak one even by accident.
 */
export function MembersPanel({
  members,
  isLoading,
  error,
  onRetry,
  onClose,
}: {
  members: RoomMember[] | undefined;
  isLoading: boolean;
  error: unknown;
  onRetry: () => void;
  /** Present on narrow screens, where the panel is a drawer rather than a column. */
  onClose?: () => void;
}) {
  const me = useCurrentUser();
  const [query, setQuery] = useState("");

  const { online, offline, total } = useMemo(() => {
    const all = members ?? [];
    const needle = query.trim().toLowerCase();
    const visible = needle
      ? all.filter((m) => m.anonymousName.toLowerCase().includes(needle))
      : all;

    // Own entry first within each group, then alphabetical — a stable order, so the
    // list does not reshuffle every time presence ticks.
    const sort = (list: RoomMember[]) =>
      [...list].sort((a, b) => {
        if (a.userId === me.userId) return -1;
        if (b.userId === me.userId) return 1;
        return a.anonymousName.localeCompare(b.anonymousName);
      });

    return {
      online: sort(visible.filter((m) => m.isOnline)),
      offline: sort(visible.filter((m) => !m.isOnline)),
      total: all.length,
    };
  }, [members, query, me.userId]);

  return (
    <aside
      aria-label="Channel members"
      className="w-full lg:w-[260px] shrink-0 flex flex-col bg-surface border-l border-line min-h-0"
    >
      <header className="h-[var(--zc-header-height)] flex items-center gap-2 px-3.5 border-b border-line-subtle shrink-0">
        <Users size={15} className="text-faint shrink-0" />
        <h2 className="text-[13.5px] font-semibold text-body flex-1 min-w-0 truncate">
          Members
          {total > 0 && <span className="text-faint font-normal ml-1.5 zc-tabular">{total}</span>}
        </h2>

        {onClose && (
          <button
            type="button"
            onClick={onClose}
            aria-label="Close members panel"
            className="lg:hidden p-1 rounded-[--radius-sm] text-muted hover:bg-surface-2 hover:text-body"
          >
            <X size={16} />
          </button>
        )}
      </header>

      {total > 8 && (
        <div className="p-2.5 border-b border-line-subtle shrink-0">
          <div className="relative">
            <Search
              size={14}
              className="absolute left-2.5 top-1/2 -translate-y-1/2 text-faint pointer-events-none"
            />
            <Input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Find a member"
              aria-label="Find a member"
              className="h-8 pl-7.5 text-[12.5px]"
            />
          </div>
        </div>
      )}

      <div className="flex-1 min-h-0 overflow-y-auto py-2">
        {isLoading ? (
          <div className="flex flex-col gap-1.5 px-2.5">
            <Skeleton className="h-8 rounded-[--radius-DEFAULT]" count={7} />
          </div>
        ) : error ? (
          <div className="px-2.5">
            <ErrorState error={error} onRetry={onRetry} compact />
          </div>
        ) : total === 0 ? (
          <p className="px-4 py-6 text-[12.5px] text-faint text-center">
            Nobody has joined this channel yet.
          </p>
        ) : online.length + offline.length === 0 ? (
          <p className="px-4 py-6 text-[12.5px] text-faint text-center">
            No member matches “{query}”.
          </p>
        ) : (
          <>
            <Group label="Online" count={online.length} members={online} meId={me.userId} accent />
            <Group label="Offline" count={offline.length} members={offline} meId={me.userId} />
          </>
        )}
      </div>
    </aside>
  );
}

function Group({
  label,
  count,
  members,
  meId,
  accent,
}: {
  label: string;
  count: number;
  members: RoomMember[];
  meId: string;
  accent?: boolean;
}) {
  if (members.length === 0) return null;

  return (
    <section className="mb-3">
      <div className="flex items-center gap-1.5 px-3.5 mb-1">
        <span
          className={clsx(
            "w-1.5 h-1.5 rounded-[--radius-full] shrink-0",
            accent ? "bg-success" : "bg-line-strong",
          )}
          aria-hidden
        />
        <span className="text-[11px] font-semibold uppercase tracking-[0.07em] text-faint">
          {label}
        </span>
        <span className="text-[11px] text-faint zc-tabular">{count}</span>
      </div>

      <ul className="px-1.5 flex flex-col">
        {members.map((member) => {
          const isMe = member.userId === meId;

          return (
            <li key={member.userId}>
              <div
                className={clsx(
                  "flex items-center gap-2.5 px-2 py-1.5 rounded-[--radius-DEFAULT]",
                  "hover:bg-surface-2 transition-colors",
                  !member.isOnline && "opacity-60",
                )}
              >
                <Avatar name={member.anonymousName} size={26} online={member.isOnline} />

                <span className="min-w-0 flex-1 text-[13px] truncate text-body">
                  {member.anonymousName}
                </span>

                {isMe && (
                  <span className="text-[10px] font-medium text-accent-text bg-accent-soft px-1.5 rounded-[--radius-sm] shrink-0">
                    You
                  </span>
                )}
              </div>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
