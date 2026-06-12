import { useEffect, useState } from "react";
import {
    BarChart,
    Bar,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
    Cell,
    LabelList,
} from "recharts";
import { RefreshCw, AlertCircle, RotateCcw } from "lucide-react";
import {
    getActiveRoomsTyped,
    getRoomHealth,
    getPollParticipation,
    getHourlyActivity,
    getRoomSentiment,
} from "../../api/adminApi";
import type {
    RoomHealth,
    PollParticipation,
    HourlyActivity,
    RoomSentiment,
} from "../../api/adminApi";

// ── Palette ──────────────────────────────────────────────────────────────────

const BLUE   = "#0EA5E9";
const TEAL   = "#06B6D4";
const GREEN  = "#22C55E";
const AMBER  = "#F59E0B";
const RED    = "#EF4444";
const PURPLE = "#8B5CF6";
const GREY   = "#94A3B8";

// ── Shared UI helpers ─────────────────────────────────────────────────────────

const CARD_STYLE: React.CSSProperties = {
    background: "rgba(15,23,42,0.7)",
    border: "1px solid rgba(255,255,255,0.06)",
};

const TOOLTIP_STYLE = {
    contentStyle: {
        background: "#0f172a",
        border: "1px solid rgba(255,255,255,0.1)",
        borderRadius: "12px",
        color: "#f1f5f9",
        fontSize: 12,
    },
    labelStyle: { color: "#94a3b8" },
};

const TICK_STYLE = { fill: "#475569", fontSize: 11 };
const Y_TICK_STYLE = { fill: "#94a3b8", fontSize: 11 };

// ── Small sub-components ──────────────────────────────────────────────────────

function InsightLabel({ text }: { text: string }) {
    return (
        <p className="text-slate-500 text-xs italic leading-relaxed mb-4">{text}</p>
    );
}

function LoadingSkeleton() {
    return (
        <div className="space-y-2 animate-pulse py-4">
            {[80, 65, 50, 40, 30].map((w, i) => (
                <div key={i} className="flex items-center gap-3">
                    <div
                        className="h-5 rounded"
                        style={{ width: `${w}%`, background: "rgba(255,255,255,0.06)" }}
                    />
                </div>
            ))}
        </div>
    );
}

function ErrorState({ onRetry }: { onRetry: () => void }) {
    return (
        <div className="flex flex-col items-center justify-center gap-3 py-8 text-center">
            <AlertCircle size={24} className="text-red-400" />
            <p className="text-slate-500 text-sm">Could not load data</p>
            <button
                onClick={onRetry}
                className="flex items-center gap-1.5 text-xs text-cyan-400 hover:text-cyan-300 transition-colors"
            >
                <RotateCcw size={12} />
                Retry
            </button>
        </div>
    );
}

function EmptyState() {
    return (
        <div className="flex items-center justify-center py-10 text-slate-600 text-sm">
            No data yet
        </div>
    );
}

interface ChartCardProps {
    title: string;
    insight: string;
    loading: boolean;
    error: boolean;
    empty: boolean;
    onRetry: () => void;
    children: React.ReactNode;
    fullWidth?: boolean;
}

function ChartCard({ title, insight, loading, error, empty, onRetry, children }: ChartCardProps) {
    return (
        <div className="rounded-2xl p-5" style={CARD_STYLE}>
            <h3 className="text-sm font-bold text-white mb-1">{title}</h3>
            <InsightLabel text={insight} />
            {loading ? (
                <LoadingSkeleton />
            ) : error ? (
                <ErrorState onRetry={onRetry} />
            ) : empty ? (
                <EmptyState />
            ) : (
                children
            )}
        </div>
    );
}

// ── Hour label helper (0 → 12am, 9 → 9am, 13 → 1pm) ─────────────────────────

function hourLabel(h: number): string {
    if (h === 0)  return "12am";
    if (h < 12)   return `${h}am`;
    if (h === 12) return "12pm";
    return `${h - 12}pm`;
}

function hourColor(h: number): string {
    if (h >= 6  && h < 12) return BLUE;    // Morning
    if (h >= 12 && h < 18) return GREEN;   // Afternoon
    if (h >= 18 && h < 22) return AMBER;   // Evening
    return RED;                             // Night (22–24, 0–6)
}

