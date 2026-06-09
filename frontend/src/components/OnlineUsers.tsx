import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { connection } from "../hubs/chatHub";
import { getUsers } from "../api/authApi";
import type { User } from "../types/User";
import { MessageSquare, Users, Wifi, PinIcon, Volume2 } from "lucide-react";

interface Props {
    roomName?: string;
}

const AVATAR_GRADIENTS = [
    ["#0ea5e9", "#06b6d4"],
    ["#8b5cf6", "#6d28d9"],
    ["#10b981", "#0d9488"],
    ["#f59e0b", "#d97706"],
    ["#ef4444", "#dc2626"],
    ["#ec4899", "#db2777"],
];

function avatarColors(name: string): [string, string] {
    const idx = name.charCodeAt(0) % AVATAR_GRADIENTS.length;
    return AVATAR_GRADIENTS[idx] as [string, string];
}

export default function OnlineUsers({ roomName }: Props) {
    const [users, setUsers]           = useState<User[]>([]);
    const [onlineNames, setOnlineNames] = useState<Set<string>>(new Set());
    const navigate                    = useNavigate();
    const currentUserId               = localStorage.getItem("userId");

    // Load all registered users
    useEffect(() => {
        getUsers().then(setUsers).catch(console.error);
    }, []);

    // Listen for presence updates
    useEffect(() => {
        const handlePresence = (names: string[]) => {
            setOnlineNames(new Set(names));
        };

        connection.off("OnlineUsersUpdated");
        connection.on("OnlineUsersUpdated", handlePresence);

        if (connection.state === "Connected") {
            connection.invoke("GetOnlineUsers")
                .then((names: string[]) => setOnlineNames(new Set(names)))
                .catch(() => { /* ignore */ });
        }

        return () => { connection.off("OnlineUsersUpdated"); };
    }, []);

    const others  = users.filter(u => u.id !== currentUserId);
    const online  = others.filter(u => onlineNames.has(u.anonymousName));
    const offline = others.filter(u => !onlineNames.has(u.anonymousName));

    return (
        <div
            className="h-full flex flex-col"
            style={{
                background: "linear-gradient(180deg, #0d1628 0%, #0a1120 100%)",
                borderLeft: "1px solid rgba(255,255,255,0.06)",
            }}
        >
            {/* ── Room Info header ──────────────────────────────────── */}
            <div
                className="px-4 py-4 shrink-0"
                style={{ borderBottom: "1px solid rgba(255,255,255,0.06)" }}
            >
                <div className="flex items-center justify-between mb-3">
                    <h2 className="text-xs font-bold uppercase tracking-widest"
                        style={{ color: "#64748b" }}>
                        Room Info
                    </h2>
                    <Wifi size={12} style={{ color: "#22c55e" }} />
                </div>

                {/* Room name badge */}
                <div
                    className="flex items-center gap-2 px-3 py-2.5 rounded-xl"
                    style={{
                        background: "rgba(6,182,212,0.08)",
                        border: "1px solid rgba(6,182,212,0.15)",
                    }}
                >
                    <div
                        className="w-6 h-6 rounded-lg flex items-center justify-center shrink-0"
                        style={{ background: "rgba(6,182,212,0.2)" }}
                    >
                        <span className="text-xs font-bold" style={{ color: "#06b6d4" }}>#</span>
                    </div>
                    <div className="min-w-0">
                        <div className="text-xs font-semibold text-white truncate">
                            {roomName ?? "General Chat"}
                        </div>
                        <div className="text-[10px] mt-0.5" style={{ color: "#475569" }}>
                            Group channel
                        </div>
                    </div>
                </div>

                {/* Quick stats */}
                <div className="grid grid-cols-2 gap-2 mt-3">
                    <div
                        className="flex flex-col items-center py-2.5 rounded-xl"
                        style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.06)" }}
                    >
                        <div className="text-base font-bold text-white">
                            {others.length}
                        </div>
                        <div className="text-[10px] mt-0.5" style={{ color: "#64748b" }}>Members</div>
                    </div>
                    <div
                        className="flex flex-col items-center py-2.5 rounded-xl"
                        style={{ background: "rgba(34,197,94,0.06)", border: "1px solid rgba(34,197,94,0.15)" }}
                    >
                        <div className="text-base font-bold" style={{ color: "#22c55e" }}>
                            {online.length}
                        </div>
                        <div className="text-[10px] mt-0.5" style={{ color: "#64748b" }}>Online</div>
                    </div>
                </div>
            </div>

            {/* ── Members list ─────────────────────────────────────── */}
            <div
                className="flex-1 overflow-y-auto py-3"
                style={{ scrollbarWidth: "none" }}
            >
                {/* Online section */}
                {online.length > 0 && (
                    <div className="px-3 mb-2">
                        <div className="flex items-center gap-1.5 px-2 mb-1.5">
                            <span
                                className="w-1.5 h-1.5 rounded-full"
                                style={{ background: "#22c55e" }}
                            />
                            <span
                                className="text-[10px] font-bold uppercase tracking-widest"
                                style={{ color: "#64748b" }}
                            >
                                Active Now — {online.length}
                            </span>
                        </div>
                        <div className="space-y-0.5">
                            {online.map(user => {
                                const [from, to] = avatarColors(user.anonymousName);
                                return (
                                    <MemberRow
                                        key={user.id}
                                        user={user}
                                        isOnline={true}
                                        fromColor={from}
                                        toColor={to}
                                        onDM={() => navigate(`/dm/${user.id}`)}
                                    />
                                );
                            })}
                        </div>
                    </div>
                )}

                {/* Offline section */}
                {offline.length > 0 && (
                    <div className="px-3 mb-2">
                        <div className="flex items-center gap-1.5 px-2 mb-1.5 mt-1">
                            <span
                                className="w-1.5 h-1.5 rounded-full"
                                style={{ background: "#334155" }}
                            />
                            <span
                                className="text-[10px] font-bold uppercase tracking-widest"
                                style={{ color: "#64748b" }}
                            >
                                Offline — {offline.length}
                            </span>
                        </div>
                        <div className="space-y-0.5">
                            {offline.map(user => {
                                const [from, to] = avatarColors(user.anonymousName);
                                return (
                                    <MemberRow
                                        key={user.id}
                                        user={user}
                                        isOnline={false}
                                        fromColor={from}
                                        toColor={to}
                                        onDM={() => navigate(`/dm/${user.id}`)}
                                    />
                                );
                            })}
                        </div>
                    </div>
                )}

                {others.length === 0 && (
                    <div className="flex flex-col items-center justify-center py-10 px-4 text-center">
                        <Users size={28} className="mb-3" style={{ color: "#1e293b" }} />
                        <p className="text-sm font-medium" style={{ color: "#334155" }}>
                            No other members
                        </p>
                        <p className="text-xs mt-1" style={{ color: "#1e293b" }}>
                            Invite colleagues to join
                        </p>
                    </div>
                )}
            </div>

            {/* ── Future placeholders ───────────────────────────────── */}
            <div
                className="px-3 py-3 space-y-2 shrink-0"
                style={{ borderTop: "1px solid rgba(255,255,255,0.06)" }}
            >
                {[
                    { icon: PinIcon,  label: "Pinned Messages", note: "Coming soon" },
                    { icon: Volume2,  label: "Voice Channels",  note: "Coming soon" },
                ].map(({ icon: Icon, label, note }) => (
                    <div
                        key={label}
                        className="flex items-center gap-2.5 px-3 py-2.5 rounded-xl"
                        style={{
                            background: "rgba(255,255,255,0.03)",
                            border: "1px solid rgba(255,255,255,0.05)",
                        }}
                    >
                        <Icon size={13} style={{ color: "#334155" }} />
                        <div className="min-w-0 flex-1">
                            <div className="text-xs font-medium" style={{ color: "#475569" }}>
                                {label}
                            </div>
                        </div>
                        <span
                            className="text-[9px] px-1.5 py-0.5 rounded-md font-semibold uppercase tracking-wide shrink-0"
                            style={{
                                background: "rgba(255,255,255,0.04)",
                                color: "#334155",
                                border: "1px solid rgba(255,255,255,0.05)",
                            }}
                        >
                            {note}
                        </span>
                    </div>
                ))}
            </div>

            <style>{`
                div::-webkit-scrollbar { display: none; }
            `}</style>
        </div>
    );
}

