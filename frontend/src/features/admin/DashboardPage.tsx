import {
  AlertTriangle, Ban, Bell, Hash, MessageSquare, MessagesSquare, ShieldCheck, Users, Vote,
} from "lucide-react";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { ErrorState, Skeleton, UnavailableState } from "../../components/feedback";
import { Badge, Button, Card, CardHeader } from "../../components/ui";
import { paths } from "../../config";
import { formatCount, formatRelative } from "../../lib/format";
import { humaniseAction } from "../../lib/messages";
import { adminApi } from "../../services/api";
import type { Availability, AuditLogEntry } from "../../types/api";
import { ChartCard, DailySeries } from "./charts";
import { useAnalytics, useDashboardStats, useRecentActivity } from "./useAdmin";

/**
 * The overview.
 *
 * Each tile traces to one figure the backend actually computes:
 *
 *   People            → Auth: users collection, filtered on isDeleted
 *   Channels          → Chat: rooms collection
 *   Messages          → Chat: messages collection
 *   Conversations/DMs → PrivateChat: conversations and directMessages collections
 *   Polls             → Poll: polls collection
 *   Notifications     → Notification: notifications collection
 *   Reports / blocked → Admin: reports and blockedUsers collections
 *
 * The first six cross a service boundary and are wrapped in `Availability`, so a tile
 * shows "Unavailable" when its owning service could not be reached — never a zero.
 */