// ── Health color ──────────────────────────────────────────────────────────────

function healthColor(health: string): string {
    if (health === "Healthy")  return GREEN;
    if (health === "Monitor")  return AMBER;
    return RED;
}

// ── Custom sentiment tooltip ──────────────────────────────────────────────────

const SentimentTooltip = ({ active, payload, label }: {
    active?: boolean;
    payload?: { name: string; value: number; fill: string }[];
    label?: string;
}) => {
    if (!active || !payload?.length) return null;
    return (
        <div
            className="px-3 py-2 rounded-xl text-xs space-y-1"
            style={{
                background: "#0f172a",
                border: "1px solid rgba(255,255,255,0.1)",
            }}
        >
            <p className="text-slate-300 font-semibold mb-1.5">{label}</p>
            {payload.map((p) => (
                <div key={p.name} className="flex items-center gap-2">
                    <span
                        className="w-2 h-2 rounded-full inline-block"
                        style={{ background: p.fill }}
                    />
                    <span style={{ color: p.fill }}>{p.name}:</span>
                    <span className="text-white">{p.value}%</span>
                </div>
            ))}
        </div>
    );
};

// ── Chart 3 custom label (votes on top of each bar) ──────────────────────────

const PollVoteLabel = (props: {
    x?: number; y?: number; width?: number; value?: number;
}) => {
    const { x = 0, y = 0, width = 0, value } = props;
    return (
        <text
            x={x + width / 2}
            y={y - 4}
            fill="#94a3b8"
            textAnchor="middle"
            fontSize={10}
            fontWeight="600"
        >
            {value}
        </text>
    );
};

// ── Main component ────────────────────────────────────────────────────────────

type ChartStatus = { loading: boolean; error: boolean };