// ── Sub-component: MemberRow ──────────────────────────────────────────────────
interface MemberRowProps {
    user: User;
    isOnline: boolean;
    fromColor: string;
    toColor: string;
    onDM: () => void;
}

function MemberRow({ user, isOnline, fromColor, toColor, onDM }: MemberRowProps) {
    const [hovered, setHovered] = useState(false);

    return (
        <div
            onClick={onDM}
            onMouseEnter={() => setHovered(true)}
            onMouseLeave={() => setHovered(false)}
            className="flex items-center gap-2.5 px-2.5 py-2 rounded-xl cursor-pointer transition-all duration-150 group"
            style={{
                background: hovered ? "rgba(255,255,255,0.05)" : "transparent",
            }}
        >
            {/* Avatar */}
            <div className="relative shrink-0">
                <div
                    className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white"
                    style={{
                        background: `linear-gradient(135deg, ${fromColor}, ${toColor})`,
                        opacity: isOnline ? 1 : 0.5,
                    }}
                >
                    {user.anonymousName.charAt(0).toUpperCase()}
                </div>
                {/* Online dot */}
                <span
                    className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2"
                    style={{
                        background: isOnline ? "#22c55e" : "#334155",
                        borderColor: "#0a1120",
                    }}
                />
            </div>

            {/* Name + status */}
            <div className="flex-1 min-w-0">
                <div
                    className="text-xs font-semibold truncate transition-colors"
                    style={{ color: isOnline ? "#e2e8f0" : "#64748b" }}
                >
                    {user.anonymousName}
                </div>
                <div
                    className="text-[10px] mt-0.5"
                    style={{ color: isOnline ? "#22c55e" : "#334155" }}
                >
                    {isOnline ? "Active now" : "Offline"}
                </div>
            </div>

            {/* DM button on hover */}
            <MessageSquare
                size={13}
                className="shrink-0 transition-all duration-150"
                style={{
                    color: "#06b6d4",
                    opacity: hovered ? 1 : 0,
                    transform: hovered ? "scale(1)" : "scale(0.8)",
                }}
            />
        </div>
    );
}