export function DashboardPage() {
  const stats = useDashboardStats();
  const activity = useRecentActivity(10);

  const messages = useAnalytics("messages-per-day", () => adminApi.analytics.messagesPerDay(30), 30);
  const reports = useAnalytics("reports-per-day", () => adminApi.analytics.reportsPerDay(30), 30);

  if (stats.error) {
    return <ErrorState error={stats.error} onRetry={() => void stats.refetch()} />;
  }

  const data = stats.data;
  const pending = data?.pendingReports ?? 0;

  return (
    <div className="flex flex-col gap-5">
      {pending > 0 && (
        <div className="flex items-center gap-3 p-3.5 rounded-[--radius-lg] bg-warning-soft border border-warning/25">
          <AlertTriangle size={18} className="text-warning shrink-0" />
          <p className="text-[13.5px] text-body flex-1 min-w-0">
            <span className="font-medium">
              {pending} report{pending === 1 ? "" : "s"} awaiting review
            </span>
            <span className="text-muted"> — reported content stays visible until actioned.</span>
          </p>
          <Link to={paths.admin.moderation}>
            <Button size="sm" variant="secondary">
              Review
            </Button>
          </Link>
        </div>
      )}

      <section
        className="grid gap-3 grid-cols-2 lg:grid-cols-4"
        aria-label="Platform totals"
      >
        <Tile
          icon={<Users size={16} />}
          label="Active people"
          value={data?.activeUsers}
          loading={stats.isLoading}
          footnote={
            data?.deletedUsers?.isAvailable && (data.deletedUsers.value ?? 0) > 0
              ? `${formatCount(data.deletedUsers.value ?? 0)} deleted`
              : undefined
          }
        />
        <Tile
          icon={<Hash size={16} />}
          label="Channels"
          value={data?.totalRooms}
          loading={stats.isLoading}
        />
        <Tile
          icon={<MessageSquare size={16} />}
          label="Channel messages"
          value={data?.totalMessages}
          loading={stats.isLoading}
        />
        <Tile
          icon={<MessagesSquare size={16} />}
          label="Direct messages"
          value={data?.totalDirectMessages}
          loading={stats.isLoading}
          footnote={
            data?.totalConversations?.isAvailable
              ? `across ${formatCount(data.totalConversations.value ?? 0)} conversations`
              : undefined
          }
        />
        <Tile
          icon={<Vote size={16} />}
          label="Polls"
          value={data?.totalPolls}
          loading={stats.isLoading}
        />
        <Tile
          icon={<Bell size={16} />}
          label="Notifications sent"
          value={data?.totalNotifications}
          loading={stats.isLoading}
        />
        <Tile
          icon={<ShieldCheck size={16} />}
          label="Reports"
          // Local to the admin database, so never unavailable.
          value={data?.totalReports}
          loading={stats.isLoading}
          footnote={pending > 0 ? `${pending} pending` : "none pending"}
          tone={pending > 0 ? "warning" : undefined}
        />
        <Tile
          icon={<Ban size={16} />}
          label="Blocked accounts"
          value={data?.blockedUsers}
          loading={stats.isLoading}
        />
      </section>

      <section className="grid gap-4 lg:grid-cols-2">
        <ChartCard
          title="Messages per day"
          description="Channel messages, last 30 days. Source: Chat service."
          query={messages}
        >
          {(series) => <DailySeries data={series} label="Messages" />}
        </ChartCard>

        <ChartCard
          title="Reports per day"
          description="Moderation reports, last 30 days. Source: Admin service."
          query={reports}
        >
          {(series) => (
            <DailySeries data={series} label="Reports" color="var(--zc-warning)" />
          )}
        </ChartCard>
      </section>

      <Card>
        <CardHeader
          title="Recent administrative activity"
          description="Every moderator and automated action, newest first."
          action={
            <Link to={paths.admin.audit}>
              <Button size="sm" variant="ghost">
                Full audit log
              </Button>
            </Link>
          }
        />

        {activity.isLoading ? (
          <div className="flex flex-col gap-2">
            <Skeleton className="h-10 rounded-[--radius-sm]" count={5} />
          </div>
        ) : activity.error ? (
          <ErrorState error={activity.error} onRetry={() => void activity.refetch()} compact />
        ) : (activity.data?.length ?? 0) === 0 ? (
          <p className="text-[13px] text-faint py-6 text-center">
            Nothing has been actioned yet.
          </p>
        ) : (
          <ul className="flex flex-col divide-y divide-line-subtle -my-1.5">
            {activity.data!.map((entry) => (
              <ActivityRow key={entry.id} entry={entry} />
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}

/**
 * One dashboard figure.
 *
 * Accepts either a plain number (computed locally) or an `Availability` (fetched from
 * another service). That is the distinction the tile has to preserve: a service that is
 * down must not read as "zero messages".
 */
function Tile({
  icon,
  label,
  value,
  loading,
  footnote,
  tone,
}: {
  icon: ReactNode;
  label: string;
  value: number | Availability<number> | undefined;
  loading: boolean;
  footnote?: string | false;
  tone?: "warning";
}) {
  const render = () => {
    if (loading) return <Skeleton className="h-7 w-20 mt-1" />;

    if (value === undefined) return <UnavailableState compact />;

    if (typeof value === "number") {
      return <span className="text-[24px] font-semibold text-body zc-tabular">{formatCount(value)}</span>;
    }

    if (!value.isAvailable) return <UnavailableState compact reason={value.reason} />;

    return (
      <span className="text-[24px] font-semibold text-body zc-tabular">
        {formatCount(value.value ?? 0)}
      </span>
    );
  };

  return (
    <Card padded={false} className="p-4">
      <div className="flex items-center gap-2 text-faint">
        <span style={tone === "warning" ? { color: "var(--zc-warning)" } : undefined}>{icon}</span>
        <span className="text-[12px] font-medium uppercase tracking-[0.05em] truncate">
          {label}
        </span>
      </div>

      <div className="mt-1.5 leading-none">{render()}</div>

      {footnote && <p className="text-[11.5px] text-faint mt-1.5 truncate">{footnote}</p>}
    </Card>
  );
}

function ActivityRow({ entry }: { entry: AuditLogEntry }) {
  return (
    <li className="flex items-start gap-3 py-2.5">
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-[13px] font-medium text-body">{humaniseAction(entry.action)}</span>
          {/* Automated moderation is visually distinct from a person's decision. */}
          <Badge tone={entry.isSystem ? "warning" : "neutral"}>
            {entry.isSystem ? "Automated" : entry.actorName}
          </Badge>
        </div>

        {entry.details && (
          <p className="text-[12.5px] text-muted mt-0.5 truncate">{entry.details}</p>
        )}
      </div>

      <time
        dateTime={entry.timestamp}
        className="text-[11.5px] text-faint shrink-0 pt-0.5 zc-tabular"
      >
        {formatRelative(entry.timestamp)}
      </time>
    </li>
  );
}
