import { clsx } from "clsx";
import { Building2, Hash, LifeBuoy, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { NavLink } from "react-router-dom";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import { CountBadge, Input, SectionLabel } from "../../components/ui";
import { paths } from "../../config";
import { formatRelative } from "../../lib/format";
import { roomAccent } from "../../lib/messages";
import type { Room, RoomType } from "../../types/api";

/**
 * The channel sidebar.
 *
 * The list the server returns is already filtered by the caller's branch, so a user
 * never sees another office's channel here — and could not open it if they did. Grouping
 * is by room type, which is the distinction that actually matters to a reader:
 * company-wide, your office, and HR.
 */

const GROUPS: Array<{ type: RoomType; label: string; icon: typeof Hash }> = [
  { type: "General", label: "Company", icon: Hash },
  { type: "Branch", label: "Your office", icon: Building2 },
  { type: "Hr", label: "People & HR", icon: LifeBuoy },
  { type: "Custom", label: "Other channels", icon: Hash },
];

export function RoomList({
  rooms,
  isLoading,
  error,
  onRetry,
  activeRoomId,
}: {
  rooms: Room[] | undefined;
  isLoading: boolean;
  error: unknown;
  onRetry: () => void;
  activeRoomId?: string;
}) {
  const [search, setSearch] = useState("");

  const grouped = useMemo(() => {
    const query = search.trim().toLowerCase();
    const visible = (rooms ?? []).filter(
      (room) => !query || room.name.toLowerCase().includes(query),
    );

    return GROUPS.map((group) => ({
      ...group,
      rooms: visible.filter((room) => room.type === group.type),
    })).filter((group) => group.rooms.length > 0);
  }, [rooms, search]);

  return (
    <>
      <header className="h-[var(--zc-header-height)] flex items-center px-3 border-b border-line-subtle shrink-0">
        <h1 className="font-display text-[15px] font-semibold text-body">Channels</h1>
      </header>

      <div className="p-2.5 border-b border-line-subtle shrink-0">
        <div className="relative">
          <Search
            size={15}
            className="absolute left-2.5 top-1/2 -translate-y-1/2 text-faint pointer-events-none"
          />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Find a channel"
            aria-label="Find a channel"
            className="h-9 pl-8 text-[13px]"
          />
        </div>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto py-2">
        {isLoading ? (
          <div className="flex flex-col gap-2 px-3">
            <Skeleton className="h-11 rounded-[--radius-DEFAULT]" count={5} />
          </div>
        ) : error ? (
          <ErrorState error={error} onRetry={onRetry} />
        ) : grouped.length === 0 ? (
          <EmptyState
            icon={<Hash size={20} />}
            title={search ? "No channels match" : "No channels yet"}
            description={
              search
                ? "Try a different search term."
                : "Channels are created by an administrator. Once one exists for your office it will appear here."
            }
          />
        ) : (
          grouped.map((group) => (
            <section key={group.type} className="mb-3">
              <SectionLabel>{group.label}</SectionLabel>

              <div className="px-1.5 flex flex-col gap-0.5">
                {group.rooms.map((room) => (
                  <RoomRow key={room.id} room={room} active={room.id === activeRoomId} />
                ))}
              </div>
            </section>
          ))
        )}
      </div>
    </>
  );
}

function RoomRow({ room, active }: { room: Room; active: boolean }) {
  const accent = roomAccent(room.type);
  const unread = room.unreadCount > 0;

  return (
    <NavLink
      to={paths.room(room.id)}
      className={clsx(
        "flex items-start gap-2.5 px-2 py-2 rounded-[--radius-DEFAULT] transition-colors min-w-0",
        active ? "bg-accent-soft" : "hover:bg-surface-2",
      )}
      aria-current={active ? "page" : undefined}
    >
      <span
        className="w-[22px] h-[22px] rounded-[--radius-sm] flex items-center justify-center shrink-0 mt-px"
        style={{ background: `color-mix(in srgb, ${accent} 16%, transparent)`, color: accent }}
        aria-hidden
      >
        <Hash size={13} />
      </span>

      <span className="min-w-0 flex-1">
        <span className="flex items-baseline gap-2">
          <span
            className={clsx(
              "text-[13.5px] truncate flex-1",
              unread ? "font-semibold text-body" : active ? "text-body" : "text-muted",
            )}
          >
            {room.name}
          </span>
          {room.lastMessage && (
            <span className="text-[10.5px] text-faint shrink-0 zc-tabular">
              {formatRelative(room.lastMessage.sentAt)}
            </span>
          )}
        </span>

        <span className="flex items-center gap-2 mt-0.5">
          <span className="text-[12px] text-faint truncate flex-1">
            {room.lastMessage
              ? `${room.lastMessage.authorName}: ${room.lastMessage.preview}`
              : room.description || "No messages yet"}
          </span>
          <CountBadge count={room.unreadCount} />
        </span>
      </span>
    </NavLink>
  );
}
