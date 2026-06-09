import { useEffect, useState } from "react";
import type { Dispatch, SetStateAction } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import {
    Hash,
    MessageSquare,
    LogOut,
    Search,
    BarChart3,
    Bell,
    ChevronDown,
    ChevronRight,
    Users,
} from "lucide-react";
import { getUsers } from "../api/authApi";
import type { User } from "../types/User";
import { logout, getAnonymousName } from "../utils/auth";

interface Props {
    selectedRoom: string;
    setSelectedRoom: Dispatch<SetStateAction<string>>;
}

const ROOMS = [
    { name: "General Chat", desc: "Company-wide announcements" },
    { name: "HR Issues",    desc: "Human resources discussions" },
    { name: "Hyderabad",    desc: "Hyderabad office" },
    { name: "Bangalore",    desc: "Bangalore office" },
];

// Consistent gradient palette per user initial
const AVATAR_GRADIENTS = [
    "from-sky-500 to-blue-600",
    "from-violet-500 to-purple-600",
    "from-emerald-500 to-teal-600",
    "from-rose-500 to-pink-600",
    "from-amber-500 to-orange-600",
    "from-cyan-500 to-sky-600",
];

function avatarGradient(name: string) {
    const idx = name.charCodeAt(0) % AVATAR_GRADIENTS.length;
    return AVATAR_GRADIENTS[idx];
}

