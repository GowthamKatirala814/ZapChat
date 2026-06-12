import { useEffect, useState } from "react";
import { AlertCircle, RefreshCw, ChevronLeft, ChevronRight, ScrollText } from "lucide-react";
import { getAuditLogs } from "../../api/adminApi";
import type { AuditLog } from "../../api/adminApi";

type DateFilter = "today" | "7d" | "30d" | "all";

function filterByDate(logs: AuditLog[], filter: DateFilter): AuditLog[] {
    if (filter === "all") return logs;
    const now = new Date();
    const cutoff = new Date(now);
    if (filter === "today") {
        cutoff.setHours(0, 0, 0, 0);
    } else if (filter === "7d") {
        cutoff.setDate(now.getDate() - 7);
    } else {
        cutoff.setDate(now.getDate() - 30);
    }
    return logs.filter(l => new Date(l.timestamp) >= cutoff);
}

function actionColor(action: string): string {
    if (action.toLowerCase().includes("block")) return "#f87171";
    if (action.toLowerCase().includes("delete")) return "#fb923c";
    if (action.toLowerCase().includes("create")) return "#34d399";
    if (action.toLowerCase().includes("approve")) return "#60a5fa";
    if (action.toLowerCase().includes("ignore")) return "#94a3b8";
    if (action.toLowerCase().includes("threshold")) return "#a78bfa";
    if (action.toLowerCase().includes("auto")) return "#facc15";
    return "#64748b";
}

export default function AdminAuditLogsPage() {
    const [logs, setLogs] = useState<AuditLog[]>([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [dateFilter, setDateFilter] = useState<DateFilter>("all");

    const PAGE_SIZE = 50;

    const load = async (p: number) => {
        setLoading(true);
        setError(null);
        try {
            const result = await getAuditLogs(p, PAGE_SIZE);
            setLogs(result.data);
            setTotal(result.totalCount);
        } catch {
            setError("Failed to load audit logs.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(page); }, [page]);

    const displayed = filterByDate(logs, dateFilter);
    const totalPages = Math.ceil(total / PAGE_SIZE);

    const DATE_FILTERS: { key: DateFilter; label: string }[] = [
        { key: "today", label: "Today" },
        { key: "7d", label: "7 Days" },
        { key: "30d", label: "30 Days" },
        { key: "all", label: "All" },
    ];

    return (
        <div className="p-6 space-y-5">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white">Audit Logs</h1>
                    <p className="text-sm text-slate-400 mt-0.5">{total.toLocaleString()} total entries</p>
                </div>
                <button onClick={() => load(page)} disabled={loading}
                    className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50">
                    <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
                    Refresh
                </button>
            </div>

            {/* Date filter */}
            <div className="flex items-center gap-2">
                <span className="text-xs text-slate-500 uppercase tracking-wider">Filter:</span>
                <div className="flex gap-1 p-1 rounded-xl"
                    style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.06)" }}>
                    {DATE_FILTERS.map(f => (
                        <button key={f.key} onClick={() => setDateFilter(f.key)}
                            className="px-4 py-1.5 rounded-lg text-sm font-medium transition-all"
                            style={dateFilter === f.key
                                ? { background: "linear-gradient(135deg,#7c3aed,#6d28d9)", color: "#fff" }
                                : { color: "#64748b" }}>
                            {f.label}
                        </button>
                    ))}
                </div>
                <span className="text-xs text-slate-500 ml-2">{displayed.length} shown</span>
            </div>

            {/* Error */}
            {error && (
                <div className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm"
                    style={{ background: "rgba(239,68,68,0.1)", border: "1px solid rgba(239,68,68,0.3)", color: "#f87171" }}>
                    <AlertCircle size={16} /> {error}
                </div>
            )}

            {/* Table */}
            <div className="rounded-2xl border border-slate-800 overflow-hidden"
                style={{ background: "rgba(15,23,42,0.7)" }}>
                <table className="w-full text-sm">
                    <thead>
                        <tr className="border-b border-slate-800">
                            {["Action", "Entity Type", "Entity ID", "Performed By", "Timestamp"].map(h => (
                                <th key={h} className="px-5 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">{h}</th>
                            ))}
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-800/60">
                        {loading ? (
                            Array.from({ length: 8 }).map((_, i) => (
                                <tr key={i}>
                                    {Array.from({ length: 5 }).map((_, j) => (
                                        <td key={j} className="px-5 py-4">
                                            <div className="h-3.5 bg-slate-800 rounded animate-pulse w-20" />
                                        </td>
                                    ))}
                                </tr>
                            ))
                        ) : displayed.length === 0 ? (
                            <tr>
                                <td colSpan={5}>
                                    <div className="flex flex-col items-center py-16 gap-3">
                                        <ScrollText size={36} className="text-slate-700" />
                                        <p className="text-slate-500 text-sm">No logs for this filter</p>
                                    </div>
                                </td>
                            </tr>
                        ) : (
                            displayed.map(log => (
                                <tr key={log.id} className="hover:bg-slate-800/30 transition-colors">
                                    <td className="px-5 py-3.5">
                                        <span className="text-xs font-semibold px-2.5 py-1 rounded-full"
                                            style={{ background: `${actionColor(log.action)}18`, color: actionColor(log.action) }}>
                                            {log.action}
                                        </span>
                                    </td>
                                    <td className="px-5 py-3.5 text-slate-400">{log.targetType || "—"}</td>
                                    <td className="px-5 py-3.5">
                                        <code className="text-xs text-slate-500">{log.targetId ? log.targetId.slice(0, 12) + "…" : "—"}</code>
                                    </td>
                                    <td className="px-5 py-3.5">
                                        <code className="text-xs text-slate-500">
                                            {log.performedBy === "00000000-0000-0000-0000-000000000000"
                                                ? "System"
                                                : log.performedBy.slice(0, 8) + "…"}
                                        </code>
                                    </td>
                                    <td className="px-5 py-3.5 text-slate-500 text-xs">
                                        {new Date(log.timestamp).toLocaleString()}
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex items-center justify-between">
                    <p className="text-xs text-slate-500">
                        Page {page} of {totalPages} · {total.toLocaleString()} total
                    </p>
                    <div className="flex items-center gap-2">
                        <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1 || loading}
                            className="p-2 rounded-xl border border-slate-700 text-slate-400 hover:text-white hover:border-slate-500 transition-all disabled:opacity-40">
                            <ChevronLeft size={15} />
                        </button>
                        <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages || loading}
                            className="p-2 rounded-xl border border-slate-700 text-slate-400 hover:text-white hover:border-slate-500 transition-all disabled:opacity-40">
                            <ChevronRight size={15} />
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
