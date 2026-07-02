import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { connection } from "../hubs/chatHub";
import { getNormalUsers } from "../api/authApi";
import type { User } from "../types/User";
import { MessageSquare, Users, Wifi } from "lucide-react";
import { useTheme } from "../context/ThemeContext";

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
    const { isDark }                  = useTheme();

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

    const panelBg  = isDark ? "#0f172a" : "#ffffff";
    const border   = isDark ? "rgba(255,255,255,0.07)" : "#E2E8F0";
    const hdrText  = isDark ? "#94a3b8" : "#64748b";
    const roomBg   = isDark ? "rgba(14,165,233,0.1)" : "#EFF6FF";
    const roomBord = isDark ? "rgba(14,165,233,0.2)" : "#BAE6FD";
    const statBg   = isDark ? "rgba(255,255,255,0.04)" : "#F8FAFC";
    const statBord = isDark ? "rgba(255,255,255,0.06)" : "#E2E8F0";
    const statText = isDark ? "#f1f5f9" : "#0f172a";
    const onlineBg = isDark ? "rgba(34,197,94,0.08)" : "#F0FDF4";
    const onlineBrd= isDark ? "rgba(34,197,94,0.2)" : "#BBF7D0";

    return (
        <div
            className="h-full flex flex-col"
            style={{ background: panelBg, borderLeft: `1px solid ${border}` }}
        >
            {/* ── Room Info header ──────────────────────────────────── */}
            <div className="px-4 py-4 shrink-0" style={{ borderBottom: `1px solid ${border}` }}>
                <div className="flex items-center justify-between mb-3">
                    <h2 className="text-xs font-bold uppercase tracking-widest" style={{ color: hdrText }}>Room Info</h2>
                    <Wifi size={12} style={{ color: "#22C55E" }} />
                </div>

                {/* Room name badge */}
                <div className="flex items-center gap-2 px-3 py-2.5 rounded-xl" style={{ background: roomBg, border: `1px solid ${roomBord}` }}>
                    <div className="w-6 h-6 rounded-lg flex items-center justify-center shrink-0" style={{ background: isDark ? "rgba(14,165,233,0.15)" : "#DBEAFE" }}>
                        <span className="text-xs font-bold" style={{ color: "#0EA5E9" }}>#</span>
                    </div>
                    <div className="min-w-0">
                        <div className="text-xs font-semibold truncate" style={{ color: statText }}>{roomName ?? "General Chat"}</div>
                        <div className="text-[10px] mt-0.5" style={{ color: hdrText }}>Group channel</div>
                    </div>
                </div>

                <div className="grid grid-cols-2 gap-2 mt-3">
                    <div className="flex flex-col items-center py-2.5 rounded-xl" style={{ background: statBg, border: `1px solid ${statBord}` }}>
                        <div className="text-base font-bold" style={{ color: statText }}>{others.length}</div>
                        <div className="text-[10px] mt-0.5" style={{ color: hdrText }}>Members</div>
                    </div>
                    <div className="flex flex-col items-center py-2.5 rounded-xl" style={{ background: onlineBg, border: `1px solid ${onlineBrd}` }}>
                        <div className="text-base font-bold" style={{ color: "#16A34A" }}>{online.length}</div>
                        <div className="text-[10px] mt-0.5" style={{ color: hdrText }}>Online</div>
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
                                        isDark={isDark}
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
                                        isDark={isDark}
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
    isDark: boolean;
}

function MemberRow({ user, isOnline, fromColor, toColor, onDM, isDark }: MemberRowProps) {
    const [hovered, setHovered] = useState(false);

    return (
        <div
            onClick={onDM}
            onMouseEnter={() => setHovered(true)}
            onMouseLeave={() => setHovered(false)}
            className="flex items-center gap-2.5 px-2.5 py-2 rounded-xl cursor-pointer transition-all duration-150 group"
            style={{
                background: hovered ? (isDark ? "rgba(255,255,255,0.05)" : "#F1F5F9") : "transparent",
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
                    style={{ color: isOnline ? (isDark ? "#f1f5f9" : "#0F172A") : "#94A3B8" }}
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