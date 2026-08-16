import type { UseQueryResult } from "@tanstack/react-query";
import type { ReactNode } from "react";
import {
  Area, AreaChart, Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip,
  XAxis, YAxis,
} from "recharts";
import { EmptyState, ErrorState, UnavailableState } from "../../components/feedback";
import { Card, CardHeader } from "../../components/ui";
import { formatCount, formatShortDate } from "../../lib/format";
import type { Availability } from "../../types/api";

/**
 * Analytics charts.
 *
 * Every chart on this page is backed by an endpoint that counts documents in MongoDB.
 * There is no sample data anywhere in this file, and — importantly — no chart renders a
 * flat zero line when its data could not be fetched. Cross-service figures arrive wrapped
 * in `Availability`, and an unavailable series says so.
 */

// ── Chart frame ───────────────────────────────────────────────────────────────

/**
 * Wraps a chart in its loading, error, unavailable and empty states.
 *
 * Passing the query in whole is what makes those four states impossible to forget: a
 * chart cannot be written that renders `data ?? []` and silently shows nothing.
 */
export function ChartCard<T>({
  title,
  description,
  query,
  height = 220,
  children,
  emptyLabel = "No data in this period.",
}: {
  title: string;
  description?: string;
  query: UseQueryResult<Availability<T[]> | T[]>;
  height?: number;
  children: (data: T[]) => ReactNode;
  emptyLabel?: string;
}) {
  const body = () => {
    if (query.isLoading) {
      return <div className="zc-skeleton w-full" style={{ height }} aria-hidden />;
    }

    if (query.error) {
      return <ErrorState error={query.error} onRetry={() => void query.refetch()} compact />;
    }

    const result = query.data;

    if (!result) return <UnavailableState />;

    // Series computed inside the admin database are returned bare; anything fetched
    // from another service is wrapped so a failure is distinguishable from a zero.
    const wrapped = !Array.isArray(result);

    if (wrapped && !result.isAvailable) {
      return <UnavailableState reason={result.reason} />;
    }

    const data = (wrapped ? (result.value ?? []) : result) as T[];

    if (data.length === 0) {
      return (
        <div style={{ height }} className="flex items-center justify-center">
          <EmptyState title={emptyLabel} className="!py-0" />
        </div>
      );
    }

    return <div style={{ height }}>{children(data)}</div>;
  };

  return (
    <Card>
      <CardHeader title={title} description={description} />
      {body()}
    </Card>
  );
}

// ── Shared chart config ───────────────────────────────────────────────────────

const axisStyle = {
  fontSize: 11,
  fill: "var(--zc-text-3)",
} as const;

/** Tooltips are themed through the tokens, so they are legible in dark mode too. */
/**
 * Recharts types its tooltip callbacks against every value type a chart could hold, so
 * the concrete `(value: number) => …` signatures below need narrowing at the call site.
 * These wrappers do that in one place rather than at each `<Tooltip>`.
 */
const countFormatter = (label: string) =>
  ((value: unknown) => [formatCount(Number(value)), label]) as never;

const dateLabelFormatter = ((label: unknown) => formatShortDate(String(label))) as never;

const tooltipStyle = {
  contentStyle: {
    background: "var(--zc-surface)",
    border: "1px solid var(--zc-border)",
    borderRadius: "var(--zc-radius-sm)",
    fontSize: "12.5px",
    color: "var(--zc-text)",
    boxShadow: "var(--zc-shadow)",
  },
  labelStyle: { color: "var(--zc-text-2)", fontWeight: 600, marginBottom: 2 },
  cursor: { fill: "var(--zc-surface-2)" },
} as const;

// ── Chart types ───────────────────────────────────────────────────────────────

/** A daily time series. Dates are ISO from the server and formatted locally. */
export function DailySeries({
  data,
  color = "var(--zc-accent)",
  label,
}: {
  data: Array<{ date: string; count: number }>;
  color?: string;
  label: string;
}) {
  const gradientId = `zc-gradient-${label.replace(/\W/g, "")}`;

  return (
    <ResponsiveContainer width="100%" height="100%">
      <AreaChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: -18 }}>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={color} stopOpacity={0.28} />
            <stop offset="100%" stopColor={color} stopOpacity={0.02} />
          </linearGradient>
        </defs>

        <CartesianGrid stroke="var(--zc-border-subtle)" vertical={false} />
        <XAxis
          dataKey="date"
          tickFormatter={formatShortDate}
          tick={axisStyle}
          tickLine={false}
          axisLine={{ stroke: "var(--zc-border-subtle)" }}
          minTickGap={28}
        />
        <YAxis tick={axisStyle} tickLine={false} axisLine={false} allowDecimals={false} width={44} />
        <Tooltip
          {...tooltipStyle}
          labelFormatter={dateLabelFormatter}
          formatter={countFormatter(label)}
        />
        <Area
          type="monotone"
          dataKey="count"
          stroke={color}
          strokeWidth={2}
          fill={`url(#${gradientId})`}
          // The final point is the interesting one, so it gets a visible marker.
          activeDot={{ r: 4, strokeWidth: 0 }}
          dot={false}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}

/** A ranked bar chart — top rooms, top authors, report reasons. */
export function RankedBars({
  data,
  color = "var(--zc-accent)",
  label,
}: {
  data: Array<{ name: string; count: number }>;
  color?: string;
  label: string;
}) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={data} layout="vertical" margin={{ top: 0, right: 12, bottom: 0, left: 4 }}>
        <CartesianGrid stroke="var(--zc-border-subtle)" horizontal={false} />
        <XAxis type="number" tick={axisStyle} tickLine={false} axisLine={false} allowDecimals={false} />
        <YAxis
          type="category"
          dataKey="name"
          tick={axisStyle}
          tickLine={false}
          axisLine={false}
          width={116}
          interval={0}
        />
        <Tooltip {...tooltipStyle} formatter={countFormatter(label)} />
        <Bar dataKey="count" fill={color} radius={[0, 4, 4, 0]} maxBarSize={22} />
      </BarChart>
    </ResponsiveContainer>
  );
}

/** Hourly distribution. Categorical rather than a time series — 24 fixed buckets. */
export function HourlyBars({ data }: { data: Array<{ name: string; count: number }> }) {
  const peak = Math.max(...data.map((d) => d.count));

  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: -18 }}>
        <CartesianGrid stroke="var(--zc-border-subtle)" vertical={false} />
        <XAxis
          dataKey="name"
          tick={axisStyle}
          tickLine={false}
          axisLine={{ stroke: "var(--zc-border-subtle)" }}
          interval={2}
        />
        <YAxis tick={axisStyle} tickLine={false} axisLine={false} allowDecimals={false} width={44} />
        <Tooltip {...tooltipStyle} formatter={countFormatter("Messages")} />
        <Bar dataKey="count" radius={[3, 3, 0, 0]}>
          {data.map((entry) => (
            // The busiest hour is emphasised; the rest recede. That is the one thing
            // this chart exists to show.
            <Cell
              key={entry.name}
              fill={entry.count === peak && peak > 0 ? "var(--zc-accent)" : "var(--zc-border-strong)"}
            />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
