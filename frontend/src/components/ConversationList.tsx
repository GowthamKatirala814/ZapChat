import { useEffect, useRef, useState } from "react";
import { Search, MessageSquare, Plus, X, Loader2 } from "lucide-react";
import { getConversations, markAsRead, createConversation } from "../api/privateChatApi";
import type { Conversation } from "../api/privateChatApi";
import type { User } from "../types/User";
import { getPrivateChatConnection } from "../hubs/privateChatHub";

interface ConversationListProps {
    currentUserId: string;
    onSelectConversation: (conversationId: string, otherUserId: string) => void;
    activeConversationId?: string;
}

export default function ConversationList({ currentUserId, onSelectConversation, activeConversationId }: ConversationListProps) {
    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState("");
    const [searchResults, setSearchResults] = useState<User[]>([]);
    const [searching, setSearching] = useState(false);
    const [showSearch, setShowSearch] = useState(false);
    const signalRRegistered = useRef(false);

    const loadConversations = async () => {
        setLoading(true);
        try {
            const data = await getConversations(currentUserId);
            setConversations(data);
        } catch (err) {
            console.error("[ConversationList] load error:", err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { loadConversations(); }, [currentUserId]);

    // ── SignalR: reorder conversation list in-place when a message is sent/received ──
    useEffect(() => {
        if (!currentUserId || signalRRegistered.current) return;

        const conn = getPrivateChatConnection();

        const handleConversationUpdated = (data: {
            conversationId: string;
            lastMessageAt: string;
            lastMessageContent: string;
            lastMessageSenderName: string;
            unreadCount: number;
        }) => {
            setConversations(prev => {
                const idx = prev.findIndex(c => c.id === data.conversationId);
                
                if (idx === -1) {
                    // Unknown conversation! It's a brand new chat.
                    // Instead of complex async state logic, just queue a full reload.
                    setTimeout(() => loadConversations(), 100);
                    return prev;
                }

                // Pull the matched conversation, update its timestamp and preview
                const updated = {
                    ...prev[idx],
                    lastMessageAt: data.lastMessageAt,
                    lastMessage: prev[idx].lastMessage
                        ? {
                              ...prev[idx].lastMessage!,
                              content: data.lastMessageContent,
                              sentAt: data.lastMessageAt,
                              senderName: data.lastMessageSenderName
                          }
                        : {
                              id: "",
                              content: data.lastMessageContent,
                              sentAt: data.lastMessageAt,
                              senderId: "",
                              senderName: data.lastMessageSenderName,
                              isRead: false
                          },
                    // Use the exact unread count from the DB (if -1, it means we are the sender so don't update it)
                    unreadCount: data.unreadCount === -1 
                        ? prev[idx].unreadCount 
                        : (activeConversationId === data.conversationId ? 0 : data.unreadCount)
                };

                // Remove from current position and prepend to top
                const rest = prev.filter(c => c.id !== data.conversationId);
                return [updated, ...rest];
            });
        };

        conn.off("ConversationUpdated", handleConversationUpdated);
        conn.on("ConversationUpdated", handleConversationUpdated);
        signalRRegistered.current = true;

        return () => {
            conn.off("ConversationUpdated", handleConversationUpdated);
            signalRRegistered.current = false;
        };
    }, [currentUserId, activeConversationId]);

    const handleSearch = async (q: string) => {
        setSearchQuery(q);
        if (!q.trim()) {
            setSearchResults([]);
            return;
        }
        setSearching(true);
        try {
            const allUsers = await fetch("https://localhost:5000/api/auth/users").then(r => r.json());
            const filtered = allUsers.filter((u: User) =>
                u.anonymousName.toLowerCase().includes(q.toLowerCase()) &&
                u.id !== currentUserId
            );
            setSearchResults(filtered);
        } catch (err) {
            console.error("[ConversationList] search error:", err);
        } finally {
            setSearching(false);
        }
    };

    const startConversation = async (otherUserId: string) => {
        try {
            const conv = await createConversation(currentUserId, otherUserId);
            setShowSearch(false);
            setSearchQuery("");
            setSearchResults([]);
            loadConversations();
            onSelectConversation(conv.id, otherUserId);
        } catch (err) {
            console.error("[ConversationList] start conversation error:", err);
        }
    };

    const handleSelect = async (conv: Conversation) => {
        onSelectConversation(conv.id, conv.otherUserId);
        // Mark last message as read if exists
        if (conv.lastMessage && !conv.lastMessage.isRead) {
            try {
                await markAsRead(conv.lastMessage.id);
                loadConversations(); // Refresh to update unread count
            } catch (err) {
                console.error("[ConversationList] mark as read error:", err);
            }
        }
    };

    const formatTime = (dateStr: string | null) => {
        if (!dateStr) return "";
        const date = new Date(dateStr);
        const now = new Date();
        const diff = now.getTime() - date.getTime();
        const hours = Math.floor(diff / (1000 * 60 * 60));
        if (hours < 1) return "now";
        if (hours < 24) return `${hours}h`;
        const days = Math.floor(hours / 24);
        if (days < 7) return `${days}d`;
        return date.toLocaleDateString();
    };

    return (
        <div className="w-80 border-r border-slate-800 flex flex-col bg-slate-900 h-full">
            {/* Header */}
            <div className="p-4 border-b border-slate-800">
                <div className="flex items-center justify-between mb-3">
                    <h2 className="font-semibold text-white">Messages</h2>
                    <button
                        onClick={() => setShowSearch(true)}
                        className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                    >
                        <Plus size={18} />
                    </button>
                </div>
                {!showSearch && (
                    <div className="relative">
                        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
                        <input
                            value={searchQuery}
                            onChange={e => handleSearch(e.target.value)}
                            placeholder="Search conversations…"
                            className="w-full pl-9 pr-4 py-2 rounded-lg bg-slate-800 border border-slate-700 text-sm text-white outline-none focus:border-blue-500 placeholder:text-slate-500"
                        />
                    </div>
                )}
            </div>

            {/* Search Modal */}
            {showSearch && (
                <div className="p-4 border-b border-slate-800">
                    <div className="flex items-center gap-2 mb-3">
                        <div className="flex-1 relative">
                            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
                            <input
                                value={searchQuery}
                                onChange={e => handleSearch(e.target.value)}
                                placeholder="Search users…"
                                autoFocus
                                className="w-full pl-9 pr-4 py-2 rounded-lg bg-slate-800 border border-slate-700 text-sm text-white outline-none focus:border-blue-500 placeholder:text-slate-500"
                            />
                        </div>
                        <button
                            onClick={() => {
                                setShowSearch(false);
                                setSearchQuery("");
                                setSearchResults([]);
                            }}
                            className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                        >
                            <X size={18} />
                        </button>
                    </div>
                    {searching && (
                        <div className="flex items-center justify-center py-4">
                            <Loader2 size={20} className="animate-spin text-slate-500" />
                        </div>
                    )}
                    {!searching && searchResults.length > 0 && (
                        <div className="space-y-1 max-h-48 overflow-y-auto">
                            {searchResults.map((user: User) => (
                                <button
                                    key={user.id}
                                    onClick={() => startConversation(user.id)}
                                    className="w-full flex items-center gap-3 p-2 rounded-lg hover:bg-slate-800 transition-colors text-left"
                                >
                                    <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-violet-600 flex items-center justify-center font-bold text-xs shrink-0">
                                        {user.anonymousName.charAt(0).toUpperCase()}
                                    </div>
                                    <span className="text-sm text-white">{user.anonymousName}</span>
                                </button>
                            ))}
                        </div>
                    )}
                    {!searching && searchQuery && searchResults.length === 0 && (
                        <p className="text-sm text-slate-500 py-2">No users found</p>
                    )}
                </div>
            )}

            {/* Conversation List */}
            <div className="flex-1 overflow-y-auto">
                {loading ? (
                    <div className="flex items-center justify-center py-8">
                        <Loader2 size={24} className="animate-spin text-slate-500" />
                    </div>
                ) : conversations.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-12 text-slate-500">
                        <MessageSquare size={32} className="mb-2" />
                        <p className="text-sm">No conversations yet</p>
                        <p className="text-xs mt-1">Start a new chat</p>
                    </div>
                ) : (
                    <div className="divide-y divide-slate-800/60">
                        {conversations.map((conv) => (
                            <button
                                key={conv.id}
                                onClick={() => handleSelect(conv)}
                                className={`w-full p-4 flex items-start gap-3 hover:bg-slate-800 transition-colors text-left ${
                                    activeConversationId === conv.id ? "bg-slate-800" : ""
                                }`}
                            >
                                <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-violet-600 flex items-center justify-center font-bold text-sm shrink-0">
                                    {conv.otherUserId.slice(0, 2).toUpperCase()}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center justify-between mb-1">
                                        <span className="font-medium text-white text-sm truncate">
                                            User {conv.otherUserId.slice(0, 8)}
                                        </span>
                                        {conv.lastMessageAt && (
                                            <span className="text-xs text-slate-500">
                                                {formatTime(conv.lastMessageAt)}
                                            </span>
                                        )}
                                    </div>
                                    <div className="flex items-center justify-between">
                                        <p className="text-xs text-slate-400 truncate">
                                            {conv.lastMessage?.content || "No messages yet"}
                                        </p>
                                        {conv.unreadCount > 0 && (
                                            <span className="ml-2 px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-600 text-white">
                                                {conv.unreadCount}
                                            </span>
                                        )}
                                    </div>
                                </div>
                            </button>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