export default function AdminAnalyticsPage() {
    const [activeRooms, setActiveRooms] = useState<{ roomId: string; roomName: string; messageCount: number }[]>([]);
    const [roomHealth, setRoomHealth]   = useState<RoomHealth[]>([]);
    const [polls, setPolls]             = useState<PollParticipation[]>([]);
    const [hourly, setHourly]           = useState<HourlyActivity[]>([]);
    const [sentiment, setSentiment]     = useState<RoomSentiment[]>([]);

    const [status, setStatus] = useState<Record<string, ChartStatus>>({
        rooms:     { loading: true, error: false },
        health:    { loading: true, error: false },
        polls:     { loading: true, error: false },
        hourly:    { loading: true, error: false },
        sentiment: { loading: true, error: false },
    });

    const setChartStatus = (key: string, s: Partial<ChartStatus>) =>
        setStatus((prev) => ({ ...prev, [key]: { ...prev[key], ...s } }));

    const loadRooms = async () => {
        setChartStatus("rooms", { loading: true, error: false });
        try {
            const d = await getActiveRoomsTyped(8);
            setActiveRooms(d);
            setChartStatus("rooms", { loading: false, error: false });
        } catch {
            setChartStatus("rooms", { loading: false, error: true });
        }
    };

    const loadHealth = async () => {
        setChartStatus("health", { loading: true, error: false });
        try {
            const d = await getRoomHealth(10);
            setRoomHealth(d);
            setChartStatus("health", { loading: false, error: false });
        } catch {
            setChartStatus("health", { loading: false, error: true });
        }
    };

    const loadPolls = async () => {
        setChartStatus("polls", { loading: true, error: false });
        try {
            const d = await getPollParticipation(6);
            setPolls(d);
            setChartStatus("polls", { loading: false, error: false });
        } catch {
            setChartStatus("polls", { loading: false, error: true });
        }
    };

    const loadHourly = async () => {
        setChartStatus("hourly", { loading: true, error: false });
        try {
            const d = await getHourlyActivity();
            setHourly(d);
            setChartStatus("hourly", { loading: false, error: false });
        } catch {
            setChartStatus("hourly", { loading: false, error: true });
        }
    };

    const loadSentiment = async () => {
        setChartStatus("sentiment", { loading: true, error: false });
        try {
            const d = await getRoomSentiment(8);
            setSentiment(d);
            setChartStatus("sentiment", { loading: false, error: false });
        } catch {
            setChartStatus("sentiment", { loading: false, error: true });
        }
    };

    const loadAll = () => {
        // Use Promise.allSettled: one failure does not block others
        Promise.allSettled([
            loadRooms(),
            loadHealth(),
            loadPolls(),
            loadHourly(),
            loadSentiment(),
        ]);
    };

    useEffect(() => { loadAll(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

    const anyLoading = Object.values(status).some((s) => s.loading);

    // Prepare Chart 3 data (truncate poll question to 30 chars)
    const pollChartData = polls.map((p) => ({
        name: p.pollQuestion.length > 30 ? p.pollQuestion.slice(0, 30) + "…" : p.pollQuestion,
        votes: p.totalVotes,
        rate:  p.participationRate,
    }));

    // Prepare Chart 4 data
    const hourlyChartData = hourly.map((h) => ({
        label:        hourLabel(h.hour),
        hour:         h.hour,
        messageCount: h.messageCount,
    }));

    return (
        <div className="p-6 space-y-5">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white">Analytics</h1>
                    <p className="text-sm text-slate-400 mt-0.5">
                        Organizational insights — what employees are discussing and feeling
                    </p>
                </div>
                <button
                    onClick={loadAll}
                    disabled={anyLoading}
                    className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50"
                >
                    <RefreshCw size={14} className={anyLoading ? "animate-spin" : ""} />
                    Refresh
                </button>
            </div>

            {/* Legend for Hourly Activity */}
            <div className="flex items-center gap-4 flex-wrap">
                {[
                    { label: "Morning (6am–12pm)", color: BLUE },
                    { label: "Afternoon (12pm–6pm)", color: GREEN },
                    { label: "Evening (6pm–10pm)", color: AMBER },
                    { label: "Night (10pm–6am)", color: RED },
                ].map(({ label, color }) => (
                    <div key={label} className="flex items-center gap-1.5 text-xs text-slate-400">
                        <span className="w-2.5 h-2.5 rounded-full inline-block" style={{ background: color }} />
                        {label}
                    </div>
                ))}
            </div>

            {/* Row 1: Chart 1 | Chart 2 */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Chart 1 — Most Active Rooms */}
                <ChartCard
                    title="Most Active Rooms"
                    insight="Rooms with high message counts reveal what employees are actively discussing. High activity in topic rooms like HR Issues or Management signals these are areas of concern or interest."
                    loading={status.rooms.loading}
                    error={status.rooms.error}
                    empty={!status.rooms.loading && !status.rooms.error && activeRooms.length === 0}
                    onRetry={loadRooms}
                >
                    <ResponsiveContainer width="100%" height={250}>
                        <BarChart
                            data={activeRooms.map((r) => ({ name: r.roomName, messages: r.messageCount }))}
                            layout="vertical"
                            margin={{ left: 8, right: 20, top: 4, bottom: 4 }}
                        >
                            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                            <XAxis type="number" tick={TICK_STYLE} tickLine={false} axisLine={false} />
                            <YAxis
                                dataKey="name"
                                type="category"
                                tick={Y_TICK_STYLE}
                                tickLine={false}
                                axisLine={false}
                                width={110}
                            />
                            <Tooltip {...TOOLTIP_STYLE} />
                            <Bar dataKey="messages" name="Messages" radius={[0, 4, 4, 0]}>
                                {activeRooms.map((_, i) => (
                                    <Cell key={i} fill={i % 2 === 0 ? TEAL : BLUE} />
                                ))}
                            </Bar>
                        </BarChart>
                    </ResponsiveContainer>
                </ChartCard>

                {/* Chart 2 — Room Health Index */}
                <ChartCard
                    title="Room Health Index"
                    insight="Room Health reveals where conversations are turning toxic or controversial. Red rooms need immediate moderation attention. High reports in a specific branch room may indicate inter-team conflict."
                    loading={status.health.loading}
                    error={status.health.error}
                    empty={!status.health.loading && !status.health.error && roomHealth.length === 0}
                    onRetry={loadHealth}
                >
                    <div className="flex items-center gap-4 mb-3">
                        {(["Healthy", "Monitor", "Critical"] as const).map((h) => (
                            <div key={h} className="flex items-center gap-1.5 text-xs text-slate-400">
                                <span className="w-2 h-2 rounded-full" style={{ background: healthColor(h) }} />
                                {h} {h === "Healthy" ? "(<1%)" : h === "Monitor" ? "(1–5%)" : "(>5%)"}
                            </div>
                        ))}
                    </div>
                    <ResponsiveContainer width="100%" height={220}>
                        <BarChart
                            data={roomHealth.map((r) => ({ name: r.roomName, rate: r.reportRate, health: r.health }))}
                            layout="vertical"
                            margin={{ left: 8, right: 40, top: 4, bottom: 4 }}
                        >
                            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                            <XAxis type="number" tick={TICK_STYLE} tickLine={false} axisLine={false} unit="%" domain={[0, "dataMax + 1"]} />
                            <YAxis
                                dataKey="name"
                                type="category"
                                tick={Y_TICK_STYLE}
                                tickLine={false}
                                axisLine={false}
                                width={110}
                            />
                            <Tooltip
                                {...TOOLTIP_STYLE}
                                formatter={(value: number, _name: string, props: { payload?: { health?: string } }) => [
                                    `${value.toFixed(1)}% (${props?.payload?.health ?? ""})`,
                                    "Report Rate",
                                ]}
                            />
                            <Bar dataKey="rate" name="Report Rate" radius={[0, 4, 4, 0]}>
                                {roomHealth.map((r, i) => (
                                    <Cell key={i} fill={healthColor(r.health)} />
                                ))}
                                <LabelList
                                    dataKey="rate"
                                    position="right"
                                    formatter={(v: number) => `${v.toFixed(1)}%`}
                                    style={{ fill: "#64748b", fontSize: 10 }}
                                />
                            </Bar>
                        </BarChart>
                    </ResponsiveContainer>
                </ChartCard>
            </div>

            {/* Row 2: Chart 3 | Chart 4 */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Chart 3 — Poll Participation by Topic */}
                <ChartCard
                    title="Poll Participation by Topic"
                    insight="Poll participation reveals which organizational questions employees care enough to answer. High participation on sensitive topics like workload or management shows strong collective sentiment worth addressing."
                    loading={status.polls.loading}
                    error={status.polls.error}
                    empty={!status.polls.loading && !status.polls.error && polls.length === 0}
                    onRetry={loadPolls}
                >
                    <ResponsiveContainer width="100%" height={270}>
                        <BarChart
                            data={pollChartData}
                            margin={{ left: 4, right: 10, top: 20, bottom: 60 }}
                        >
                            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                            <XAxis
                                dataKey="name"
                                tick={{ fill: "#475569", fontSize: 9 }}
                                tickLine={false}
                                axisLine={false}
                                angle={-30}
                                textAnchor="end"
                                interval={0}
                                height={60}
                            />
                            <YAxis tick={TICK_STYLE} tickLine={false} axisLine={false} />
                            <Tooltip
                                {...TOOLTIP_STYLE}
                                formatter={(value: number) => [value, "Votes"]}
                            />
                            <Bar dataKey="votes" name="Votes" radius={[4, 4, 0, 0]} fill={PURPLE}>
                                <LabelList content={<PollVoteLabel />} />
                            </Bar>
                        </BarChart>
                    </ResponsiveContainer>

                    {/* Participation rate badges */}
                    {polls.length > 0 && (
                        <div className="flex flex-wrap gap-2 mt-2">
                            {polls.map((p, i) => (
                                <div
                                    key={i}
                                    className="px-2 py-0.5 rounded-full text-xs font-medium"
                                    style={{
                                        background: "rgba(139,92,246,0.12)",
                                        border: "1px solid rgba(139,92,246,0.3)",
                                        color: "#a78bfa",
                                    }}
                                    title={p.pollQuestion}
                                >
                                    {p.participationRate}% participation
                                </div>
                            ))}
                        </div>
                    )}
                </ChartCard>

                {/* Chart 4 — Message Volume by Hour */}
                <ChartCard
                    title="Message Volume by Hour of Day"
                    insight="Message timing reveals your organization's real communication patterns. High activity outside work hours may signal deadline pressure, understaffing, or employees feeling unable to speak up during the day."
                    loading={status.hourly.loading}
                    error={status.hourly.error}
                    empty={!status.hourly.loading && !status.hourly.error && hourly.every((h) => h.messageCount === 0)}
                    onRetry={loadHourly}
                >
                    <ResponsiveContainer width="100%" height={240}>
                        <BarChart
                            data={hourlyChartData}
                            margin={{ left: 4, right: 8, top: 4, bottom: 4 }}
                        >
                            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                            <XAxis
                                dataKey="label"
                                tick={{ fill: "#475569", fontSize: 9 }}
                                tickLine={false}
                                axisLine={false}
                                interval={2}
                            />
                            <YAxis tick={TICK_STYLE} tickLine={false} axisLine={false} />
                            <Tooltip
                                {...TOOLTIP_STYLE}
                                formatter={(value: number) => [value, "Messages"]}
                                labelFormatter={(label) => `Hour: ${label}`}
                            />
                            <Bar dataKey="messageCount" name="Messages" radius={[2, 2, 0, 0]}>
                                {hourlyChartData.map((h, i) => (
                                    <Cell key={i} fill={hourColor(h.hour)} />
                                ))}
                            </Bar>
                        </BarChart>
                    </ResponsiveContainer>
                </ChartCard>
            </div>

            {/* Row 3: Chart 5 — Sentiment (full width) */}
            <ChartCard
                title="Sentiment Distribution by Room"
                insight="Sentiment analysis shows the emotional tone of each room without reading individual messages. A room with 40% negative sentiment is telling management something important about that group's experience — anonymously."
                loading={status.sentiment.loading}
                error={status.sentiment.error}
                empty={!status.sentiment.loading && !status.sentiment.error && sentiment.length === 0}
                onRetry={loadSentiment}
                fullWidth
            >
                <div className="flex items-center gap-5 mb-4">
                    {[
                        { label: "Positive", color: GREEN },
                        { label: "Neutral", color: GREY },
                        { label: "Negative", color: RED },
                    ].map(({ label, color }) => (
                        <div key={label} className="flex items-center gap-1.5 text-xs text-slate-400">
                            <span className="w-3 h-3 rounded-sm" style={{ background: color }} />
                            {label}
                        </div>
                    ))}
                </div>
                <ResponsiveContainer width="100%" height={Math.max(200, sentiment.length * 40 + 40)}>
                    <BarChart
                        data={sentiment}
                        layout="vertical"
                        margin={{ left: 8, right: 20, top: 4, bottom: 4 }}
                        stackOffset="expand"
                    >
                        <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                        <XAxis
                            type="number"
                            domain={[0, 100]}
                            tickFormatter={(v: number) => `${v}%`}
                            tick={TICK_STYLE}
                            tickLine={false}
                            axisLine={false}
                        />
                        <YAxis
                            dataKey="roomName"
                            type="category"
                            tick={Y_TICK_STYLE}
                            tickLine={false}
                            axisLine={false}
                            width={120}
                        />
                        <Tooltip content={<SentimentTooltip />} />
                        <Bar dataKey="positive" name="Positive" stackId="s" fill={GREEN} radius={[0, 0, 0, 0]}>
                            <LabelList
                                dataKey="positive"
                                position="inside"
                                formatter={(v: number) => (v >= 8 ? `${v}%` : "")}
                                style={{ fill: "#fff", fontSize: 10, fontWeight: 600 }}
                            />
                        </Bar>
                        <Bar dataKey="neutral" name="Neutral" stackId="s" fill={GREY}>
                            <LabelList
                                dataKey="neutral"
                                position="inside"
                                formatter={(v: number) => (v >= 8 ? `${v}%` : "")}
                                style={{ fill: "#fff", fontSize: 10, fontWeight: 600 }}
                            />
                        </Bar>
                        <Bar dataKey="negative" name="Negative" stackId="s" fill={RED} radius={[0, 4, 4, 0]}>
                            <LabelList
                                dataKey="negative"
                                position="inside"
                                formatter={(v: number) => (v >= 8 ? `${v}%` : "")}
                                style={{ fill: "#fff", fontSize: 10, fontWeight: 600 }}
                            />
                        </Bar>
                    </BarChart>
                </ResponsiveContainer>
            </ChartCard>
        </div>
    );
}
