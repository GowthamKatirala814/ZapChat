import { useEffect, useState, useCallback, useRef } from "react";
import {
    Trash2, AlertCircle, RefreshCw, Settings, Loader2, MessageSquare,
    User, CheckCircle, XCircle, Shield, ChevronRight, X, Ban
} from "lucide-react";
import {
    getReports, deleteReportedMessage, deleteReportedUser,
    markReportAsReviewed, ignoreReport,
    getModerationSettings, updateModerationSettings
} from "../../api/adminApi";
import type { ReportDto, ModerationSettings } from "../../api/adminApi";

// ── Tab definitions ───────────────────────────────────────────────────────────
const TABS = [
    { label: "Pending",      status: 0,         isAutoRemovedFilter: false  },
    { label: "Reviewed",     status: 1,         isAutoRemovedFilter: undefined },
    { label: "Ignored",      status: 2,         isAutoRemovedFilter: undefined },
    { label: "Auto Removed", status: undefined,  isAutoRemovedFilter: true  },
] as const;

// ── Status badge styles ───────────────────────────────────────────────────────
const STATUS_STYLE: Record<number, { bg: string; text: string; label: string }> = {
    0: { bg: "rgba(234,179,8,0.15)",   text: "#facc15", label: "Pending"      },
    1: { bg: "rgba(34,197,94,0.15)",   text: "#4ade80", label: "Reviewed"     },
    2: { bg: "rgba(148,163,184,0.15)", text: "#94a3b8", label: "Ignored"      },
    3: { bg: "rgba(239,68,68,0.15)",   text: "#f87171", label: "Auto Removed" },
};

// ── Type badge styles ─────────────────────────────────────────────────────────
const TYPE_STYLE: Record<number, { bg: string; text: string }> = {
    0: { bg: "rgba(59,130,246,0.15)",  text: "#60a5fa" }, // Room
    1: { bg: "rgba(168,85,247,0.15)",  text: "#c084fc" }, // Private
};

