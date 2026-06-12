import { useEffect, useRef, useState } from "react";
import type { Dispatch, SetStateAction } from "react";
import { useNavigate } from "react-router-dom";
import {
    Hash,
    MessageSquare,
    LogOut,
    Search,
    ChevronDown,
    ChevronRight,
} from "lucide-react";
import { getUsers } from "../api/authApi";
import { getRooms, type Room } from "../api/chatApi";
import { connection as chatHubConnection } from "../hubs/chatHub";
import { getPrivateChatConnection } from "../hubs/privateChatHub";
import type { User } from "../types/User";
import { logout, getAnonymousName } from "../utils/auth";
import type { Message } from "../types/Message";

interface Props {
    selectedRoom: string | null;
    setSelectedRoom: Dispatch<SetStateAction<string | null>>;
}

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
    const [users, setUsers]               = useState<User[]>([]);
    const [rooms, setRooms]               = useState<Room[]>([]);
    const [roomsLoading, setRoomsLoading] = useState(true);
    const [dmSearch, setDmSearch]         = useState("");
    const [dmOpen, setDmOpen]             = useState(true);
    const [channelsOpen, setChannelsOpen] = useState(true);

    // Unread & Last Message states
    const [unread, setUnread]                                   = useState<Map<string, number>>(new Map());
    const [roomLastMessage, setRoomLastMessage]                 = useState<Map<string, { text: string; sentAt: string }>>(new Map());
    const [dmOrder, setDmOrder]                                 = useState<string[]>([]);
    const [dmLastMessage, setDmLastMessage]                     = useState<Map<string, { text: string; sentAt: string }>>(new Map());
    const [dmUnread, setDmUnread]                               = useState<Map<string, number>>(new Map());

    const myName       = getAnonymousName();
    const myEmail      = localStorage.getItem("email") ?? "";
    const currentUserId = localStorage.getItem("userId") ?? "";

    const joinedRoomRef = useRef<string | null>(null);

    useEffect(() => {
        getUsers()
            .then(data => setUsers(data.filter(u => u.id !== currentUserId)))
            .catch(console.error);
    }, [currentUserId]);

    useEffect(() => {
        loadRooms();
    }, []);

    const loadRooms = async () => {
        setRoomsLoading(true);
        try {
            const data = await getRooms();
            setRooms(data);
        } catch (err) {
            console.error("Failed to load rooms:", err);
        } finally {
            setRoomsLoading(false);
        }
    };

    useEffect(() => {
        joinedRoomRef.current = selectedRoom;
    }, [selectedRoom]);

    // ── Rooms SignalR Integration ──
    useEffect(() => {
        const handleReceiveMessage = (data: Message) => {
            const roomName = joinedRoomRef.current;
            if (!roomName) return;

            setRooms(prev => {
                const idx = prev.findIndex(r => r.name === roomName);
                if (idx <= 0) return prev;
                const next = [...prev];
                const [moved] = next.splice(idx, 1);
                next.unshift(moved);
                return next;
            });

            setRoomLastMessage(prev => {
                const next = new Map(prev);
                const textPreview = data.message.length > 40 ? data.message.substring(0, 40) + "..." : data.message;
                next.set(roomName, { 
                    text: `${data.anonymousName}: ${textPreview}`, 
                    sentAt: data.sentAt 
                });
                return next;
            });
        };

        const handleGlobalNotification = (data: { roomName: string, message: string, createdAt: string }) => {
            const roomName = data.roomName;
            if (!roomName) return;

            setRooms(prev => {
                const idx = prev.findIndex(r => r.name === roomName);
                if (idx <= 0) return prev;
                const next = [...prev];
                const [moved] = next.splice(idx, 1);
                next.unshift(moved);
                return next;
            });

            setRoomLastMessage(prev => {
                const next = new Map(prev);
                const textPreview = data.message.length > 40 ? data.message.substring(0, 40) + "..." : data.message;
                next.set(roomName, { 
                    text: textPreview, 
                    sentAt: data.createdAt 
                });
                return next;
            });

            if (roomName !== joinedRoomRef.current) {
                setUnread(prev => {
                    const next = new Map(prev);
                    next.set(roomName, (next.get(roomName) ?? 0) + 1);
                    return next;
                });
            }
        };

        chatHubConnection.on("ReceiveMessage", handleReceiveMessage);
        chatHubConnection.on("GlobalNotification", handleGlobalNotification);
        return () => {
            chatHubConnection.off("ReceiveMessage", handleReceiveMessage);
            chatHubConnection.off("GlobalNotification", handleGlobalNotification);
        };
    }, []);

    // ── Private Chat SignalR Integration ──
    useEffect(() => {
        const pConn = getPrivateChatConnection();
        const handlePrivateMessage = (data: any) => {
            const senderId = data.senderId;
            const textPreview = data.content.length > 40 ? data.content.substring(0, 40) + "..." : data.content;

            setDmOrder(prev => {
                const next = prev.filter(id => id !== senderId);
                next.unshift(senderId);
                return next;
            });

            setDmLastMessage(prev => {
                const next = new Map(prev);
                next.set(senderId, {
                    text: textPreview,
                    sentAt: data.sentAt
                });
                return next;
            });

            if (!window.location.pathname.includes(`/dm/${senderId}`)) {
                setDmUnread(prev => {
                    const next = new Map(prev);
                    next.set(senderId, (next.get(senderId) ?? 0) + 1);
                    return next;
                });
            }
        };

        pConn.on("ReceivePrivateMessage", handlePrivateMessage);
        
        if (pConn.state === "Disconnected") {
            pConn.start().catch(() => {});
        }

        return () => {
            pConn.off("ReceivePrivateMessage", handlePrivateMessage);
        };
    }, []);


    const handleSelectRoom = (roomName: string) => {
        setUnread(prev => {
            const next = new Map(prev);
            next.delete(roomName);
            return next;
        });
        setSelectedRoom(roomName);
    };

    const handleSelectDM = (userId: string) => {
        setDmUnread(prev => {
            const next = new Map(prev);
            next.delete(userId);
            return next;
        });
        navigate(`/dm/${userId}`);
    };

    const sortedUsers = [...users].sort((a, b) => {
        const aIndex = dmOrder.indexOf(a.id);
        const bIndex = dmOrder.indexOf(b.id);
        if (aIndex !== -1 && bIndex !== -1) return aIndex - bIndex;
        if (aIndex !== -1) return -1;
        if (bIndex !== -1) return 1;
        return 0;
    });

    const filteredUsers = sortedUsers.filter(u =>
        u.anonymousName.toLowerCase().includes(dmSearch.toLowerCase())
    );

    return (
        <div
            className="h-full flex flex-col overflow-hidden"
            style={{
                background: "linear-gradient(180deg, #0d1628 0%, #0a1120 100%)",
                borderRight: "1px solid rgba(255,255,255,0.06)",
            }}
        >
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
                    <div className="text-sm font-bold text-white leading-none">Zap<span style={{ color: "#38BDF8" }}>Com</span></div>
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

            <div className="flex-1 overflow-y-auto py-3 space-y-1"
                style={{ scrollbarWidth: "none" }}>

                {/* ── WORKSPACES ── */}
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
                            {roomsLoading ? (
                                <div className="px-3 py-2 text-xs text-slate-500">Loading rooms...</div>
                            ) : rooms.length === 0 ? (
                                <div className="px-3 py-2 text-xs text-slate-500">No rooms available</div>
                            ) : (
                                rooms.map(room => {
                                    const active = selectedRoom === room.name;
                                    const roomUnread = unread.get(room.name) ?? 0;
                                    const lastMsg = roomLastMessage.get(room.name);
                                    
                                    return (
                                        <button
                                            key={room.id}
                                            onClick={() => handleSelectRoom(room.name)}
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
                                            <div className="flex-1 min-w-0 flex flex-col justify-center">
                                                <span className="truncate font-medium text-[13px] leading-tight">
                                                    {room.name}
                                                </span>
                                                {lastMsg && (
                                                    <span className="text-[11px] text-slate-500 truncate block mt-0.5 leading-tight">
                                                        {lastMsg.text}
                                                    </span>
                                                )}
                                            </div>
                                            
                                            {!active && roomUnread > 0 && (
                                                <span
                                                    className="min-w-[18px] h-[18px] px-1 rounded-full text-[10px] font-bold text-white flex items-center justify-center shrink-0 leading-none"
                                                    style={{ background: "#06b6d4" }}
                                                >
                                                    {roomUnread > 9 ? "9+" : roomUnread}
                                                </span>
                                            )}
                                            {active && (
                                                <span
                                                    className="w-1.5 h-1.5 rounded-full shrink-0"
                                                    style={{ background: "#06b6d4" }}
                                                />
                                            )}
                                        </button>
                                    );
                                })
                            )}
                        </div>
                    )}
                </div>

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
                                {filteredUsers.map(user => {
                                    const dmMsg = dmLastMessage.get(user.id);
                                    const uCount = dmUnread.get(user.id) ?? 0;
                                    const isDmActive = window.location.pathname.includes(`/dm/${user.id}`);

                                    return (
                                        <button
                                            key={user.id}
                                            onClick={() => handleSelectDM(user.id)}
                                            className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-left transition-all duration-150 group"
                                            style={isDmActive ? {
                                                background: "rgba(6,182,212,0.15)",
                                                color: "#06b6d4",
                                            } : {
                                                color: "#94a3b8",
                                            }}
                                            onMouseEnter={e => {
                                                if (!isDmActive) {
                                                    e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                                                    e.currentTarget.style.color = "#e2e8f0";
                                                }
                                            }}
                                            onMouseLeave={e => {
                                                if (!isDmActive) {
                                                    e.currentTarget.style.background = "transparent";
                                                    e.currentTarget.style.color = "#94a3b8";
                                                }
                                            }}
                                        >
                                            {isDmActive && (
                                                <span
                                                    className="absolute left-0 w-0.5 h-6 rounded-r"
                                                    style={{ background: "#06b6d4" }}
                                                />
                                            )}
                                            <div className="relative shrink-0">
                                                <div
                                                    className={`w-7 h-7 rounded-full flex items-center justify-center text-[11px] font-bold text-white bg-gradient-to-br ${avatarGradient(user.anonymousName)}`}
                                                >
                                                    {user.anonymousName.charAt(0).toUpperCase()}
                                                </div>
                                                <span
                                                    className="absolute -bottom-0.5 -right-0.5 w-2 h-2 rounded-full border border-slate-900"
                                                    style={{ background: "#334155" }}
                                                />
                                            </div>
                                            <div className="flex-1 min-w-0 flex flex-col justify-center">
                                                <span className="truncate font-medium text-[13px] leading-tight">
                                                    {user.anonymousName}
                                                </span>
                                                {dmMsg && (
                                                    <span className="text-[11px] text-slate-500 truncate block mt-0.5 leading-tight">
                                                        {dmMsg.text}
                                                    </span>
                                                )}
                                            </div>

                                            {!isDmActive && uCount > 0 && (
                                                <span
                                                    className="min-w-[18px] h-[18px] px-1 rounded-full text-[10px] font-bold text-white flex items-center justify-center shrink-0 leading-none"
                                                    style={{ background: "#06b6d4" }}
                                                >
                                                    {uCount > 9 ? "9+" : uCount}
                                                </span>
                                            )}
                                        </button>
                                    );
                                })}
                            </div>
                        </>
                    )}
                </div>

            </div>

            <div
                className="px-3 py-3 shrink-0"
                style={{ borderTop: "1px solid rgba(255,255,255,0.06)" }}
            >
                <div
                    className="flex items-center gap-2.5 p-2.5 rounded-xl transition-all cursor-default"
                    style={{ background: "rgba(255,255,255,0.04)" }}
                >
                    <div
                        className="w-9 h-9 rounded-full shrink-0 flex items-center justify-center text-sm font-bold text-white shadow"
                        style={{
                            background: `linear-gradient(135deg, #0ea5e9, #06b6d4)`,
                            boxShadow: "0 0 10px rgba(6,182,212,0.3)",
                        }}
                    >
                        {myName.charAt(0).toUpperCase()}
                    </div>

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