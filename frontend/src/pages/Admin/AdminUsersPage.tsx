import { useEffect, useState, useCallback } from "react";
import { Search, Trash2, AlertCircle, RefreshCw, X, Loader2 } from "lucide-react";
import { getAdminUsers, searchAdminUsers, deleteUser } from "../../api/adminApi";
import type { AdminUser } from "../../api/adminApi";

type ModalState =
    | { kind: "delete"; user: AdminUser }
    | null;

function StatusBadge({ isDeleted }: { isDeleted: boolean }) {
    const cfg = isDeleted
        ? { bg: "rgba(248,113,113,0.12)", color: "#f87171", label: "Deleted" }
        : { bg: "rgba(52,211,153,0.12)", color: "#34d399", label: "Active" };
    return (
        <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold"
            style={{ background: cfg.bg, color: cfg.color }}>{cfg.label}</span>
    );
}

function ConfirmModal({ modal, onConfirm, onClose }: {
    modal: ModalState;
    onConfirm: (reason: string) => void;
    onClose: () => void;
}) {
    const [reason, setReason] = useState("");
    const [busy, setBusy] = useState(false);

    if (!modal) return null;

    const handleConfirm = async () => {
        if (!reason.trim()) return;
        setBusy(true);
        await onConfirm(reason.trim());
        setBusy(false);
    };

    const title = "Delete User";
    const accent = "#ef4444";

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center px-4"
            style={{ background: "rgba(0,0,0,0.65)", backdropFilter: "blur(4px)" }}
            onClick={e => { if (e.target === e.currentTarget) onClose(); }}>
            <div className="w-full max-w-md rounded-2xl p-6 space-y-4"
                style={{ background: "#0f172a", border: "1px solid rgba(255,255,255,0.08)", boxShadow: "0 24px 60px rgba(0,0,0,0.6)" }}>
                <div className="flex items-center justify-between">
                    <h3 className="text-sm font-bold text-white">{title}</h3>
                    <button onClick={onClose} className="text-slate-500 hover:text-slate-300"><X size={16} /></button>
                </div>
                <p className="text-sm text-slate-400">
                    Soft-delete user <span className="font-semibold text-white">{modal.user.anonymousName}</span>.
                    User will not be able to log in.
                </p>
                <div>
                    <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1.5">
                        Reason <span style={{ color: accent }}>*</span>
                    </label>
                    <input
                        value={reason}
                        onChange={e => setReason(e.target.value)}
                        placeholder="Enter reason…"
                        className="w-full rounded-xl px-4 py-2.5 text-sm outline-none"
                        style={{ background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)", color: "#f1f5f9" }}
                        onFocus={e => (e.currentTarget.style.borderColor = accent)}
                        onBlur={e => (e.currentTarget.style.borderColor = "rgba(255,255,255,0.1)")}
                    />
                </div>
                <div className="flex gap-3 pt-1">
                    <button onClick={onClose} disabled={busy}
                        className="flex-1 py-2.5 rounded-xl text-sm border border-slate-700 text-slate-400 hover:text-white hover:border-slate-500 transition-all disabled:opacity-50">
                        Cancel
                    </button>
                    <button onClick={handleConfirm} disabled={busy || !reason.trim()}
                        className="flex-1 py-2.5 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-all disabled:opacity-40"
                        style={{ background: accent, color: "#fff", opacity: (busy || !reason.trim()) ? 0.4 : 1 }}>
                        {busy ? <Loader2 size={14} className="animate-spin" /> : title}
                    </button>
                </div>
            </div>
        </div>
    );
}

