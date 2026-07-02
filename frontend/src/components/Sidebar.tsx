import React, { useEffect, useRef, useState } from "react";
// Removed unused Dispatch, SetStateAction
import { useNavigate, useLocation } from "react-router-dom";
import {
    Hash,
    LogOut,
    Search,
    ChevronDown,
    ChevronRight,
} from "lucide-react";
import { getUsers } from "../api/authApi";
import { getRooms, markRoomAsRead, type Room } from "../api/chatApi";
import { getConversations, markConversationAsRead } from "../api/privateChatApi";
import { markAllAsRead } from "../api/notificationApi";
import { connection as chatHubConnection } from "../hubs/chatHub";
import { getPrivateChatConnection } from "../hubs/privateChatHub";
import type { User } from "../types/User";
import { logout, getAnonymousName } from "../utils/auth";
// Removed Message import

interface Props {
    selectedRoom: string | null;
    setSelectedRoom: ((room: string | null) => void) | React.Dispatch<React.SetStateAction<string | null>>;
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
    const location    = useLocation();
    const [users, setUsers]               = useState<User[]>([]);
    const [rooms, setRooms]               = useState<Room[]>([]);
    const [roomsLoading, setRoomsLoading] = useState(true);
    const [dmSearch, setDmSearch]         = useState("");
    const [dmOpen, setDmOpen]             = useState(true);
    const [channelsOpen, setChannelsOpen] = useState(true);

    // Unread states
    const [unread, setUnread]                                   = useState<Map<string, number>>(new Map());
    const [dmOrder, setDmOrder]                                 = useState<string[]>([]);
    const [dmUnread, setDmUnread]                               = useState<Map<string, number>>(new Map());

    const myName       = getAnonymousName();
    const myEmail      = localStorage.getItem("email") ?? "";
    const currentUserId = localStorage.getItem("userId") ?? "";

    const joinedRoomRef = useRef<string | null>(null);
    const convIdToUserIdRef = useRef<Map<string, string>>(new Map());

    // ── Seed DM list from API on mount (Bug 1 fix) ─────────────────────────────
    // This ensures the list is ordered by LastMessageAt desc even after a fresh
    // login/refresh, before any SignalR events arrive.
    useEffect(() => {
        if (!currentUserId) return;
        getConversations(currentUserId)
            .then(convs => {
                // API already returns them in LastMessageAt desc order
                const orderedIds: string[] = [];
                const lastMsgMap = new Map<string, { text: string; sentAt: string }>();
                const unreadMap = new Map<string, number>();

                for (const conv of convs) {
                    const otherId = conv.otherUserId;
                    if (!otherId) continue;

                    if (conv.id) {
                        convIdToUserIdRef.current.set(conv.id, otherId);
                    }

                    orderedIds.push(otherId);

                    if (conv.lastMessage?.content) {
                        const text = conv.lastMessage.content.length > 40
                            ? conv.lastMessage.content.substring(0, 40) + "..."
                            : conv.lastMessage.content;
                        lastMsgMap.set(otherId, {
                            text,
                            sentAt: conv.lastMessage.sentAt ?? ""
                        });
                    }

                    if (conv.unreadCount > 0) {
                        unreadMap.set(otherId, conv.unreadCount);
                    }
                }

                setDmOrder(orderedIds);
                setDmUnread(unreadMap);
            })
            .catch(() => {/* non-critical – degrade gracefully */});
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentUserId]);

    useEffect(() => {
        getUsers()
            .then(data => setUsers(data)) // Fixed: Include current user so self-DMs work
            .catch(console.error);
    }, [currentUserId]);

    useEffect(() => {
        loadRooms();
    }, []);

