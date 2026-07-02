import { useEffect, useRef, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, Send, X, ShieldAlert, ShieldCheck } from "lucide-react";
import { getUserById } from "../../api/authApi";
import { createConversation, getConversation, deletePrivateMessage, blockUser, unblockUser, getBlockedUsers } from "../../api/privateChatApi";
import { getPrivateChatConnection } from "../../hubs/privateChatHub";
import type { User } from "../../types/User";
import type { PrivateMessage, PrivateMessageReaction } from "../../types/PrivateMessage";
import type { HubConnection } from "@microsoft/signalr";
import PrivateMessageBubble from "../../components/PrivateMessageBubble";
import TopNav from "../../components/TopNav";
import { useTheme } from "../../context/ThemeContext";

interface ServerMessage {
    id: string;
    conversationId: string;
    senderId: string;
    senderName: string;
    content: string;
    sentAt: string;
    isRead: boolean;
    parentMessageId?: string;
    attachmentUrl?: string;
    fileName?: string;
    reactions?: PrivateMessageReaction[];
    isDeleted?: boolean;
    deletedAt?: string;
}

export default function PrivateChatPage() {
    const { userId: receiverUserId } = useParams<{ userId: string }>();
    const navigate = useNavigate();
    const { isDark } = useTheme();

    const [selectedOtherUserId] = useState<string | undefined>(receiverUserId);
    const [receiver, setReceiver] = useState<User | null>(null);
    const [messages, setMessages] = useState<PrivateMessage[]>([]);
    const [messageInput, setMessageInput] = useState("");
    const [sending, setSending] = useState(false);
    const [ready, setReady] = useState(false);
    const [replyingTo, setReplyingTo] = useState<PrivateMessage | null>(null);
    const [blockedMessage, setBlockedMessage] = useState<{ category: string; reason: string } | null>(null);
    const [isBlocked, setIsBlocked] = useState(false);

    const toggleBlock = async () => {
        if (!selectedOtherUserId || !currentUserId) return;
        try {
            if (isBlocked) {
                await unblockUser(currentUserId, selectedOtherUserId);
                setIsBlocked(false);
            } else {
                await blockUser(currentUserId, selectedOtherUserId);
                setIsBlocked(true);
            }
        } catch (err) {
            console.error("Failed to toggle block", err);
        }
    };

    // Refs — always hold latest values without triggering re-renders
    const conversationIdRef = useRef("");
    const connectionRef = useRef<HubConnection | null>(null);
    const messagesEndRef = useRef<HTMLDivElement | null>(null);
    const inputRef = useRef<HTMLInputElement>(null);

    const currentUserId = localStorage.getItem("userId") ?? "";

    // Auto-scroll to bottom
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);

    // Load receiver's anonymous name and block status
    useEffect(() => {
        if (!selectedOtherUserId) return;
        getUserById(selectedOtherUserId).then(setReceiver).catch(console.error);

        getBlockedUsers(currentUserId)
            .then(blockedIds => {
                setIsBlocked(blockedIds.includes(selectedOtherUserId));
            })
            .catch(console.error);
    }, [selectedOtherUserId, currentUserId]);

    // Main init: conversation + history + SignalR
    useEffect(() => {
        if (!selectedOtherUserId || !currentUserId) return;
        let isMounted = true;

        const init = async () => {
            try {
                // Get or create conversation (normalized server-side)
                const conv = await createConversation(currentUserId, selectedOtherUserId);
                if (!isMounted) return;

                conversationIdRef.current = conv.id;

                // Load history
                const history: ServerMessage[] = await getConversation(conv.id);
                if (isMounted) {
                    setMessages(history.map(mapServerMessage));
                }

                // Connect SignalR
                const conn = getPrivateChatConnection();
                connectionRef.current = conn;

                // Always re-register using a named handler to avoid duplicate listeners or killing other listeners
                const handleReceive = (msg: ServerMessage) => {
                    if (!isMounted) return;
                    // Guard: only show messages for THIS conversation
                    if (msg.conversationId !== conv.id) return;

                    setMessages(prev => {
                        // Deduplicate by id in case of reconnect replays
                        if (prev.some(m => m.id === msg.id)) return prev;
                        return [...prev, mapServerMessage(msg)];
                    });
                };

                const handleReactionAdded = (data: {
                    messageId: string;
                    senderName: string;
                    reaction: string;
                }) => {
                    if (!isMounted) return;
                    setMessages(prev => prev.map(m => {
                        if (m.id !== data.messageId) return m;
                        const existing = m.reactions ?? [];
                        const filtered = existing.filter(
                            r => !(r.senderName === data.senderName && r.reaction === data.reaction)
                        );
                        const toggled = filtered.length === existing.length
                            ? [...existing, { senderName: data.senderName, reaction: data.reaction }]
                            : filtered;
                        return { ...m, reactions: toggled };
                    }));
                };

                const handleMessageDeleted = (data: { messageId: string; deletedAt: string; deletedBy: string }) => {
                    setMessages(prev => prev.map(m =>
                        m.id === data.messageId
                            ? {
                                ...m,
                                isDeleted: data.deletedBy === "User",
                                deletedBy: data.deletedBy,
                                deletedAt: data.deletedAt,
                                content: ""
                            }
                            : m
                    ));
                };

                const handleBlocked = (data: { category: string; reason: string }) => {
                    setBlockedMessage(data);
                    setTimeout(() => setBlockedMessage(null), 5000);
                };

                const handleMessageEdited = (data: { messageId: string; content: string; editedAt: string; isEdited: boolean }) => {
                    setMessages(prev => prev.map(m =>
                        m.id === data.messageId
                            ? { ...m, content: data.content, isEdited: data.isEdited, editedAt: data.editedAt }
                            : m
                    ));
                };

                const handleMessageRead = (data: { messageId: string; readAt: string }) => {
                    setMessages(prev => prev.map(m =>
                        m.id === data.messageId
                            ? { ...m, isRead: true }
                            : m
                    ));
                };

                conn.off("ReceivePrivateMessage", handleReceive);
                conn.on("ReceivePrivateMessage", handleReceive);

                conn.off("ReactionAdded", handleReactionAdded);
                conn.on("ReactionAdded", handleReactionAdded);

                conn.off("MessageDeleted", handleMessageDeleted);
                conn.on("MessageDeleted", handleMessageDeleted);

                conn.off("PrivateMessageBlocked", handleBlocked);
                conn.on("PrivateMessageBlocked", handleBlocked);

                conn.off("MessageEdited", handleMessageEdited);
                conn.on("MessageEdited", handleMessageEdited);

                conn.off("MessageRead", handleMessageRead);
                conn.on("MessageRead", handleMessageRead);

                (connectionRef.current as any)._messageDeletedHandler = handleMessageDeleted;
                (connectionRef.current as any)._blockedHandler = handleBlocked;
                (connectionRef.current as any)._messageEditedHandler = handleMessageEdited;
                (connectionRef.current as any)._messageReadHandler = handleMessageRead;

                if (conn.state === "Disconnected") {
                    await conn.start();
                }

                // Attach to ref for cleanup
                (connectionRef.current as any)._receiveHandler = handleReceive;
                (connectionRef.current as any)._reactionHandler = handleReactionAdded;

                if (isMounted) setReady(true);
            } catch (err) {
                console.error("[PrivateChat] init error:", err);
            }
        };

        init();

        return () => {
            isMounted = false;
            const conn = connectionRef.current as any;
            if (conn) {
                if (conn._receiveHandler) conn.off("ReceivePrivateMessage", conn._receiveHandler);
                if (conn._reactionHandler) conn.off("ReactionAdded", conn._reactionHandler);
                if (conn._messageDeletedHandler) conn.off("MessageDeleted", conn._messageDeletedHandler);
                if (conn._blockedHandler) conn.off("PrivateMessageBlocked", conn._blockedHandler);
                if (conn._messageEditedHandler) conn.off("MessageEdited", conn._messageEditedHandler);
                if (conn._messageReadHandler) conn.off("MessageRead", conn._messageReadHandler);
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedOtherUserId, currentUserId]);

    const mapServerMessage = (m: ServerMessage): PrivateMessage => ({
        id: m.id,
        conversationId: m.conversationId,
        senderId: m.senderId,
        senderName: m.senderName,
        content: m.content,
        sentAt: m.sentAt,
        isRead: m.isRead,
        parentMessageId: m.parentMessageId,
        attachmentUrl: m.attachmentUrl,
        fileName: m.fileName,
        reactions: m.reactions,
        isDeleted: m.isDeleted,
        deletedAt: m.deletedAt
    });

    const sendMessage = useCallback(async () => {
        const text = messageInput.trim();
        if (!text || !ready) return;

        const convId = conversationIdRef.current;
        if (!convId) {
            console.error("[PrivateChat] conversationId not ready");
            return;
        }

        const conn = connectionRef.current;
        if (!conn) return;

        setSending(true);
        setMessageInput("");

        try {
            if (conn.state === "Disconnected") {
                await conn.start();
            }

            // Hub signature: SendPrivateMessage(conversationId, receiverId, message, parentMessageId)
            // senderId is read server-side from JWT — never passed from client
            await conn.invoke(
                "SendPrivateMessage",
                convId,
                receiverUserId,
                text,
                replyingTo?.id ?? null
            );
            setReplyingTo(null);
            // No optimistic message — server pushes back via ReceivePrivateMessage
        } catch (err) {
            console.error("[PrivateChat] send error:", err);
            setMessageInput(text); // restore on failure
        } finally {
            setSending(false);
        }
    }, [messageInput, ready, receiverUserId, replyingTo]);

    const handleReaction = useCallback(async (messageId: string, emoji: string) => {
        const conn = connectionRef.current;
        if (!conn || conn.state !== "Connected") return;
        try {
            await conn.invoke("AddReaction", messageId, emoji);
        } catch (err) {
            console.error("[PrivateChatHub] AddReaction error:", err);
        }
    }, []);

    const handleEdit = useCallback(async (messageId: string, newContent: string) => {
        const conn = connectionRef.current;
        if (!conn || conn.state !== "Connected") return;
        try {
            await conn.invoke("EditPrivateMessage", messageId, newContent);
        } catch (err) {
            console.error("[PrivateChatHub] EditPrivateMessage error:", err);
        }
    }, []);

    const headerBg     = isDark ? "#0f172a" : "#ffffff";
    const headerBorder = isDark ? "rgba(255,255,255,0.07)" : "#e2e8f0";
    const headerText   = isDark ? "#f1f5f9" : "#0f172a";
    const headerSub    = isDark ? "#64748b" : "#475569";
    const chatBg       = isDark ? "#0c1220" : "#f0f9ff";
    const inputBg      = isDark ? "#0f172a" : "#ffffff";
    const inputBorder  = isDark ? "rgba(255,255,255,0.1)" : "#e2e8f0";
    const inputText    = isDark ? "#f1f5f9" : "#0f172a";
    const replyBg      = isDark ? "rgba(14,165,233,0.1)" : "#eff6ff";

    return (
        <div className="h-screen flex flex-col overflow-hidden" style={{ background: chatBg }}>
            <TopNav />
            {/* Main Chat Area */}
            {selectedOtherUserId ? (
                <div className="flex-1 flex flex-col overflow-hidden">
                    {/* Header */}
                    <div
                        className="px-4 sm:px-5 py-3 flex items-center gap-3 shrink-0"
                        style={{ background: headerBg, borderBottom: `1px solid ${headerBorder}` }}
                    >
                        <button
                            onClick={() => navigate("/dashboard")}
                            className="text-slate-500 hover:text-slate-900 transition-colors p-1 rounded">
                            <ArrowLeft size={18} />
                        </button>

                        <div
                            className="w-9 h-9 rounded-full flex items-center justify-center font-bold text-sm text-white shrink-0"
                            style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                        >
                            {receiver?.anonymousName?.charAt(0).toUpperCase() ?? "?"}
                        </div>

                        <div className="flex-1 min-w-0">
                            <div className="font-semibold text-sm" style={{ color: headerText }}>
                                {receiver?.anonymousName ?? "Loading…"}
                            </div>
                            <div className="text-xs flex items-center gap-2" style={{ color: "#059669" }}>
                                Private · Encrypted {isBlocked && <span className="text-rose-500 font-bold ml-2">BLOCKED</span>}
                            </div>
                        </div>

                        <button
                            onClick={toggleBlock}
                            className={`p-1.5 rounded-lg transition-colors shrink-0 flex items-center gap-1.5 text-xs font-semibold ${isBlocked ? "bg-rose-50 text-rose-600 hover:bg-rose-100" : "bg-slate-100 text-slate-600 hover:bg-slate-200"}`}
                            title={isBlocked ? "Unblock user" : "Block user"}
                        >
                            {isBlocked ? <ShieldCheck size={14} /> : <ShieldAlert size={14} />}
                            <span className="hidden sm:inline">{isBlocked ? "Unblock" : "Block"}</span>
                        </button>
                    </div>

                    {/* Moderation Warning Toast */}
                    {blockedMessage && (
                        <div
                            className="absolute top-[80px] left-1/2 -translate-x-1/2 flex items-start gap-3 
                                       px-4 py-3 rounded-lg shadow-lg z-50 text-sm w-[90%] max-w-[400px]
                                       animate-in fade-in slide-in-from-top-4"
                            style={{
                                background: "#FFFBEB", // Amber-50
                                border: "1px solid #FCD34D", // Amber-300
                                color: "#92400E" // Amber-900
                            }}
                        >
                            <span className="text-lg">🚫</span>
                            <div className="flex-1">
                                <div className="font-semibold mb-0.5">Message Blocked</div>
                                <div className="opacity-90">{blockedMessage.reason}</div>
                            </div>
                            <button
                                onClick={() => setBlockedMessage(null)}
                                className="opacity-60 hover:opacity-100 transition-opacity p-1"
                            >
                                <X size={16} />
                            </button>
                        </div>
                    )}

                    {/* Messages */}
                    <div className="flex-1 overflow-y-auto px-4 sm:px-5 py-4 space-y-2" style={{ background: chatBg }}>
                        {messages.length === 0 && ready && (
                            <div className="flex flex-col items-center justify-center h-full text-slate-400 gap-2">
                                <span className="text-4xl">🔒</span>
                                <p className="text-sm">Start your private conversation</p>
                                <p className="text-xs">Messages are anonymous</p>
                            </div>
                        )}

                        {messages.map((m, index) => (
                            <PrivateMessageBubble
                                key={m.id ?? index}
                                message={m}
                                onReply={() => {
                                    setReplyingTo(m);
                                    inputRef.current?.focus();
                                }}
                                onReact={(emoji: string) => handleReaction(m.id!, emoji)}
                                onDelete={m.id ? async () => {
                                    try {
                                        await deletePrivateMessage(m.id!);
                                        setMessages(prev => prev.map(msg =>
                                            msg.id === m.id
                                                ? { ...msg, isDeleted: true, deletedAt: new Date().toISOString() }
                                                : msg
                                        ));
                                    } catch (err) {
                                        console.error("[PrivateChat] DeleteMessage error:", err);
                                    }
                                } : undefined}
                                onEdit={m.id ? (newContent: string) => handleEdit(m.id!, newContent) : undefined}
                            />
                        ))}

                        <div ref={messagesEndRef} />
                        {/* Reply preview */}
                        {replyingTo && (
                            <div
                                className="mx-3 sm:mx-4 mb-1 px-3 py-2 rounded flex items-center justify-between"
                                style={{ background: replyBg, borderLeft: "2px solid #38BDF8" }}
                            >
                                <div className="min-w-0">
                                    <div className="text-xs font-medium" style={{ color: "#0284c7" }}>
                                        Replying to {replyingTo.senderName}
                                    </div>
                                    <div className="text-xs truncate" style={{ color: headerSub }}>
                                        {replyingTo.content}
                                    </div>
                                </div>
                                <button
                                    onClick={() => setReplyingTo(null)}
                                    className="ml-2 shrink-0 transition-opacity"
                                    style={{ color: headerSub }}
                                >
                                    <X size={14} />
                                </button>
                            </div>
                        )}
                        {/* Input */}
                        <div className="shrink-0 px-3 sm:px-4 py-3" style={{ background: inputBg, borderTop: `1px solid ${inputBorder}` }}>
                            <div className="flex items-center gap-2 sm:gap-3">
                                <input
                                    ref={inputRef}
                                    value={messageInput}
                                    onChange={e => setMessageInput(e.target.value)}
                                    onKeyDown={e => {
                                        if (e.key === "Enter" && !e.shiftKey) {
                                            e.preventDefault();
                                            sendMessage();
                                        }
                                        if (e.key === "Escape") setReplyingTo(null);
                                    }}
                                    placeholder={replyingTo ? `Reply to ${replyingTo.senderName}…` : `Message ${receiver?.anonymousName ?? "…"}`}
                                    disabled={!ready}
                                    className="flex-1 rounded-xl px-3 sm:px-4 py-2.5 sm:py-3 text-sm outline-none transition-colors disabled:opacity-50"
                                    style={{
                                        background: isDark ? "rgba(255,255,255,0.06)" : "#f8fafc",
                                        border: `1px solid ${inputBorder}`,
                                        color: inputText,
                                    }}
                                />
                                <button
                                    onClick={sendMessage}
                                    disabled={!messageInput.trim() || sending || isBlocked}
                                    className="p-3 rounded-xl bg-sky-500 hover:bg-sky-600 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-colors shrink-0 shadow-sm"
                                >
                                    <Send size={18} />
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            ) : (
                <div className="flex-1 flex items-center justify-center text-slate-500">
                    <div className="text-center">
                        <p className="text-lg font-medium">No conversation selected</p>
                        <p className="text-sm">Navigate to a user to start a private chat</p>
                    </div>
                </div>
            )}
        </div>
    );
}