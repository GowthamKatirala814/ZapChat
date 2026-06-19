import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { connection } from "../hubs/chatHub";
import { getNormalUsers } from "../api/authApi";
import type { User } from "../types/User";
import { MessageSquare, Users, Wifi } from "lucide-react";

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

    // Load all active non-admin platform users (admin and deleted accounts excluded at source)
    useEffect(() => {
        getNormalUsers().then(setUsers).catch(console.error);
    }, []);

    // Listen for presence updates
    useEffect(() => {
        const handlePresence = (names: string[]) => {
            setOnlineNames(new Set(names));
        };

        connection.on("OnlineUsersUpdated", handlePresence);

        // Retry getting online users until connection is ready
        const tryGetOnline = () => {
            if (connection.state === "Connected") {
                connection.invoke("GetOnlineUsers")
                    .then((names: string[]) => setOnlineNames(new Set(names)))
                    .catch(() => { /* ignore */ });
            } else {
                setTimeout(tryGetOnline, 500);
            }
        };
        tryGetOnline();

        return () => { connection.off("OnlineUsersUpdated", handlePresence); };
    }, []);

    // getNormalUsers already excludes admin + deleted, so all entries are valid participants.
    // Offline = active users who are not currently connected.
    const others  = users.filter(u => u.id !== currentUserId);
    const online  = others.filter(u => onlineNames.has(u.anonymousName));
    const offline = others.filter(u => !onlineNames.has(u.anonymousName));

    return (
        <div
            className="h-full flex flex-col bg-white"
            style={{ borderLeft: "1px solid #E2E8F0" }}
        >
            {/* ── Room Info header ──────────────────────────────────── */}
            <div
                className="px-4 py-4 shrink-0"
                style={{ borderBottom: "1px solid #E2E8F0" }}
            >
                <div className="flex items-center justify-between mb-3">
                    <h2 className="text-xs font-bold uppercase tracking-widest text-slate-500">
                        Room Info
                    </h2>
                    <Wifi size={12} style={{ color: "#22C55E" }} />
                </div>

                {/* Room name badge */}
                <div
                    className="flex items-center gap-2 px-3 py-2.5 rounded-xl"
                    style={{ background: "#EFF6FF", border: "1px solid #BAE6FD" }}
                >
                    <div
                        className="w-6 h-6 rounded-lg flex items-center justify-center shrink-0"
                        style={{ background: "#DBEAFE" }}
                    >
                        <span className="text-xs font-bold" style={{ color: "#0EA5E9" }}>#</span>
                    </div>
                    <div className="min-w-0">
                        <div className="text-xs font-semibold text-slate-800 truncate">
                            {roomName ?? "General Chat"}
                        </div>
                        <div className="text-[10px] mt-0.5 text-slate-500">
                            Group channel
                        </div>
                    </div>
                </div>

                {/* Quick stats */}
                <div className="grid grid-cols-2 gap-2 mt-3">
                    <div
                        className="flex flex-col items-center py-2.5 rounded-xl"
                        style={{ background: "#F8FAFC", border: "1px solid #E2E8F0" }}
                    >
                        <div className="text-base font-bold text-slate-800">
                            {others.length}
                        </div>
                        <div className="text-[10px] mt-0.5 text-slate-500">Members</div>
                    </div>
                    <div
                        className="flex flex-col items-center py-2.5 rounded-xl"
                        style={{ background: "#F0FDF4", border: "1px solid #BBF7D0" }}
                    >
                        <div className="text-base font-bold" style={{ color: "#16A34A" }}>
                            {online.length}
                        </div>
                        <div className="text-[10px] mt-0.5 text-slate-500">Online</div>
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
                                style={{ background: "#22C55E" }}
                            />
                            <span className="text-[10px] font-bold uppercase tracking-widest text-slate-500">
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
                                className="w-1.5 h-1.5 rounded-full bg-slate-300"
                            />
                            <span className="text-[10px] font-bold uppercase tracking-widest text-slate-400">
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
                        <Users size={28} className="mb-3 text-slate-300" />
                        <p className="text-sm font-medium text-slate-500">
                            No other members
                        </p>
                        <p className="text-xs mt-1 text-slate-400">
                            Invite colleagues to join
                        </p>
                    </div>
                )}
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
                background: hovered ? "#F1F5F9" : "transparent",
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
                        background: isOnline ? "#22C55E" : "#CBD5E1",
                        borderColor: "#FFFFFF",
                    }}
                />
            </div>

            {/* Name + status */}
            <div className="flex-1 min-w-0">
                <div
                    className="text-xs font-semibold truncate transition-colors"
                    style={{ color: isOnline ? "#0F172A" : "#94A3B8" }}
                >
                    {user.anonymousName}
                </div>
                <div
                    className="text-[10px] mt-0.5"
                    style={{ color: isOnline ? "#16A34A" : "#94A3B8" }}
                >
                    {isOnline ? "Active now" : "Offline"}
                </div>
            </div>

            {/* DM button on hover */}
            <MessageSquare
                size={13}
                className="shrink-0 transition-all duration-150"
                style={{
                    color: "#0EA5E9",
                    opacity: hovered ? 1 : 0,
                    transform: hovered ? "scale(1)" : "scale(0.8)",
                }}
            />
        </div>
    );
}