    const loadRooms = async () => {
        setRoomsLoading(true);
        try {
            // Pass userId so the backend joins ChatRoomReadState and returns
            // the real persisted unread count for each room (fixes badge on login).
            const data = await getRooms(currentUserId || undefined);
            const sorted = data.sort((a: any, b: any) => {
                const aTime = a.lastMessageAt ? new Date(a.lastMessageAt).getTime() : 0;
                const bTime = b.lastMessageAt ? new Date(b.lastMessageAt).getTime() : 0;
                if (aTime !== bTime) return bTime - aTime;
                return a.name.localeCompare(b.name);
            });
            setRooms(sorted);

            const lastMsgMap = new Map<string, { text: string; sentAt: string }>();
            const unreadMap = new Map<string, number>();

            sorted.forEach((r: any) => {
                if (r.lastMessagePreview) {
                    lastMsgMap.set(r.name, { text: r.lastMessagePreview, sentAt: r.lastMessageAt });
                }
                // Read the real persisted unread count from the DB (via ChatRoomReadState)
                if (r.unreadCount > 0) {
                    unreadMap.set(r.name, r.unreadCount);
                }
            });

            setUnread(unreadMap);
        } catch (err) {
            console.error("Failed to load rooms:", err);
        } finally {
            setRoomsLoading(false);
        }
    };

    useEffect(() => {
        joinedRoomRef.current = selectedRoom;
    }, [selectedRoom]);

    // ── Auto-clear room unread when selected room changes ───────────────────────
    // Handles direct URL navigation and page mount – not just sidebar clicks.
    useEffect(() => {
        if (!selectedRoom || !currentUserId) return;
        setUnread(prev => {
            if ((prev.get(selectedRoom) ?? 0) === 0) return prev;
            const next = new Map(prev);
            next.set(selectedRoom, 0);
            return next;
        });
        markRoomAsRead(selectedRoom, currentUserId).catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedRoom]);