export default function Sidebar({ selectedRoom, setSelectedRoom }: Props) {
    const navigate    = useNavigate();
    const location    = useLocation();
    const [users, setUsers]               = useState<User[]>([]);
    const [dmSearch, setDmSearch]         = useState("");
    const [dmOpen, setDmOpen]             = useState(true);
    const [channelsOpen, setChannelsOpen] = useState(true);

    const myName       = getAnonymousName();
    const myEmail      = localStorage.getItem("email") ?? "";
    const currentUserId = localStorage.getItem("userId") ?? "";

    useEffect(() => {
        getUsers()
            .then(data => setUsers(data.filter(u => u.id !== currentUserId)))
            .catch(console.error);
    }, [currentUserId]);

    const filteredUsers = users.filter(u =>
        u.anonymousName.toLowerCase().includes(dmSearch.toLowerCase())
    );

    const isToolActive = (path: string) => location.pathname === path;

    return (
        <div
            className="h-full flex flex-col overflow-hidden"
            style={{
                background: "linear-gradient(180deg, #0d1628 0%, #0a1120 100%)",
                borderRight: "1px solid rgba(255,255,255,0.06)",
            }}
        >
            {/* ── Logo / Workspace header ─────────────────────────────── */}
            <div
                className="px-4 py-4 flex items-center gap-3 shrink-0"
                style={{ borderBottom: "1px solid rgba(255,255,255,0.06)" }}
            >
                <div
                    className="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 shadow-lg"
                    style={{
                        background: "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                        boxShadow: "0 0 16px rgba(6,182,212,0.4)",
                    }}
                >
                    <span className="text-base font-black text-white select-none">Z</span>
                </div>
                <div className="flex-1 min-w-0">
                    <div className="text-sm font-bold text-white leading-none">ZapPulse</div>
                    <div className="flex items-center gap-1.5 mt-1">
                        <span
                            className="w-1.5 h-1.5 rounded-full shrink-0"
                            style={{ background: "#22c55e" }}
                        />
                        <span className="text-xs text-slate-400">Anonymous Mode</span>
                    </div>
                </div>
                <ChevronDown size={13} className="text-slate-600 shrink-0" />
            </div>

            {/* ── Scrollable nav ──────────────────────────────────────── */}
            <div className="flex-1 overflow-y-auto py-3 space-y-1"
                style={{ scrollbarWidth: "none" }}>

                {/* ── WORKSPACES (Channels) ── */}
                <div className="px-2">
                    <button
                        onClick={() => setChannelsOpen(v => !v)}
                        className="w-full flex items-center justify-between px-2 py-1.5 rounded-lg group"
                        style={{ color: "#64748b" }}
                        onMouseEnter={e => (e.currentTarget.style.color = "#94a3b8")}
                        onMouseLeave={e => (e.currentTarget.style.color = "#64748b")}
                    >
                        <span className="text-[10px] font-bold uppercase tracking-widest">
                            Workspaces
                        </span>
                        {channelsOpen
                            ? <ChevronDown size={11} />
                            : <ChevronRight size={11} />}
                    </button>

                    {channelsOpen && (
                        <div className="mt-1 space-y-0.5">
                            {ROOMS.map(room => {
                                const active = selectedRoom === room.name;
                                return (
                                    <button
                                        key={room.name}
                                        onClick={() => setSelectedRoom(room.name)}
                                        className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-left text-sm transition-all duration-150 group"
                                        style={active ? {
                                            background: "rgba(6,182,212,0.15)",
                                            color: "#06b6d4",
                                        } : {
                                            color: "#94a3b8",
                                        }}
                                        onMouseEnter={e => {
                                            if (!active) {
                                                e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                                                e.currentTarget.style.color = "#e2e8f0";
                                            }
                                        }}
                                        onMouseLeave={e => {
                                            if (!active) {
                                                e.currentTarget.style.background = "transparent";
                                                e.currentTarget.style.color = "#94a3b8";
                                            }
                                        }}
                                    >
                                        {active && (
                                            <span
                                                className="absolute left-0 w-0.5 h-6 rounded-r"
                                                style={{ background: "#06b6d4" }}
                                            />
                                        )}
                                        <Hash
                                            size={14}
                                            className="shrink-0"
                                            style={{ opacity: active ? 1 : 0.6 }}
                                        />
                                        <span className="truncate flex-1 font-medium text-[13px]">
                                            {room.name}
                                        </span>
                                        {active && (
                                            <span
                                                className="w-1.5 h-1.5 rounded-full shrink-0"
                                                style={{ background: "#06b6d4" }}
                                            />
                                        )}
                                    </button>
                                );
                            })}
                        </div>
                    )}
                </div>

                {/* ── Divider ── */}
                <div
                    className="mx-4 my-2"
                    style={{ height: "1px", background: "rgba(255,255,255,0.05)" }}
                />

                {/* ── DIRECT MESSAGES ── */}
                <div className="px-2">
                    <button
                        onClick={() => setDmOpen(v => !v)}
                        className="w-full flex items-center justify-between px-2 py-1.5 rounded-lg"
                        style={{ color: "#64748b" }}
                        onMouseEnter={e => (e.currentTarget.style.color = "#94a3b8")}
                        onMouseLeave={e => (e.currentTarget.style.color = "#64748b")}
                    >
                        <span className="text-[10px] font-bold uppercase tracking-widest">
                            Direct Messages
                        </span>
                        {dmOpen
                            ? <ChevronDown size={11} />
                            : <ChevronRight size={11} />}
                    </button>

                    {dmOpen && (
                        <>
                            {/* Search users */}
                            <div className="relative mt-1.5 mb-1.5 px-1">
                                <Search
                                    size={12}
                                    className="absolute left-4 top-1/2 -translate-y-1/2"
                                    style={{ color: "#475569" }}
                                />
                                <input
                                    type="text"
                                    value={dmSearch}
                                    onChange={e => setDmSearch(e.target.value)}
                                    placeholder="Find a person…"
                                    className="w-full pl-7 pr-3 py-2 rounded-lg text-xs outline-none transition-all"
                                    style={{
                                        background: "rgba(255,255,255,0.05)",
                                        border: "1px solid rgba(255,255,255,0.08)",
                                        color: "#e2e8f0",
                                        caretColor: "#06b6d4",
                                    }}
                                    onFocus={e => {
                                        e.target.style.border = "1px solid rgba(6,182,212,0.5)";
                                    }}
                                    onBlur={e => {
                                        e.target.style.border = "1px solid rgba(255,255,255,0.08)";
                                    }}
                                />
                            </div>

                            <div className="space-y-0.5">
                                {filteredUsers.length === 0 && (
                                    <div className="px-3 py-3 text-xs text-center"
                                        style={{ color: "#334155" }}>
                                        {dmSearch ? "No results" : "No other users yet"}
                                    </div>
                                )}
                                {filteredUsers.map(user => (
                                    <button
                                        key={user.id}
                                        onClick={() => navigate(`/dm/${user.id}`)}
                                        className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-left transition-all duration-150 group"
                                        style={{ color: "#94a3b8" }}
                                        onMouseEnter={e => {
                                            e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                                            e.currentTarget.style.color = "#e2e8f0";
                                        }}
                                        onMouseLeave={e => {
                                            e.currentTarget.style.background = "transparent";
                                            e.currentTarget.style.color = "#94a3b8";
                                        }}
                                    >
                                        <div className="relative shrink-0">
                                            <div
                                                className={`w-7 h-7 rounded-full flex items-center justify-center text-[11px] font-bold text-white bg-gradient-to-br ${avatarGradient(user.anonymousName)}`}
                                            >
                                                {user.anonymousName.charAt(0).toUpperCase()}
                                            </div>
                                            {/* Online indicator — static for now */}
                                            <span
                                                className="absolute -bottom-0.5 -right-0.5 w-2 h-2 rounded-full border border-slate-900"
                                                style={{ background: "#334155" }}
                                            />
                                        </div>
                                        <span className="truncate flex-1 text-[13px] font-medium">
                                            {user.anonymousName}
                                        </span>
                                        <MessageSquare
                                            size={12}
                                            className="opacity-0 group-hover:opacity-100 transition-opacity shrink-0"
                                            style={{ color: "#06b6d4" }}
                                        />
                                    </button>
                                ))}
                            </div>
                        </>
                    )}
                </div>

                {/* ── Divider ── */}
                <div
                    className="mx-4 my-2"
                    style={{ height: "1px", background: "rgba(255,255,255,0.05)" }}
                />

                {/* ── TOOLS ── */}
                <div className="px-2">
                    <div className="px-2 py-1.5">
                        <span
                            className="text-[10px] font-bold uppercase tracking-widest"
                            style={{ color: "#64748b" }}
                        >
                            Tools
                        </span>
                    </div>
                    <div className="space-y-0.5 mt-1">
                        {[
                            { icon: BarChart3, label: "Polls", path: "/polls" },
                            { icon: Bell,      label: "Notifications", path: "/notifications" },
                            { icon: Users,     label: "Members", path: "#members" },
                        ].map(({ icon: Icon, label, path }) => {
                            const active = path !== "#members" && isToolActive(path);
                            return (
                                <button
                                    key={label}
                                    onClick={() => path !== "#members" ? navigate(path) : undefined}
                                    className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-left text-[13px] font-medium transition-all duration-150"
                                    style={active ? {
                                        background: "rgba(6,182,212,0.12)",
                                        color: "#06b6d4",
                                    } : { color: "#94a3b8" }}
                                    onMouseEnter={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                                            e.currentTarget.style.color = "#e2e8f0";
                                        }
                                    }}
                                    onMouseLeave={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "transparent";
                                            e.currentTarget.style.color = "#94a3b8";
                                        }
                                    }}
                                >
                                    <Icon size={14} className="shrink-0" />
                                    <span>{label}</span>
                                </button>
                            );
                        })}
                    </div>
                </div>
            </div>

            {/* ── Footer: profile card ──────────────────────────────── */}
            <div
                className="px-3 py-3 shrink-0"
                style={{ borderTop: "1px solid rgba(255,255,255,0.06)" }}
            >
                <div
                    className="flex items-center gap-2.5 p-2.5 rounded-xl transition-all cursor-default"
                    style={{ background: "rgba(255,255,255,0.04)" }}
                >
                    {/* Avatar */}
                    <div
                        className="w-9 h-9 rounded-full shrink-0 flex items-center justify-center text-sm font-bold text-white shadow"
                        style={{
                            background: `linear-gradient(135deg, #0ea5e9, #06b6d4)`,
                            boxShadow: "0 0 10px rgba(6,182,212,0.3)",
                        }}
                    >
                        {myName.charAt(0).toUpperCase()}
                    </div>

                    {/* Info */}
                    <div className="flex-1 min-w-0">
                        <div className="text-sm font-semibold text-white truncate leading-none">
                            {myName}
                        </div>
                        {myEmail && (
                            <div
                                className="text-[10px] mt-0.5 truncate"
                                style={{ color: "#475569" }}
                            >
                                {myEmail}
                            </div>
                        )}
                        <div className="flex items-center gap-1 mt-0.5">
                            <span
                                className="w-1.5 h-1.5 rounded-full"
                                style={{ background: "#22c55e" }}
                            />
                            <span className="text-[10px]" style={{ color: "#22c55e" }}>
                                Active
                            </span>
                        </div>
                    </div>

                    {/* Logout */}
                    <button
                        onClick={logout}
                        title="Sign out"
                        className="p-1.5 rounded-lg transition-colors shrink-0"
                        style={{ color: "#475569" }}
                        onMouseEnter={e => (e.currentTarget.style.color = "#f87171")}
                        onMouseLeave={e => (e.currentTarget.style.color = "#475569")}
                    >
                        <LogOut size={14} />
                    </button>
                </div>
            </div>

            <style>{`
                div::-webkit-scrollbar { display: none; }
            `}</style>
        </div>
    );
}