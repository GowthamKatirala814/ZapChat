import { useEffect, useState, useCallback } from "react";
import {
    Plus,
    Trash2,
    AlertCircle,
    RefreshCw,
    X,
    Loader2,
    Building2,
    Users,
    Hash,
    Calendar,
    MessageSquare,
    Shield,
    CheckCircle2,
    AlertTriangle
} from "lucide-react";
import { getAdminRooms, createAdminRoom, deleteAdminRoom } from "../../api/adminApi";
import type { RoomDto } from "../../api/adminApi";

export default function AdminRoomsPage() {
    const [rooms, setRooms] = useState<RoomDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
        const [toast, setToast] = useState<string | null>(null);

    const [showCreate, setShowCreate] = useState(false);
    const [newName, setNewName] = useState("");
    const [newDesc, setNewDesc] = useState("");
    const [creating, setCreating] = useState(false);
    const [createError, setCreateError] = useState("");

    const [deleteTarget, setDeleteTarget] = useState<RoomDto | null>(null);
    const [deleting, setDeleting] = useState(false);

    const showToast = (msg: string) => {
        setToast(msg);
        setTimeout(() => setToast(null), 3000);
    };

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await getAdminRooms(true);
            setRooms(data);
        } catch {
            setError("Failed to load rooms.");
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    const handleCreate = async () => {
        if (!newName.trim()) { setCreateError("Room name is required."); return; }
        setCreating(true);
        setCreateError("");
        try {
            await createAdminRoom(newName.trim(), newDesc.trim());
            showToast("Room created successfully.");
            setShowCreate(false);
            setNewName("");
            setNewDesc("");
            load();
        } catch {
            setCreateError("Failed to create room. Name may already exist.");
        } finally {
            setCreating(false);
        }
    };

    const handleDelete = async () => {
        if (!deleteTarget) return;
        setDeleting(true);
        try {
            await deleteAdminRoom(deleteTarget.id);
            showToast("Room deleted.");
            setDeleteTarget(null);
            load();
        } catch {
            showToast("Failed to delete room.");
            setDeleteTarget(null);
        } finally {
            setDeleting(false);
        }
    };

    const activeRooms = rooms.filter(r => !r.isDeleted);
    const deletedRooms = rooms.filter(r => r.isDeleted);

    // Format date nicely
    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    };

    // Get gradient based on room name
    const getRoomGradient = (name: string) => {
        const gradients = [
            'from-violet-500 to-purple-600',
            'from-blue-500 to-cyan-500',
            'from-emerald-500 to-teal-500',
            'from-rose-500 to-pink-500',
            'from-amber-500 to-orange-500',
            'from-indigo-500 to-blue-500'
        ];
        const index = name.charCodeAt(0) % gradients.length;
        return gradients[index];
    };

    return (
        <div className="min-h-screen p-6 space-y-6" style={{ background: 'linear-gradient(180deg, #0a0f1a 0%, #0d1424 100%)' }}>
            {/* Toast */}
            {toast && (
                <div className="fixed top-4 right-4 z-50 px-4 py-3 rounded-xl text-sm font-medium text-white shadow-2xl"
                    style={{ background: "rgba(30,41,59,0.97)", border: "1px solid rgba(255,255,255,0.1)" }}>
                    {toast}
                </div>
            )}

            {/* Premium Header */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div>
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl flex items-center justify-center"
                            style={{ background: 'linear-gradient(135deg, #7c3aed, #a855f7)', boxShadow: '0 4px 20px rgba(124,58,237,0.3)' }}>
                            <Building2 size={20} className="text-white" />
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold text-white">Room Management</h1>
                            <p className="text-sm text-slate-400">
                                {activeRooms.length} active · {deletedRooms.length} deleted
                            </p>
                        </div>
                    </div>
                </div>
                <div className="flex items-center gap-3">
                    <button onClick={() => setShowCreate(true)}
                        className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold text-white transition-all hover:scale-105"
                        style={{ background: 'linear-gradient(135deg,#7c3aed,#a855f7)', boxShadow: '0 4px 20px rgba(124,58,237,0.4)' }}>
                        <Plus size={16} /> New Room
                    </button>
                    <button onClick={load} disabled={loading}
                        className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm text-slate-300 hover:text-white border border-slate-700 hover:border-slate-500 transition-all disabled:opacity-50"
                        style={{ background: 'rgba(255,255,255,0.03)' }}>
                        <RefreshCw size={16} className={loading ? "animate-spin" : ""} />
                    </button>
                </div>
            </div>

            {/* Error */}
            {error && (
                <div className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm"
                    style={{ background: "rgba(239,68,68,0.1)", border: "1px solid rgba(239,68,68,0.3)", color: "#f87171" }}>
                    <AlertCircle size={16} /> {error}
                </div>
            )}

            {/* Premium Create Modal */}
            {showCreate && (
                <div className="fixed inset-0 z-50 flex items-center justify-center px-4"
                    style={{ background: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(8px)' }}
                    onClick={e => { if (e.target === e.currentTarget) setShowCreate(false); }}>
                    <div className="w-full max-w-md rounded-2xl p-6 space-y-5"
                        style={{ background: 'linear-gradient(180deg, #0f172a 0%, #1e293b 100%)', border: '1px solid rgba(255,255,255,0.1)', boxShadow: '0 24px 60px rgba(0,0,0,0.8)' }}>
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                                <div className="w-10 h-10 rounded-xl flex items-center justify-center"
                                    style={{ background: 'linear-gradient(135deg, #7c3aed, #a855f7)' }}>
                                    <Plus size={20} className="text-white" />
                                </div>
                                <div>
                                    <h3 className="text-lg font-bold text-white">Create New Room</h3>
                                    <p className="text-xs text-slate-400">Set up a new workspace channel</p>
                                </div>
                            </div>
                            <button onClick={() => setShowCreate(false)} className="p-2 rounded-lg text-slate-500 hover:text-slate-300 hover:bg-slate-800 transition-all">
                                <X size={20} />
                            </button>
                        </div>

                        <div className="space-y-4">
                            <div>
                                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                                    Room Name <span className="text-red-400">*</span>
                                </label>
                                <div className="relative">
                                    <Hash size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500" />
                                    <input
                                        value={newName}
                                        onChange={e => setNewName(e.target.value)}
                                        placeholder="e.g. general-announcements"
                                        maxLength={50}
                                        className="w-full rounded-xl pl-11 pr-4 py-3 text-sm outline-none transition-all"
                                        style={{
                                            background: 'rgba(255,255,255,0.05)',
                                            border: '1px solid rgba(255,255,255,0.1)',
                                            color: '#f1f5f9'
                                        }}
                                        onFocus={e => (e.currentTarget.style.borderColor = '#7c3aed')}
                                        onBlur={e => (e.currentTarget.style.borderColor = 'rgba(255,255,255,0.1)')}
                                    />
                                </div>
                                <p className="text-xs text-slate-500 mt-1.5">
                                    {newName.length}/50 characters • Min 2 characters
                                </p>
                            </div>
                            <div>
                                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                                    Description <span className="text-slate-500">(optional)</span>
                                </label>
                                <textarea
                                    value={newDesc}
                                    onChange={e => setNewDesc(e.target.value)}
                                    placeholder="What is this room about?"
                                    rows={3}
                                    maxLength={500}
                                    className="w-full rounded-xl px-4 py-3 text-sm outline-none resize-none transition-all"
                                    style={{
                                        background: 'rgba(255,255,255,0.05)',
                                        border: '1px solid rgba(255,255,255,0.1)',
                                        color: '#f1f5f9'
                                    }}
                                    onFocus={e => (e.currentTarget.style.borderColor = '#7c3aed')}
                                    onBlur={e => (e.currentTarget.style.borderColor = 'rgba(255,255,255,0.1)')}
                                />
                                <p className="text-xs text-slate-500 mt-1.5">
                                    {newDesc.length}/500 characters
                                </p>
                            </div>
                        </div>

                        {createError && (
                            <div className="flex items-center gap-2 px-4 py-3 rounded-xl text-sm"
                                style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.3)', color: '#f87171' }}>
                                <AlertCircle size={16} />
                                {createError}
                            </div>
                        )}

                        <div className="flex gap-3 pt-2">
                            <button onClick={() => setShowCreate(false)} disabled={creating}
                                className="flex-1 py-3 rounded-xl text-sm font-medium border border-slate-700 text-slate-300 hover:text-white hover:border-slate-500 transition-all disabled:opacity-50"
                                style={{ background: 'rgba(255,255,255,0.03)' }}>
                                Cancel
                            </button>
                            <button onClick={handleCreate}
                                disabled={creating || !newName.trim() || newName.trim().length < 2}
                                className="flex-1 py-3 rounded-xl text-sm font-semibold text-white flex items-center justify-center gap-2 transition-all disabled:opacity-40 hover:scale-[1.02]"
                                style={{ background: 'linear-gradient(135deg,#7c3aed,#a855f7)', boxShadow: '0 4px 20px rgba(124,58,237,0.4)' }}>
                                {creating ? <Loader2 size={16} className="animate-spin" /> : <CheckCircle2 size={16} />}
                                {creating ? "Creating..." : "Create Room"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Premium Delete Confirmation */}
            {deleteTarget && (
                <div className="fixed inset-0 z-50 flex items-center justify-center px-4"
                    style={{ background: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(8px)' }}
                    onClick={e => { if (e.target === e.currentTarget) setDeleteTarget(null); }}>
                    <div className="w-full max-w-md rounded-2xl p-6 space-y-5"
                        style={{ background: 'linear-gradient(180deg, #0f172a 0%, #1e293b 100%)', border: '1px solid rgba(239,68,68,0.3)', boxShadow: '0 24px 60px rgba(0,0,0,0.8)' }}>
                        <div className="flex items-center gap-4">
                            <div className="w-12 h-12 rounded-xl flex items-center justify-center"
                                style={{ background: 'rgba(239,68,68,0.15)' }}>
                                <AlertTriangle size={24} style={{ color: '#ef4444' }} />
                            </div>
                            <div>
                                <h3 className="text-lg font-bold text-white">Delete Room</h3>
                                <p className="text-sm text-slate-400">This action cannot be undone</p>
                            </div>
                        </div>

                        <div className="p-4 rounded-xl" style={{ background: 'rgba(239,68,68,0.05)', border: '1px solid rgba(239,68,68,0.2)' }}>
                            <p className="text-sm text-slate-300">
                                You are about to delete <span className="font-semibold text-white">{deleteTarget.name}</span>
                            </p>
                            <ul className="mt-3 space-y-2 text-xs text-slate-400">
                                <li className="flex items-center gap-2">
                                    <Users size={12} />
                                    {deleteTarget.memberCount} members will lose access
                                </li>
                                <li className="flex items-center gap-2">
                                    <MessageSquare size={12} />
                                    All messages will be archived
                                </li>
                                <li className="flex items-center gap-2">
                                    <Shield size={12} />
                                    Room can be recovered from database if needed
                                </li>
                            </ul>
                        </div>

                        <div className="flex gap-3">
                            <button onClick={() => setDeleteTarget(null)} disabled={deleting}
                                className="flex-1 py-3 rounded-xl text-sm font-medium border border-slate-700 text-slate-300 hover:text-white hover:border-slate-500 transition-all disabled:opacity-50"
                                style={{ background: 'rgba(255,255,255,0.03)' }}>
                                Cancel
                            </button>
                            <button onClick={handleDelete} disabled={deleting}
                                className="flex-1 py-3 rounded-xl text-sm font-semibold text-white flex items-center justify-center gap-2 transition-all hover:scale-[1.02]"
                                style={{ background: '#ef4444', boxShadow: '0 4px 20px rgba(239,68,68,0.4)' }}>
                                {deleting ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                                {deleting ? "Deleting..." : "Delete Room"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Premium Rooms Grid */}
            {loading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <div key={i} className="rounded-2xl h-48 border border-slate-800 animate-pulse"
                            style={{ background: 'rgba(15,23,42,0.5)' }} />
                    ))}
                </div>
            ) : rooms.length === 0 ? (
                <div className="rounded-2xl border border-slate-800 py-20 text-center"
                    style={{ background: 'rgba(15,23,42,0.5)' }}>
                    <div className="w-16 h-16 rounded-2xl mx-auto mb-4 flex items-center justify-center"
                        style={{ background: 'rgba(124,58,237,0.15)' }}>
                        <Building2 size={32} style={{ color: '#a78bfa' }} />
                    </div>
                    <p className="text-slate-400 text-lg font-medium">No rooms yet</p>
                    <p className="text-slate-500 text-sm mt-1">Create your first workspace channel</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                    {rooms.map(room => (
                        <div key={room.id}
                            className="group rounded-2xl p-5 transition-all duration-300 hover:scale-[1.02]"
                            style={{
                                background: room.isDeleted
                                    ? 'linear-gradient(180deg, rgba(100,116,139,0.1) 0%, rgba(71,85,105,0.05) 100%)'
                                    : 'linear-gradient(180deg, rgba(30,41,59,0.8) 0%, rgba(15,23,42,0.9) 100%)',
                                border: room.isDeleted
                                    ? '1px solid rgba(100,116,139,0.3)'
                                    : '1px solid rgba(255,255,255,0.08)',
                                boxShadow: room.isDeleted
                                    ? 'none'
                                    : '0 4px 20px rgba(0,0,0,0.3), 0 0 0 1px rgba(255,255,255,0.02)',
                                opacity: room.isDeleted ? 0.7 : 1
                            }}>
                            {/* Header with Icon and Actions */}
                            <div className="flex items-start justify-between mb-4">
                                <div className="flex items-center gap-3">
                                    <div className={`w-12 h-12 rounded-2xl flex items-center justify-center shrink-0 bg-gradient-to-br ${getRoomGradient(room.name)}`}
                                        style={{ boxShadow: '0 4px 15px rgba(0,0,0,0.3)' }}>
                                        <Hash size={24} className="text-white" />
                                    </div>
                                    <div>
                                        <h3 className="font-bold text-white text-base">{room.name}</h3>
                                        <div className="flex items-center gap-2 mt-1">
                                            {room.isDeleted ? (
                                                <span className="px-2 py-0.5 rounded-md text-xs font-medium"
                                                    style={{ background: 'rgba(100,116,139,0.2)', color: '#94a3b8' }}>
                                                    Deleted
                                                </span>
                                            ) : (
                                                <span className="px-2 py-0.5 rounded-md text-xs font-medium flex items-center gap-1"
                                                    style={{ background: 'rgba(34,197,94,0.15)', color: '#4ade80' }}>
                                                    <span className="w-1.5 h-1.5 rounded-full bg-green-400"></span>
                                                    Active
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                </div>
                                {!room.isDeleted && (
                                    <button onClick={() => setDeleteTarget(room)}
                                        className="p-2 rounded-xl text-slate-500 hover:text-red-400 hover:bg-red-500/10 transition-all opacity-0 group-hover:opacity-100"
                                        title="Delete room">
                                        <Trash2 size={18} />
                                    </button>
                                )}
                            </div>

                            {/* Description */}
                            {room.description ? (
                                <p className="text-sm text-slate-400 line-clamp-2 mb-4">{room.description}</p>
                            ) : (
                                <p className="text-sm text-slate-600 italic mb-4">No description</p>
                            )}

                            {/* Stats Row */}
                            <div className="flex items-center gap-4 mb-4">
                                <div className="flex items-center gap-1.5 text-sm"
                                    style={{ color: room.memberCount > 0 ? '#60a5fa' : '#64748b' }}>
                                    <Users size={14} />
                                    <span className="font-semibold">{room.memberCount}</span>
                                    <span className="text-slate-500">members</span>
                                </div>
                            </div>

                            {/* Footer */}
                            <div className="pt-4 border-t border-slate-700/50 flex items-center justify-between">
                                <div className="flex items-center gap-1.5 text-xs text-slate-500">
                                    <Calendar size={12} />
                                    <span>{formatDate(room.createdAt)}</span>
                                </div>
                                {room.isDeleted && room.deletedAt && (
                                    <span className="text-xs text-slate-600">
                                        Deleted {formatDate(room.deletedAt)}
                                    </span>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
