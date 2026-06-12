import { useEffect, useState, useCallback } from "react";
import { Trash2, AlertCircle, RefreshCw, Settings, Loader2, MessageSquare, User, CheckCircle, XCircle } from "lucide-react";
import {
    getReports, deleteReportedMessage, deleteReportedUser,
    markReportAsReviewed, ignoreReport,
    getModerationSettings, updateModerationSettings
} from "../../api/adminApi";
import type { ReportDto, ModerationSettings } from "../../api/adminApi";

const TABS = [
    { label: "Pending", status: 0 },
    { label: "Reviewed", status: 1 },
    { label: "Ignored", status: 2 },
    { label: "Auto Removed", status: undefined, autoRemoved: true },
];


export default function AdminModerationPage() {
    const [tab, setTab] = useState(0);
    const [reports, setReports] = useState<ReportDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [actionId, setActionId] = useState<string | null>(null);
    const [toast, setToast] = useState<string | null>(null);
    const [showSettings, setShowSettings] = useState(false);
    const [settings, setSettings] = useState<ModerationSettings>({ reportThreshold: 5, autoDeleteEnabled: true });
    const [savingSettings, setSavingSettings] = useState(false);

    const showToast = (msg: string) => {
        setToast(msg);
        setTimeout(() => setToast(null), 3000);
    };

    const currentTab = TABS[tab];

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            // For Auto Removed tab, filter by isAutoRemoved=true and any status
            // For other tabs, filter by status and ensure isAutoRemoved=false
            const isAutoRemoved = "autoRemoved" in currentTab ? true : undefined;
            const status = "autoRemoved" in currentTab ? undefined : currentTab.status;
            const data = await getReports(status, isAutoRemoved);
            setReports(data);
        } catch {
            setError("Failed to load reports.");
        } finally {
            setLoading(false);
        }
    }, [tab]); // eslint-disable-line react-hooks/exhaustive-deps

    useEffect(() => { load(); }, [load]);

    useEffect(() => {
        getModerationSettings().then(setSettings).catch(() => { });
    }, []);

    const handleMarkAsReviewed = async (reportId: string) => {
        setActionId(reportId);
        try {
            await markReportAsReviewed(reportId);
            showToast("Report marked as reviewed.");
            load();
        } catch { showToast("Action failed."); }
        finally { setActionId(null); }
    };

    const handleIgnoreReport = async (reportId: string) => {
        setActionId(reportId);
        try {
            await ignoreReport(reportId);
            showToast("Report ignored.");
            load();
        } catch { showToast("Action failed."); }
        finally { setActionId(null); }
    };

    const handleDeleteMessage = async (reportId: string, messageId: string) => {
        setActionId(reportId);
        try {
            await deleteReportedMessage(messageId);
            showToast("Message deleted permanently.");
            load();
        } catch { showToast("Action failed."); }
        finally { setActionId(null); }
    };

    const handleDeleteUser = async (reportId: string, userId: string) => {
        setActionId(reportId);
        try {
            await deleteReportedUser(userId);
            showToast("User deleted.");
            load();
        } catch { showToast("Action failed."); }
        finally { setActionId(null); }
    };

    const handleSaveSettings = async () => {
        setSavingSettings(true);
        try {
            const updated = await updateModerationSettings(settings);
            setSettings(updated);
            showToast("Settings saved.");
            setShowSettings(false);
        } catch { showToast("Failed to save settings."); }
        finally { setSavingSettings(false); }
    };

    return (
        <div className="p-6 space-y-5">
            {/* Toast */}
            {toast && (
                <div className="fixed top-4 right-4 z-50 px-4 py-3 rounded-xl text-sm font-medium text-white shadow-2xl"
                    style={{ background: "rgba(30,41,59,0.97)", border: "1px solid rgba(255,255,255,0.1)" }}>
                    {toast}
                </div>
            )}

            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white">Reports</h1>
                    <p className="text-sm text-slate-400 mt-0.5">Review and act on reported messages</p>
                </div>
                <div className="flex items-center gap-2">
                    <button onClick={() => setShowSettings(s => !s)}
                        className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all">
                        <Settings size={14} /> Settings
                    </button>
                    <button onClick={load} disabled={loading}
                        className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50">
                        <RefreshCw size={14} className={loading ? "animate-spin" : ""} /> Refresh
                    </button>
                </div>
            </div>

            {/* Settings panel */}
            {showSettings && (
                <div className="rounded-2xl border border-purple-500/20 p-5 space-y-4"
                    style={{ background: "rgba(124,58,237,0.06)" }}>
                    <h3 className="text-sm font-semibold text-white">Moderation Settings</h3>
                    <div className="flex items-center gap-6">
                        <div className="space-y-1">
                            <label className="text-xs text-slate-400 uppercase tracking-wider">Report Threshold</label>
                            <input type="number" min={1} max={100}
                                value={settings.reportThreshold}
                                onChange={e => setSettings(s => ({ ...s, reportThreshold: Number(e.target.value) }))}
                                className="w-24 rounded-xl px-4 py-2 text-sm outline-none"
                                style={{ background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)", color: "#f1f5f9" }} />
                        </div>
                        <div className="flex items-center gap-3">
                            <label className="text-xs text-slate-400 uppercase tracking-wider">Auto-Delete</label>
                            <button
                                onClick={() => setSettings(s => ({ ...s, autoDeleteEnabled: !s.autoDeleteEnabled }))}
                                className="w-11 h-6 rounded-full relative transition-colors"
                                style={{ background: settings.autoDeleteEnabled ? "#7c3aed" : "rgba(255,255,255,0.1)" }}>
                                <span className="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-all duration-200"
                                    style={{ left: settings.autoDeleteEnabled ? "calc(100% - 22px)" : "2px" }} />
                            </button>
                        </div>
                    </div>
                    <button onClick={handleSaveSettings} disabled={savingSettings}
                        className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white transition-all disabled:opacity-50"
                        style={{ background: "linear-gradient(135deg,#7c3aed,#6d28d9)" }}>
                        {savingSettings ? <Loader2 size={14} className="animate-spin" /> : null}
                        Save Settings
                    </button>
                </div>
            )}

            {/* Tabs */}
            <div className="flex gap-1 p-1 rounded-xl w-fit"
                style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.06)" }}>
                {TABS.map((t, i) => (
                    <button key={i} onClick={() => setTab(i)}
                        className="px-4 py-1.5 rounded-lg text-sm font-medium transition-all"
                        style={tab === i
                            ? { background: "linear-gradient(135deg,#7c3aed,#6d28d9)", color: "#fff" }
                            : { color: "#64748b" }}>
                        {t.label}
                    </button>
                ))}
            </div>

            {/* Error */}
            {error && (
                <div className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm"
                    style={{ background: "rgba(239,68,68,0.1)", border: "1px solid rgba(239,68,68,0.3)", color: "#f87171" }}>
                    <AlertCircle size={16} /> {error}
                </div>
            )}

            {/* Reports table */}
            <div className="rounded-2xl border border-slate-800 overflow-hidden"
                style={{ background: "rgba(15,23,42,0.7)" }}>
                <table className="w-full text-sm">
                    <thead>
                        <tr className="border-b border-slate-800">
                            {["Message", "Sender", "Reporter", "Type", "Reason", "Date", "Status", "Actions"].map(h => (
                                <th key={h} className="px-5 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">{h}</th>
                            ))}
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-800/60">
                        {loading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <tr key={i}>
                                    {Array.from({ length: 8 }).map((_, j) => (
                                        <td key={j} className="px-5 py-4"><div className="h-3.5 bg-slate-800 rounded animate-pulse w-16" /></td>
                                    ))}
                                </tr>
                            ))
                        ) : reports.length === 0 ? (
                            <tr><td colSpan={8} className="px-5 py-10 text-center text-slate-500">No reports in this category</td></tr>
                        ) : (
                            reports.map(r => (
                                <tr key={r.id} className="hover:bg-slate-800/30 transition-colors">
                                    <td className="px-5 py-3.5">
                                        <div className="flex items-center gap-2 max-w-md">
                                            <MessageSquare size={14} className="text-slate-500 shrink-0" />
                                            <span className="text-slate-300 truncate">{r.messageContent || "Message content unavailable"}</span>
                                        </div>
                                    </td>
                                    <td className="px-5 py-3.5">
                                        <div className="flex items-center gap-2">
                                            <User size={14} className="text-slate-500 shrink-0" />
                                            <span className="text-slate-300">{r.messageAuthorName || "Unknown"}</span>
                                        </div>
                                    </td>
                                    <td className="px-5 py-3.5">
                                        <div className="flex items-center gap-2">
                                            <User size={14} className="text-slate-500 shrink-0" />
                                            <span className="text-slate-300">{r.reportedByUserName || "Unknown"}</span>
                                        </div>
                                    </td>
                                    <td className="px-5 py-3.5">
                                        <span className="px-2 py-1 rounded-md text-xs font-medium"
                                            style={{
                                                background: r.messageType === 0 ? "rgba(59,130,246,0.15)" : "rgba(168,85,247,0.15)",
                                                color: r.messageType === 0 ? "#60a5fa" : "#c084fc"
                                            }}>
                                            {r.messageTypeName}
                                        </span>
                                    </td>
                                    <td className="px-5 py-3.5 text-slate-300 max-w-xs truncate">{r.reason}</td>
                                    <td className="px-5 py-3.5 text-slate-500 text-xs">
                                        {new Date(r.reportedAt).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}
                                        <br />
                                        {new Date(r.reportedAt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })}
                                    </td>
                                    <td className="px-5 py-3.5">
                                        <span className={`px-2 py-1 rounded-md text-xs font-medium ${
                                            r.status === 0 ? 'bg-yellow-500/15 text-yellow-400' :
                                            r.status === 1 ? 'bg-green-500/15 text-green-400' :
                                            r.status === 2 ? 'bg-slate-500/15 text-slate-400' :
                                            'bg-red-500/15 text-red-400'
                                        }`}>
                                            {r.statusName}
                                            {r.isAutoRemoved && <span className="ml-1 text-xs">(Auto)</span>}
                                        </span>
                                    </td>
                                    <td className="px-5 py-3.5">
                                        <div className="flex items-center gap-2">
                                            {/* Show Mark as Reviewed and Ignore for pending reports */}
                                            {r.status === 0 && !r.isAutoRemoved && (
                                                <>
                                                    <button
                                                        onClick={() => handleMarkAsReviewed(r.id)}
                                                        disabled={actionId === r.id}
                                                        title="Mark as Reviewed"
                                                        className="p-1.5 rounded-lg text-slate-500 hover:text-green-400 hover:bg-green-500/10 transition-colors disabled:opacity-40">
                                                        {actionId === r.id ? <Loader2 size={15} className="animate-spin" /> : <CheckCircle size={15} />}
                                                    </button>
                                                    <button
                                                        onClick={() => handleIgnoreReport(r.id)}
                                                        disabled={actionId === r.id}
                                                        title="Ignore Report"
                                                        className="p-1.5 rounded-lg text-slate-500 hover:text-yellow-400 hover:bg-yellow-500/10 transition-colors disabled:opacity-40">
                                                        {actionId === r.id ? <Loader2 size={15} className="animate-spin" /> : <XCircle size={15} />}
                                                    </button>
                                                </>
                                            )}
                                            {/* Delete Message button - always available */}
                                            <button
                                                onClick={() => handleDeleteMessage(r.id, r.messageId)}
                                                disabled={actionId === r.id}
                                                title="Delete Message"
                                                className="p-1.5 rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-500/10 transition-colors disabled:opacity-40">
                                                {actionId === r.id ? <Loader2 size={15} className="animate-spin" /> : <Trash2 size={15} />}
                                            </button>
                                            {/* Delete User button - always available */}
                                            <button
                                                onClick={() => handleDeleteUser(r.id, r.messageAuthorId)}
                                                disabled={actionId === r.id}
                                                title="Delete User"
                                                className="p-1.5 rounded-lg text-slate-500 hover:text-red-500 hover:bg-red-500/10 transition-colors disabled:opacity-40">
                                                {actionId === r.id ? <Loader2 size={15} className="animate-spin" /> : <User size={15} />}
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
