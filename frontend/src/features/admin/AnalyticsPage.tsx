import { useState } from "react";
import { ErrorState, UnavailableState } from "../../components/feedback";
import { Badge, Card, CardHeader, Select } from "../../components/ui";
import { formatCount } from "../../lib/format";
import { adminApi } from "../../services/api";
import type { RoomHealth } from "../../types/api";
import { ChartCard, DailySeries, HourlyBars, RankedBars } from "./charts";
import { useAnalytics } from "./useAdmin";

/**
 * Analytics.
 *
 * Each panel names the service it reads from, because "where does this number come
 * from?" is the question an admin actually has. Panels whose data crosses a service
 * boundary degrade to "unavailable" rather than to zero.
 */
export function AnalyticsPage() {
  const [days, setDays] = useState(30);

  const messages = useAnalytics("messages-per-day", () => adminApi.analytics.messagesPerDay(days), days);
  const hourly = useAnalytics("messages-per-hour", () => adminApi.analytics.messagesPerHour());
  const directMessages = useAnalytics(
    "direct-messages-per-day",
    () => adminApi.analytics.directMessagesPerDay(days),
    days,
  );
  const polls = useAnalytics("polls-per-day", () => adminApi.analytics.pollsPerDay(days), days);
  const notifications = useAnalytics(
    "notifications-per-day",
    () => adminApi.analytics.notificationsPerDay(days),
    days,
  );
  const reports = useAnalytics("reports-per-day", () => adminApi.analytics.reportsPerDay(days), days);

  const topRooms = useAnalytics("top-rooms", () => adminApi.analytics.topRooms(8), 8);
  const topAuthors = useAnalytics("top-authors", () => adminApi.analytics.topAuthors(8), 8);
  const topPolls = useAnalytics("top-polls", () => adminApi.analytics.topPolls(8), 8);
  const reportReasons = useAnalytics("report-reasons", () => adminApi.analytics.reportReasons(8), 8);
  const roomHealth = useAnalytics("room-health", () => adminApi.analytics.roomHealth(10), 10);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <p className="text-[13px] text-muted">
          Counted directly from each service's MongoDB collections.
        </p>

        <Select
          value={days}
          onChange={(e) => setDays(Number(e.target.value))}
          aria-label="Time range"
          className="w-auto h-9 text-[13px]"
        >
          <option value={7}>Last 7 days</option>
          <option value={30}>Last 30 days</option>
          <option value={90}>Last 90 days</option>
        </Select>
      </div>

      <section className="grid gap-4 lg:grid-cols-2">
        <ChartCard
          title="Channel messages"
          description="Source: Chat service — messages collection."
          query={messages}
        >
          {(data) => <DailySeries data={data} label="Messages" />}
        </ChartCard>

        <ChartCard
          title="Direct messages"
          description="Source: Private chat service — directMessages collection."
          query={directMessages}
        >
          {(data) => (
            <DailySeries data={data} label="Messages" color="var(--zc-room-branch)" />
          )}
        </ChartCard>

        <ChartCard
          title="When people are talking"
          description="Messages by hour of day, all time. Source: Chat service."
          query={hourly}
        >
          {(data) => <HourlyBars data={data} />}
        </ChartCard>

        <ChartCard
          title="Reports"
          description="Source: Admin service — reports collection."
          query={reports}
        >
          {(data) => <DailySeries data={data} label="Reports" color="var(--zc-warning)" />}
        </ChartCard>

        <ChartCard
          title="Polls created"
          description="Source: Poll service — polls collection."
          query={polls}
        >
          {(data) => <DailySeries data={data} label="Polls" color="var(--zc-info)" />}
        </ChartCard>

        <ChartCard
          title="Notifications sent"
          description="Source: Notification service — notifications collection."
          query={notifications}
        >
          {(data) => <DailySeries data={data} label="Notifications" color="var(--zc-success)" />}
        </ChartCard>

        <ChartCard
          title="Busiest channels"
          description="By message count. Source: Chat service."
          query={topRooms}
          height={250}
        >
          {(data) => (
            <RankedBars
              // The endpoint returns roomName/messageCount; the chart speaks name/count.
              data={data.map((room) => ({ name: room.roomName, count: room.messageCount }))}
              label="Messages"
            />
          )}
        </ChartCard>

        <ChartCard
          title="Most active people"
          description="By message count, under their anonymous names. Source: Chat service."
          query={topAuthors}
          height={250}
        >
          {(data) => <RankedBars data={data} label="Messages" color="var(--zc-room-branch)" />}
        </ChartCard>

        <ChartCard
          title="Most-voted polls"
          description="By total votes. Source: Poll service."
          query={topPolls}
          height={250}
        >
          {(data) => <RankedBars data={data} label="Votes" color="var(--zc-info)" />}
        </ChartCard>

        <ChartCard
          title="Why people report"
          description="Most common report reasons. Source: Admin service."
          query={reportReasons}
          height={250}
          emptyLabel="No reports have been submitted."
        >
          {(data) => <RankedBars data={data} label="Reports" color="var(--zc-warning)" />}
        </ChartCard>
      </section>

      <Card>
        <CardHeader
          title="Channel health"
          description="Reports per message, joining Chat activity with the report counts held by the Admin service."
        />

        {roomHealth.isLoading ? (
          <div className="zc-skeleton h-40 w-full" aria-hidden />
        ) : roomHealth.error ? (
          <ErrorState error={roomHealth.error} onRetry={() => void roomHealth.refetch()} compact />
        ) : !roomHealth.data?.isAvailable ? (
          <UnavailableState reason={roomHealth.data?.reason} />
        ) : (roomHealth.data.value?.length ?? 0) === 0 ? (
          <p className="text-[13px] text-faint py-6 text-center">No channel activity yet.</p>
        ) : (
          <div className="zc-scroll-x">
            <table className="w-full text-left border-collapse min-w-[520px]">
              <thead>
                <tr className="border-b border-line">
                  <Th>Channel</Th>
                  <Th align="right">Messages</Th>
                  <Th align="right">Reports</Th>
                  <Th align="right">Rate</Th>
                  <Th align="right">Health</Th>
                </tr>
              </thead>
              <tbody>
                {roomHealth.data.value!.map((room) => (
                  <HealthRow key={room.roomId} room={room} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}

function HealthRow({ room }: { room: RoomHealth }) {
  const tone =
    room.health === "Critical" ? "danger" : room.health === "Monitor" ? "warning" : "success";

  return (
    <tr className="border-b border-line-subtle last:border-0">
      <Td>{room.roomName}</Td>
      <Td align="right">{formatCount(room.messageCount)}</Td>
      <Td align="right">{formatCount(room.reportCount)}</Td>
      <Td align="right">{(room.reportRate * 100).toFixed(1)}%</Td>
      <Td align="right">
        <Badge tone={tone}>{room.health}</Badge>
      </Td>
    </tr>
  );
}

function Th({ children, align }: { children: React.ReactNode; align?: "right" }) {
  return (
    <th
      className="py-2 px-2 text-[11.5px] font-semibold uppercase tracking-[0.05em] text-faint"
      style={{ textAlign: align ?? "left" }}
      scope="col"
    >
      {children}
    </th>
  );
}

function Td({ children, align }: { children: React.ReactNode; align?: "right" }) {
  return (
    <td
      className="py-2.5 px-2 text-[13px] text-body zc-tabular"
      style={{ textAlign: align ?? "left" }}
    >
      {children}
    </td>
  );
}