export default function AdminModerationPage() {
    const [tab, setTab] = useState(0);
    const [reports, setReports] = useState<ReportDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Drawer state
    const [selectedReport, setSelectedReport] = useState<ReportDto | null>(null);

    // Per-action loading state (keyed by reportId + action)
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    // Toast
    const [toast, setToast] = useState<{ msg: string; ok: boolean } | null>(null);
    const toastTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

    // Settings panel
    const [showSettings, setShowSettings] = useState(false);
    const [settings, setSettings] = useState<ModerationSettings>({ reportThreshold: 5, autoDeleteEnabled: true });
    const [savingSettings, setSavingSettings] = useState(false);

    // Completed-action tracking (issue #4) — persists across reloads for this session
    // Key: messageId, value: true when message was deleted by admin
    const [deletedMessageIds, setDeletedMessageIds] = useState<Set<string>>(new Set());
    // Key: userId, value: true when user was deleted/blocked by admin
    const [deletedUserIds, setDeletedUserIds] = useState<Set<string>>(new Set());

    // ── Toast helper ─────────────────────────────────────────────────────────
    const showToast = (msg: string, ok = true) => {
        if (toastTimer.current) clearTimeout(toastTimer.current);
        setToast({ msg, ok });
        toastTimer.current = setTimeout(() => setToast(null), 3500);
    };

    // ── Data loader ───────────────────────────────────────────────────────────
    // Fix for issue #2: compute all filter values from `tab` (the index) directly
    // inside the callback so there is no stale closure over `currentTab`.
    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const tabDef = TABS[tab];
            const status       = tabDef.status as number | undefined;
            const isAutoRemoved = tabDef.isAutoRemovedFilter as boolean | undefined;
            const data = await getReports(status, isAutoRemoved);
            setReports(data);
        } catch {
            setError("Failed to load reports. Please refresh.");
        } finally {
            setLoading(false);
        }
    }, [tab]);

    useEffect(() => { load(); }, [load]);
    useEffect(() => { getModerationSettings().then(setSettings).catch(() => {}); }, []);

    // Keep drawer in sync when the report list refreshes
    useEffect(() => {
        if (selectedReport) {
            const refreshed = reports.find(r => r.id === selectedReport.id);
            if (refreshed) setSelectedReport(refreshed);
        }
    }, [reports]); // eslint-disable-line react-hooks/exhaustive-deps

    // ── Action helpers ────────────────────────────────────────────────────────
    const withAction = async (key: string, fn: () => Promise<void>) => {
        setActionLoading(key);
        try {
            await fn();
        } catch {
            showToast("Action failed — please try again.", false);
        } finally {
            setActionLoading(null);
        }
    };

    const handleMarkAsReviewed = (r: ReportDto) =>
        withAction(`reviewed-${r.id}`, async () => {
            await markReportAsReviewed(r.id);
            showToast("Report marked as reviewed.");
            setSelectedReport(null);
            await load();
        });

    const handleIgnore = (r: ReportDto) =>
        withAction(`ignore-${r.id}`, async () => {
            await ignoreReport(r.id);
            showToast("Report ignored.");
            setSelectedReport(null);
            await load();
        });

    const handleDeleteMessage = (r: ReportDto) =>
        withAction(`msg-${r.id}`, async () => {
            await deleteReportedMessage(r.messageId);
            setDeletedMessageIds(prev => new Set([...prev, r.messageId]));
            showToast("Message removed successfully.");
            // Close drawer and reload (report leaves Pending automatically)
            setSelectedReport(null);
            await load();
        });

    const handleDeleteUser = (r: ReportDto) =>
        withAction(`user-${r.id}`, async () => {
            await deleteReportedUser(r.messageAuthorId);
            setDeletedUserIds(prev => new Set([...prev, r.messageAuthorId]));
            showToast("User deleted and reports cleared.");
            setSelectedReport(null);
            await load();
        });

    const handleSaveSettings = async () => {
        setSavingSettings(true);
        try {
            const updated = await updateModerationSettings(settings);
            setSettings(updated);
            showToast("Settings saved.");
            setShowSettings(false);
        } catch {
            showToast("Failed to save settings.", false);
        } finally {
            setSavingSettings(false);
        }
    };

    // ── Action button visibility logic (issue #4) ─────────────────────────────
    // A report is "resolved" if it is no longer Pending or is auto-removed.
    const isResolved = (r: ReportDto) => r.status !== 0 || r.isAutoRemoved;
    const isMessageAlreadyDeleted = (r: ReportDto) => deletedMessageIds.has(r.messageId);
    const isUserAlreadyDeleted    = (r: ReportDto) => deletedUserIds.has(r.messageAuthorId);

    const canRemoveMessage = (r: ReportDto) => !isResolved(r) && !isMessageAlreadyDeleted(r);
    const canDeleteUser    = (r: ReportDto) => !isResolved(r) && !isUserAlreadyDeleted(r);
    const canMarkReviewed  = (r: ReportDto) => r.status === 0 && !r.isAutoRemoved;
    const canIgnore        = (r: ReportDto) => r.status === 0 && !r.isAutoRemoved;

    const hasAnyAction = (r: ReportDto) =>
        canRemoveMessage(r) || canDeleteUser(r) || canMarkReviewed(r) || canIgnore(r);

    // ── Spinner helper ────────────────────────────────────────────────────────
    const isActing = (key: string) => actionLoading === key;

    // ── Render ────────────────────────────────────────────────────────────────
    return (
        <div className="p-3 sm:p-6 space-y-4 sm:space-y-5 relative">

            {/* ── Toast ──────────────────────────────────────────────────────── */}
            {toast && (
                <div
                    className="fixed top-5 right-5 z-[100] flex items-center gap-3 px-5 py-3.5 rounded-2xl text-sm font-medium text-white shadow-2xl transition-all"
                    style={{
                        background: toast.ok ? "rgba(16,185,129,0.95)" : "rgba(239,68,68,0.95)",
                        border: "1px solid rgba(255,255,255,0.15)",
                        backdropFilter: "blur(12px)",
                    }}
                >
                    {toast.ok ? <CheckCircle size={16} /> : <AlertCircle size={16} />}
                    {toast.msg}
                </div>
            )}

            {/* ── Page Header ─────────────────────────────────────────────────── */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white">Reports</h1>
                    <p className="text-sm text-slate-400 mt-0.5">Review and act on reported messages</p>
                </div>
                <div className="flex items-center gap-2">
                    <button
                        onClick={() => setShowSettings(s => !s)}
                        className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all"
                    >
                        <Settings size={14} /> Settings
                    </button>
                    <button
                        onClick={load}
                        disabled={loading}
                        className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50"
                    >
                        <RefreshCw size={14} className={loading ? "animate-spin" : ""} /> Refresh
                    </button>
                </div>
            </div>

            {/* ── Settings Panel ───────────────────────────────────────────────── */}
            {showSettings && (
                <div
                    className="rounded-2xl border border-purple-500/20 p-5 space-y-4"
                    style={{ background: "rgba(124,58,237,0.06)" }}
                >
                    <h3 className="text-sm font-semibold text-white">Moderation Settings</h3>
                    <div className="flex items-center gap-6">
                        <div className="space-y-1">
                            <label className="text-xs text-slate-400 uppercase tracking-wider">Report Threshold</label>
                            <input
                                type="number" min={1} max={100}
                                value={settings.reportThreshold}
                                onChange={e => setSettings(s => ({ ...s, reportThreshold: Number(e.target.value) }))}
                                className="w-24 rounded-xl px-4 py-2 text-sm outline-none"
                                style={{ background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)", color: "#f1f5f9" }}
                            />
                        </div>
                        <div className="flex items-center gap-3">
                            <label className="text-xs text-slate-400 uppercase tracking-wider">Auto-Delete</label>
                            <button
                                onClick={() => setSettings(s => ({ ...s, autoDeleteEnabled: !s.autoDeleteEnabled }))}
                                className="w-11 h-6 rounded-full relative transition-colors"
                                style={{ background: settings.autoDeleteEnabled ? "#7c3aed" : "rgba(255,255,255,0.1)" }}
                            >
                                <span
                                    className="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-all duration-200"
                                    style={{ left: settings.autoDeleteEnabled ? "calc(100% - 22px)" : "2px" }}
                                />
                            </button>
                        </div>
                    </div>
                    <button
                        onClick={handleSaveSettings}
                        disabled={savingSettings}
                        className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white transition-all disabled:opacity-50"
                        style={{ background: "linear-gradient(135deg,#7c3aed,#6d28d9)" }}
                    >
                        {savingSettings ? <Loader2 size={14} className="animate-spin" /> : null}
                        Save Settings
                    </button>
                </div>
            )}

            {/* ── Tabs ─────────────────────────────────────────────────────────── */}
            <div
                className="flex gap-1 p-1 rounded-xl w-fit"
                style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.06)" }}
            >
                {TABS.map((t, i) => (
                    <button
                        key={i}
                        onClick={() => { setTab(i); setSelectedReport(null); }}
                        className="px-4 py-1.5 rounded-lg text-sm font-medium transition-all"
                        style={tab === i
                            ? { background: "linear-gradient(135deg,#7c3aed,#6d28d9)", color: "#fff" }
                            : { color: "#64748b" }}
                    >
                        {t.label}
                    </button>
                ))}
            </div>

            {/* ── Error Banner ─────────────────────────────────────────────────── */}
            {error && (
                <div
                    className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm"
                    style={{ background: "rgba(239,68,68,0.1)", border: "1px solid rgba(239,68,68,0.3)", color: "#f87171" }}
                >
                    <AlertCircle size={16} /> {error}
                </div>
            )}

            {/* ── Main Content: Table + optional Drawer side-by-side ─────────────── */}
            <div className="flex gap-4 items-start">

                {/* ── Reports Table ──────────────────────────────────────────────── */}
                <div
                    className="flex-1 rounded-2xl border border-slate-800 overflow-hidden min-w-0"
                    style={{ background: "rgba(15,23,42,0.7)" }}
                >
                    <div className="overflow-x-auto">
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-slate-800">
                                    {["Message", "Sender", "Reporter", "Type", "Reason", "Date", "Status", ""].map(h => (
                                        <th key={h} className="px-5 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider whitespace-nowrap">
                                            {h}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-800/60">
                                {loading ? (
                                    Array.from({ length: 5 }).map((_, i) => (
                                        <tr key={i}>
                                            {Array.from({ length: 8 }).map((_, j) => (
                                                <td key={j} className="px-5 py-4">
                                                    <div className="h-3.5 bg-slate-800 rounded animate-pulse w-16" />
                                                </td>
                                            ))}
                                        </tr>
                                    ))
                                ) : reports.length === 0 ? (
                                    <tr>
                                        <td colSpan={8} className="px-5 py-12 text-center text-slate-500">
                                            <div className="flex flex-col items-center gap-2">
                                                <Shield size={28} className="text-slate-700" />
                                                <span>No reports in this category</span>
                                            </div>
                                        </td>
                                    </tr>
                                ) : (
                                    reports.map(r => {
                                        const isSelected = selectedReport?.id === r.id;
                                        const statusStyle = STATUS_STYLE[r.status] ?? STATUS_STYLE[0];
                                        const typeStyle   = TYPE_STYLE[r.messageType] ?? TYPE_STYLE[0];
                                        return (
                                            <tr
                                                key={r.id}
                                                onClick={() => setSelectedReport(isSelected ? null : r)}
                                                className="transition-colors cursor-pointer"
                                                style={{
                                                    background: isSelected
                                                        ? "rgba(124,58,237,0.12)"
                                                        : undefined,
                                                }}
                                                onMouseEnter={e => { if (!isSelected) (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,0.03)"; }}
                                                onMouseLeave={e => { if (!isSelected) (e.currentTarget as HTMLElement).style.background = ""; }}
                                            >
                                                {/* Message preview */}
                                                <td className="px-5 py-3.5">
                                                    <div className="flex items-center gap-2 max-w-[200px]">
                                                        <MessageSquare size={13} className="text-slate-500 shrink-0" />
                                                        <span className="text-slate-300 truncate text-xs">
                                                            {r.messageContent || "Content unavailable"}
                                                        </span>
                                                    </div>
                                                </td>
                                                {/* Sender */}
                                                <td className="px-5 py-3.5">
                                                    <div className="flex items-center gap-1.5">
                                                        <User size={13} className="text-slate-500 shrink-0" />
                                                        <span className="text-slate-300 text-xs truncate max-w-[80px]">
                                                            {r.messageAuthorName || "Unknown"}
                                                        </span>
                                                    </div>
                                                </td>
                                                {/* Reporter */}
                                                <td className="px-5 py-3.5">
                                                    <span className="text-slate-400 text-xs truncate max-w-[80px] block">
                                                        {r.reportedByUserName || "Unknown"}
                                                    </span>
                                                </td>
                                                {/* Type */}
                                                <td className="px-5 py-3.5">
                                                    <span
                                                        className="px-2 py-0.5 rounded-md text-xs font-medium whitespace-nowrap"
                                                        style={{ background: typeStyle.bg, color: typeStyle.text }}
                                                    >
                                                        {r.messageTypeName || (r.messageType === 0 ? "Room" : "Private")}
                                                    </span>
                                                </td>
                                                {/* Reason */}
                                                <td className="px-5 py-3.5 max-w-[120px]">
                                                    <span className="text-slate-400 text-xs truncate block">{r.reason}</span>
                                                </td>
                                                {/* Date */}
                                                <td className="px-5 py-3.5 text-slate-500 text-xs whitespace-nowrap">
                                                    {new Date(r.reportedAt).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" })}
                                                    <br />
                                                    {new Date(r.reportedAt).toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit", hour12: true })}
                                                </td>
                                                {/* Status */}
                                                <td className="px-5 py-3.5">
                                                    <span
                                                        className="px-2 py-0.5 rounded-md text-xs font-medium whitespace-nowrap"
                                                        style={{ background: statusStyle.bg, color: statusStyle.text }}
                                                    >
                                                        {r.isAutoRemoved ? "Auto Removed" : statusStyle.label}
                                                    </span>
                                                </td>
                                                {/* Open drawer chevron */}
                                                <td className="px-3 py-3.5">
                                                    <ChevronRight
                                                        size={16}
                                                        className="text-slate-600 transition-transform"
                                                        style={{ transform: isSelected ? "rotate(90deg)" : "none" }}
                                                    />
                                                </td>
                                            </tr>
                                        );
                                    })
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>

                {/* ── Report Detail Drawer ────────────────────────────────────────── */}
                {selectedReport && (
                    <aside
                        className="w-80 shrink-0 rounded-2xl border border-slate-700 flex flex-col"
                        style={{
                            background: "rgba(15,23,42,0.95)",
                            maxHeight: "calc(100vh - 180px)",
                            position: "sticky",
                            top: "1rem",
                        }}
                    >
                        {/* Drawer header */}
                        <div
                            className="flex items-center justify-between px-5 py-4 border-b border-slate-800 shrink-0"
                            style={{ background: "rgba(124,58,237,0.08)" }}
                        >
                            <span className="text-sm font-semibold text-white">Report Details</span>
                            <button
                                onClick={() => setSelectedReport(null)}
                                className="p-1 rounded-lg text-slate-500 hover:text-white hover:bg-slate-700 transition-colors"
                            >
                                <X size={16} />
                            </button>
                        </div>

                        {/* Drawer body — scrollable */}
                        <div className="flex-1 overflow-y-auto px-5 py-4 space-y-5 min-h-0">
                            {/* Message content — scrollable block */}
                            <div>
                                <label className="text-xs text-slate-500 uppercase tracking-wider mb-2 block">
                                    Message Content
                                </label>
                                <div
                                    className="rounded-xl px-4 py-3 text-sm text-slate-200 leading-relaxed overflow-y-auto"
                                    style={{
                                        background: "rgba(255,255,255,0.04)",
                                        border: "1px solid rgba(255,255,255,0.08)",
                                        maxHeight: "160px",
                                        wordBreak: "break-word",
                                        whiteSpace: "pre-wrap",
                                    }}
                                >
                                    {selectedReport.messageContent || "Message content unavailable"}
                                </div>
                            </div>

                            {/* Metadata grid */}
                            <div className="grid grid-cols-2 gap-3">
                                <InfoCard label="Sender" value={selectedReport.messageAuthorName || "Unknown"} />
                                <InfoCard label="Reporter" value={selectedReport.reportedByUserName || "Unknown"} />
                                <InfoCard
                                    label="Type"
                                    value={selectedReport.messageTypeName || (selectedReport.messageType === 0 ? "Room" : "Private")}
                                />
                                <InfoCard
                                    label="Status"
                                    value={selectedReport.isAutoRemoved ? "Auto Removed" : STATUS_STYLE[selectedReport.status]?.label ?? "Unknown"}
                                />
                                <InfoCard
                                    label="Reported"
                                    value={new Date(selectedReport.reportedAt).toLocaleDateString("en-GB", {
                                        day: "2-digit", month: "short", year: "numeric"
                                    })}
                                    wide
                                />
                            </div>

                            {/* Reason */}
                            <div>
                                <label className="text-xs text-slate-500 uppercase tracking-wider mb-2 block">Reason</label>
                                <p className="text-sm text-slate-300 leading-relaxed break-words">
                                    {selectedReport.reason}
                                </p>
                            </div>

                            {/* Completed-action notices */}
                            {isMessageAlreadyDeleted(selectedReport) && (
                                <CompletedBadge label="Message already removed" />
                            )}
                            {isUserAlreadyDeleted(selectedReport) && (
                                <CompletedBadge label="User already deleted" />
                            )}
                            {isResolved(selectedReport) && !hasAnyAction(selectedReport) && (
                                <CompletedBadge label="All actions completed — no further action required" />
                            )}
                        </div>

                        {/* Drawer footer — sticky action buttons */}
                        <div
                            className="px-5 py-4 border-t border-slate-800 space-y-2 shrink-0"
                            style={{ background: "rgba(15,23,42,0.98)" }}
                        >
                            {/* Row 1: Resolve/Ignore — only for Pending */}
                            {canMarkReviewed(selectedReport) && (
                                <button
                                    onClick={() => handleMarkAsReviewed(selectedReport)}
                                    disabled={!!actionLoading}
                                    className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium text-white transition-all disabled:opacity-50"
                                    style={{ background: "rgba(34,197,94,0.2)", border: "1px solid rgba(34,197,94,0.3)" }}
                                >
                                    {isActing(`reviewed-${selectedReport.id}`)
                                        ? <Loader2 size={14} className="animate-spin" />
                                        : <CheckCircle size={14} />}
                                    Mark as Reviewed
                                </button>
                            )}
                            {canIgnore(selectedReport) && (
                                <button
                                    onClick={() => handleIgnore(selectedReport)}
                                    disabled={!!actionLoading}
                                    className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-all disabled:opacity-50"
                                    style={{
                                        background: "rgba(148,163,184,0.1)",
                                        border: "1px solid rgba(148,163,184,0.2)",
                                        color: "#94a3b8",
                                    }}
                                >
                                    {isActing(`ignore-${selectedReport.id}`)
                                        ? <Loader2 size={14} className="animate-spin" />
                                        : <XCircle size={14} />}
                                    Ignore Report
                                </button>
                            )}

                            {/* Row 2: Remove Message */}
                            {canRemoveMessage(selectedReport) && (
                                <button
                                    onClick={() => handleDeleteMessage(selectedReport)}
                                    disabled={!!actionLoading}
                                    className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-all disabled:opacity-50"
                                    style={{
                                        background: "rgba(239,68,68,0.12)",
                                        border: "1px solid rgba(239,68,68,0.25)",
                                        color: "#f87171",
                                    }}
                                >
                                    {isActing(`msg-${selectedReport.id}`)
                                        ? <Loader2 size={14} className="animate-spin" />
                                        : <Trash2 size={14} />}
                                    Remove Message
                                </button>
                            )}

                            {/* Row 3: Delete User */}
                            {canDeleteUser(selectedReport) && (
                                <button
                                    onClick={() => handleDeleteUser(selectedReport)}
                                    disabled={!!actionLoading}
                                    className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-all disabled:opacity-50"
                                    style={{
                                        background: "rgba(239,68,68,0.18)",
                                        border: "1px solid rgba(239,68,68,0.35)",
                                        color: "#fb923c",
                                    }}
                                >
                                    {isActing(`user-${selectedReport.id}`)
                                        ? <Loader2 size={14} className="animate-spin" />
                                        : <Ban size={14} />}
                                    Delete User
                                </button>
                            )}

                            {/* Resolved state — no actions available */}
                            {!hasAnyAction(selectedReport) && (
                                <div
                                    className="text-center text-xs text-slate-500 py-2"
                                    style={{ borderTop: "1px solid rgba(255,255,255,0.04)" }}
                                >
                                    No further actions available
                                </div>
                            )}
                        </div>
                    </aside>
                )}
            </div>
        </div>
    );
}

// ── Small reusable sub-components ─────────────────────────────────────────────

function InfoCard({ label, value, wide }: { label: string; value: string; wide?: boolean }) {
    return (
        <div
            className={`rounded-xl px-3 py-2.5 ${wide ? "col-span-2" : ""}`}
            style={{ background: "rgba(255,255,255,0.03)", border: "1px solid rgba(255,255,255,0.06)" }}
        >
            <div className="text-xs text-slate-500 mb-0.5">{label}</div>
            <div className="text-sm text-slate-200 font-medium truncate">{value}</div>
        </div>
    );
}

function CompletedBadge({ label }: { label: string }) {
    return (
        <div
            className="flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-medium"
            style={{ background: "rgba(34,197,94,0.08)", color: "#4ade80", border: "1px solid rgba(34,197,94,0.2)" }}
        >
            <CheckCircle size={12} />
            {label}
        </div>
    );
}