    // ── Auto-clear DM unread when URL is /dm/:userId ────────────────────────────
    // Handles page refresh, browser back, direct navigation.
    useEffect(() => {
        const match = location.pathname.match(/^\/dm\/([-\w]+)$/);
        if (!match || !currentUserId) return;
        const activeOtherUserId = match[1];
        setDmUnread(prev => {
            if ((prev.get(activeOtherUserId) ?? 0) === 0) return prev;
            const next = new Map(prev);
            next.set(activeOtherUserId, 0);
            return next;
        });
        markConversationAsRead(activeOtherUserId, currentUserId).catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [location.pathname]);

    // ── Rooms SignalR Integration ──
    useEffect(() => {
        const handleReceiveMessage = () => {
            const roomName = joinedRoomRef.current;
            if (!roomName) return;

            setRooms(prev => {
                const idx = prev.findIndex(r => r.name === roomName);
                if (idx < 0) return prev;
                const next = [...prev];
                const [moved] = next.splice(idx, 1);
                next.unshift(moved);
                return next;
            });

        };

        const handleGlobalNotification = (data: { roomName: string, message: string, createdAt: string }) => {
            const roomName = data.roomName;
            if (!roomName) return;

            // Move room to top of list (ordering)
            setRooms(prev => {
                const idx = prev.findIndex(r => r.name === roomName);
                if (idx <= 0) return prev;
                const next = [...prev];
                const [moved] = next.splice(idx, 1);
                next.unshift(moved);
                return next;
            });

            // Do NOT increment client-side unread here.
            // The backend already incremented ChatRoomReadState.UnreadCount for every
            // non-sender member. The RoomUpdated event delivers the authoritative count.
        };

        const handleRoomUpdated = (data: any) => {
            const roomName: string = data.roomName;
            const unreadCount: number = data.unreadCount ?? -1;

            if (unreadCount !== -1) {
                setUnread(prev => {
                    const next = new Map(prev);
                    next.set(roomName, unreadCount);
                    return next;
                });
            }
        };

        chatHubConnection.on("ReceiveMessage", handleReceiveMessage);
        chatHubConnection.on("GlobalNotification", handleGlobalNotification);
        chatHubConnection.on("RoomUpdated", handleRoomUpdated);
        return () => {
            chatHubConnection.off("ReceiveMessage", handleReceiveMessage);
            chatHubConnection.off("GlobalNotification", handleGlobalNotification);
            chatHubConnection.off("RoomUpdated", handleRoomUpdated);
        };
    }, [currentUserId]);

    // ── Private Chat SignalR Integration ──
    useEffect(() => {
        const pConn = getPrivateChatConnection();

        // ReceivePrivateMessage: update last-message preview only.
        // Unread count is NOT incremented here — the server sends the authoritative
        // count via ConversationUpdated which fires right after this event.
        // Incrementing here AND trusting the server value causes a double-count bug.
        const handlePrivateMessage = () => {
            // Handled by ConversationUpdated
        };

        // ConversationUpdated: the single source of truth for reordering (Bug 2 & 3 fix).
        // Backend sends this to BOTH sender and receiver after every message.
        // unreadCount === -1 means "don't update unread" (sent by us, not received).
        const handleConversationUpdated = (data: any) => {
            const convId: string = data.conversationId;
            const unreadCount: number = data.unreadCount ?? -1;

            // We need to map conversationId → otherUserId.
            // We do this by checking the dmLastMessage map (already keyed by userId)
            // OR by inspecting the users list. The cleanest way is to keep a ref
            // of conversationId → otherUserId. We build it from the dmOrder + dmLastMessage,
            // but the simplest approach here is to find the user from the pathname or a
            // stored map. We'll use a conversation-id-to-userId map maintained in a ref.
            const otherUserId = convIdToUserIdRef.current.get(convId);
            if (!otherUserId) return;

            // Move conversation to top of list (Bug 2 fix: reorder only on real event)
            setDmOrder(prev => {
                const next = prev.filter(id => id !== otherUserId);
                next.unshift(otherUserId);
                return next;
            });

            // Only set unread count if unreadCount !== -1 (Bug 3 fix)
            if (unreadCount !== -1) {
                setDmUnread(prev => {
                    const next = new Map(prev);
                    next.set(otherUserId, unreadCount);
                    return next;
                });
            }
        };

        pConn.on("ReceivePrivateMessage", handlePrivateMessage);
        pConn.on("ConversationUpdated", handleConversationUpdated);

        if (pConn.state === "Disconnected") {
            pConn.start().catch(() => {});
        }

        return () => {
            pConn.off("ReceivePrivateMessage", handlePrivateMessage);
            pConn.off("ConversationUpdated", handleConversationUpdated);
        };
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentUserId]);


    const handleSelectRoom = (roomName: string) => {
        setUnread(prev => {
            const next = new Map(prev);
            next.set(roomName, 0); // instantly clear on click
            return next;
        });
        setSelectedRoom(roomName);
        markRoomAsRead(roomName, currentUserId).catch(console.error);
        markAllAsRead(currentUserId).catch(console.error);
    };

    const handleSelectDM = (userId: string) => {
        setDmUnread(prev => {
            const next = new Map(prev);
            next.set(userId, 0); // instantly clear on click
            return next;
        });
        navigate(`/dm/${userId}`);
        markConversationAsRead(userId, currentUserId).catch(console.error);
        markAllAsRead(currentUserId).catch(console.error);
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
                    <div className="text-sm font-bold text-white leading-none">Zap<span style={{ color: "#38BDF8" }}>Chat</span></div>
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
                                                {!active && roomUnread > 0 && (
                                                    <span
                                                        className="text-[11px] truncate block mt-0.5 leading-tight font-medium"
                                                        style={{ color: "#06b6d4" }}
                                                    >
                                                        New message
                                                    </span>
                                                )}
                                            </div>

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
                                                {!isDmActive && uCount > 0 && (
                                                    <span
                                                        className="text-[11px] truncate block mt-0.5 leading-tight font-medium"
                                                        style={{ color: "#06b6d4" }}
                                                    >
                                                        New message
                                                    </span>
                                                )}
                                            </div>
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