import { X } from "lucide-react";
import { ErrorState, Skeleton } from "../../components/feedback";
import { Avatar, SectionLabel } from "../../components/ui";
import type { RoomMember } from "../../types/api";

/**
 * Who is in this channel.
 *
 * Anonymous names only — this panel is the most obvious place a real identity could leak,
 * and the server's DTO simply has no field for one. Online state comes from presence,
 * which is keyed by connection and expires on a TTL, so a browser that dies without
 * disconnecting cleanly does not stay "online" forever.
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
  onClose: () => void;
}) {
  const online = members?.filter((m) => m.isOnline) ?? [];
  const offline = members?.filter((m) => !m.isOnline) ?? [];

  return (
    <aside
      className="w-[240px] shrink-0 flex flex-col bg-surface border-l border-line min-h-0 hidden md:flex"
      aria-label="Channel members"
    >
      <header className="h-[var(--zc-header-height)] flex items-center justify-between px-3 border-b border-line-subtle shrink-0">
        <h2 className="text-[13.5px] font-semibold text-body">Members</h2>
        <button
          type="button"
          onClick={onClose}
          className="p-1 rounded-[--radius-sm] text-muted hover:bg-surface-2 hover:text-body"
          aria-label="Close members panel"
        >
          <X size={16} />
        </button>
      </header>

      <div className="flex-1 min-h-0 overflow-y-auto py-2">
        {isLoading ? (
          <div className="flex flex-col gap-2 px-3">
            <Skeleton className="h-8 rounded-[--radius-DEFAULT]" count={6} />
          </div>
        ) : error ? (
          <ErrorState error={error} onRetry={onRetry} compact className="mx-3" />
        ) : (
          <>
            <Group label={`Online — ${online.length}`} members={online} />
            {offline.length > 0 && <Group label={`Offline — ${offline.length}`} members={offline} />}
          </>
        )}
      </div>
    </aside>
  );
}

function Group({ label, members }: { label: string; members: RoomMember[] }) {
  if (members.length === 0) return null;

  return (
    <section className="mb-3">
      <SectionLabel>{label}</SectionLabel>

      <div className="px-1.5 flex flex-col">
        {members.map((member) => (
          <div
            key={member.userId}
            className="flex items-center gap-2.5 px-2 py-1.5 rounded-[--radius-DEFAULT]"
          >
            <Avatar name={member.anonymousName} size={24} online={member.isOnline} />
            <span
              className="text-[13px] truncate"
              style={{ color: member.isOnline ? "var(--zc-text)" : "var(--zc-text-3)" }}
            >
              {member.anonymousName}
            </span>
          </div>
        ))}
      </div>
    </section>
  );
}