export default function AdminUsersPage() {
    const [users, setUsers] = useState<AdminUser[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [query, setQuery] = useState("");
    const [modal, setModal] = useState<ModalState>(null);
    const [toast, setToast] = useState<string | null>(null);

    const showToast = (msg: string) => {
        setToast(msg);
        setTimeout(() => setToast(null), 3000);
    };

    const load = useCallback(async (q = "") => {
        setLoading(true);
        setError(null);
        try {
            const data = q.trim()
                ? await searchAdminUsers(q.trim())
                : await getAdminUsers();
            setUsers(data);
        } catch {
            setError("Failed to load users.");
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    useEffect(() => {
        const t = setTimeout(() => load(query), 350);
        return () => clearTimeout(t);
    }, [query, load]);

    const handleConfirm = async (reason: string) => {
        if (!modal) return;
        try {
            await deleteUser(modal.user.id, reason);
            showToast("User deleted successfully.");
            setModal(null);
            load(query);
        } catch {
            showToast("Action failed. Please try again.");
            setModal(null);
        }
    };

    const sortedUsers = [...users].sort((a, b) => {
        if (a.isDeleted === b.isDeleted) {
            const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
            const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
            return dateB - dateA;
        }
        return a.isDeleted ? 1 : -1;
    });

    const activeUsers = sortedUsers.filter(u => !u.isDeleted);
    const deletedUsers = sortedUsers.filter(u => u.isDeleted);

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
                    <h1 className="text-2xl font-bold text-white">User Management</h1>
                    <p className="text-sm text-slate-400 mt-0.5">
                        {users.length} total · {activeUsers.length} active · {deletedUsers.length} deleted
                    </p>
                </div>
                <button onClick={() => load(query)} disabled={loading}
                    className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50">
                    <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
                    Refresh
                </button>
            </div>

            {/* Search */}
            <div className="relative">
                <Search size={15} className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500" />
                <input
                    value={query}
                    onChange={e => setQuery(e.target.value)}
                    placeholder="Search by anonymous name, department, or branch…"
                    className="w-full pl-10 pr-4 py-3 rounded-xl text-sm outline-none"
                    style={{ background: "rgba(15,23,42,0.8)", border: "1px solid rgba(255,255,255,0.08)", color: "#f1f5f9" }}
                />
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
                            {["Anonymous Name", "Department", "Branch", "Joined", "Status", "Actions"].map(h => (
                                <th key={h} className="px-5 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">{h}</th>
                            ))}
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-800/60">
                        {loading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <tr key={i}>
                                    {Array.from({ length: 6 }).map((_, j) => (
                                        <td key={j} className="px-5 py-4">
                                            <div className="h-3.5 bg-slate-800 rounded animate-pulse w-20" />
                                        </td>
                                    ))}
                                </tr>
                            ))
                        ) : sortedUsers.length === 0 ? (
                            <tr>
                                <td colSpan={6} className="px-5 py-10 text-center text-slate-500">No users found</td>
                            </tr>
                        ) : (
                            sortedUsers.map(u => (
                                <tr key={u.id} className="hover:bg-slate-800/30 transition-colors">
                                    <td className="px-5 py-3.5">
                                        <div className="flex items-center gap-3">
                                            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-purple-500 to-violet-600 flex items-center justify-center text-xs font-bold shrink-0">
                                                {u.anonymousName.charAt(0).toUpperCase()}
                                            </div>
                                            <span className="font-medium text-white">{u.anonymousName}</span>
                                        </div>
                                    </td>
                                    <td className="px-5 py-3.5 text-slate-400">{u.department || "—"}</td>
                                    <td className="px-5 py-3.5 text-slate-400">{u.branch || "—"}</td>
                                    <td className="px-5 py-3.5 text-slate-500 text-xs">
                                        {u.createdAt ? (
                                            <>
                                                {new Date(u.createdAt).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}
                                                <br />
                                                {new Date(u.createdAt).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })}
                                            </>
                                        ) : "—"}
                                    </td>
                                    <td className="px-5 py-3.5"><StatusBadge isDeleted={u.isDeleted} /></td>
                                    <td className="px-5 py-3.5">
                                        <div className="flex items-center gap-2">
                                            {!u.isDeleted && (
                                                <button
                                                    onClick={() => setModal({ kind: "delete", user: u })}
                                                    title="Delete User"
                                                    className="p-1.5 rounded-lg text-slate-500 hover:text-red-500 hover:bg-red-500/10 transition-colors">
                                                    <Trash2 size={15} />
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>

            <ConfirmModal modal={modal} onConfirm={handleConfirm} onClose={() => setModal(null)} />
        </div>
    );
}
