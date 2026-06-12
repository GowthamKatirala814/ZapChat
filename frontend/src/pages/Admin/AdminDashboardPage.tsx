import { useEffect, useState } from "react";
import {
    Users, UserCheck, UserX, Building2, Flag, AlertCircle, Clock, RefreshCw
} from "lucide-react";
import { getDashboardStats, getRecentActivity } from "../../api/adminApi";
import type { DashboardStats, RecentActivity } from "../../api/adminApi";

interface StatCard {
    label: string;
    value: number;
    icon: React.ElementType;
    color: string;
    bg: string;
}

function StatCard({ label, value, icon: Icon, color, bg }: StatCard) {
    return (
        <div className="rounded-2xl p-5 border border-slate-800 flex items-center gap-4"
            style={{ background: "rgba(15,23,42,0.7)" }}>
            <div className="w-12 h-12 rounded-xl flex items-center justify-center shrink-0" style={{ background: bg }}>
                <Icon size={22} style={{ color }} />
            </div>
            <div>
                <p className="text-2xl font-bold text-white">{value.toLocaleString()}</p>
                <p className="text-xs text-slate-400 mt-0.5">{label}</p>
            </div>
        </div>
    );
}

function activityIcon(type: string) {
    if (type.toLowerCase().includes("delete")) return "🗑️";
    if (type.toLowerCase().includes("room")) return "🏠";
    if (type.toLowerCase().includes("report")) return "🚩";
    if (type.toLowerCase().includes("threshold")) return "⚙️";
    return "📋";
}

export default function AdminDashboardPage() {
    const [stats, setStats] = useState<DashboardStats | null>(null);
    const [activity, setActivity] = useState<RecentActivity[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const [s, a] = await Promise.all([getDashboardStats(), getRecentActivity(15)]);
            setStats(s);
            setActivity(a);
        } catch {
            setError("Failed to load dashboard data. Ensure the Admin Service is running.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, []);

    const cards: StatCard[] = stats ? [
        { label: "Total Users", value: stats.totalUsers, icon: Users, color: "#60a5fa", bg: "rgba(96,165,250,0.12)" },
        { label: "Active Users", value: stats.activeUsers, icon: UserCheck, color: "#34d399", bg: "rgba(52,211,153,0.12)" },
        { label: "Deleted Users", value: stats.deletedUsers, icon: UserX, color: "#f87171", bg: "rgba(248,113,113,0.12)" },
        { label: "Chat Rooms", value: stats.totalChatRooms, icon: Building2, color: "#a78bfa", bg: "rgba(167,139,250,0.12)" },
        { label: "Total Reports", value: stats.totalReports, icon: Flag, color: "#f472b6", bg: "rgba(244,114,182,0.12)" },
        { label: "Pending Reports", value: stats.pendingReports, icon: AlertCircle, color: "#ef4444", bg: "rgba(239,68,68,0.15)" },
    ] : [];

    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white">Dashboard</h1>
                    <p className="text-sm text-slate-400 mt-0.5">Platform overview at a glance</p>
                </div>
                <button
                    onClick={load}
                    disabled={loading}
                    className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50"
                >
                    <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
                    Refresh
                </button>
            </div>

            {/* Error */}
            {error && (
                <div className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm"
                    style={{ background: "rgba(239,68,68,0.1)", border: "1px solid rgba(239,68,68,0.3)", color: "#f87171" }}>
                    <AlertCircle size={16} /> {error}
                </div>
            )}

            {/* Stats grid */}
            {loading && !stats ? (
                <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <div key={i} className="rounded-2xl h-24 border border-slate-800 animate-pulse"
                            style={{ background: "rgba(15,23,42,0.5)" }} />
                    ))}
                </div>
            ) : (
                <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
                    {cards.map(c => <StatCard key={c.label} {...c} />)}
                </div>
            )}

            {/* Recent activity */}
            <div className="rounded-2xl border border-slate-800 overflow-hidden"
                style={{ background: "rgba(15,23,42,0.7)" }}>
                <div className="px-5 py-4 border-b border-slate-800 flex items-center gap-2">
                    <Clock size={16} className="text-slate-400" />
                    <h2 className="text-sm font-semibold text-white">Recent Activity</h2>
                </div>
                <div className="divide-y divide-slate-800/60">
                    {loading && activity.length === 0 ? (
                        Array.from({ length: 5 }).map((_, i) => (
                            <div key={i} className="px-5 py-3 animate-pulse flex gap-3 items-center">
                                <div className="w-8 h-8 rounded-lg bg-slate-800" />
                                <div className="flex-1 space-y-1.5">
                                    <div className="h-3 bg-slate-800 rounded w-1/2" />
                                    <div className="h-2.5 bg-slate-800/60 rounded w-1/3" />
                                </div>
                            </div>
                        ))
                    ) : activity.length === 0 ? (
                        <div className="px-5 py-8 text-center text-slate-500 text-sm">No activity yet</div>
                    ) : (
                        activity.map(a => (
                            <div key={a.id} className="px-5 py-3 flex items-start gap-3 hover:bg-slate-800/30 transition-colors">
                                <span className="text-lg mt-0.5 shrink-0">{activityIcon(a.activityType)}</span>
                                <div className="flex-1 min-w-0">
                                    <p className="text-sm text-slate-200 truncate">{a.description}</p>
                                    <p className="text-xs text-slate-500 mt-0.5">
                                        {new Date(a.timestamp).toLocaleString()}
                                        {a.targetType && <span className="ml-2 text-slate-600">· {a.targetType}</span>}
                                    </p>
                                </div>
                            </div>
                        ))
                    )}
                </div>
            </div>
        </div>
    );
}
