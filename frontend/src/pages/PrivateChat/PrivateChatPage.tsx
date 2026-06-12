import { useEffect, useRef, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, Send, X } from "lucide-react";
import { getUserById } from "../../api/authApi";
import { createConversation, getConversation, deletePrivateMessage } from "../../api/privateChatApi";
import { getPrivateChatConnection } from "../../hubs/privateChatHub";
import type { User } from "../../types/User";
import type { PrivateMessage, PrivateMessageReaction } from "../../types/PrivateMessage";
import type { HubConnection } from "@microsoft/signalr";
import PrivateMessageBubble from "../../components/PrivateMessageBubble";
import TopNav from "../../components/TopNav";

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

    const [selectedOtherUserId] = useState<string | undefined>(receiverUserId);
    const [receiver, setReceiver] = useState<User | null>(null);
    const [messages, setMessages] = useState<PrivateMessage[]>([]);
    const [messageInput, setMessageInput] = useState("");
    const [sending, setSending] = useState(false);
    const [ready, setReady] = useState(false);
    const [replyingTo, setReplyingTo] = useState<PrivateMessage | null>(null);

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

    // Load receiver's anonymous name
    useEffect(() => {
        if (!selectedOtherUserId) return;
        getUserById(selectedOtherUserId).then(setReceiver).catch(console.error);
    }, [selectedOtherUserId]);

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

                const handleMessageDeleted = (data: { messageId: string; deletedAt: string }) => {
                    if (!isMounted) return;
                    setMessages(prev => prev.map(m =>
                        m.id === data.messageId ? { ...m, isDeleted: true, deletedAt: data.deletedAt } : m
                    ));
                };

                conn.off("ReceivePrivateMessage", handleReceive);
                conn.on("ReceivePrivateMessage", handleReceive);

                conn.off("ReactionAdded", handleReactionAdded);
                conn.on("ReactionAdded", handleReactionAdded);

                conn.off("MessageDeleted", handleMessageDeleted);
                conn.on("MessageDeleted", handleMessageDeleted);

                (connectionRef.current as any)._messageDeletedHandler = handleMessageDeleted;

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

    return (
        <div className="h-screen flex flex-col overflow-hidden" style={{ background: "#F8FAFC" }}>
            <TopNav />
            {/* Main Chat Area */}
            {selectedOtherUserId ? (
                <div className="flex-1 flex flex-col overflow-hidden">
                    {/* Header */}
                    <div
                        className="px-5 py-3 flex items-center gap-3 shrink-0"
                        style={{ background: "#FFFFFF", borderBottom: "1px solid #E2E8F0" }}
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

                        <div>
                            <div className="font-semibold text-slate-900 text-sm">
                                {receiver?.anonymousName ?? "Loading…"}
                            </div>
                            <div className="text-xs text-emerald-600">Private · Encrypted</div>
                        </div>
                    </div>

                    {/* Messages */}
                    <div className="flex-1 overflow-y-auto px-5 py-4 space-y-2" style={{ background: "#F8FAFC" }}>
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
                                onReact={(emoji) => handleReaction(m.id!, emoji)}
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
                            />
                        ))}

                        <div ref={messagesEndRef} />
                    </div>

                    {/* Reply preview */}
                    {replyingTo && (
                        <div
                            className="mx-4 mb-1 px-3 py-2 rounded flex items-center justify-between"
                            style={{ background: "#EFF6FF", borderLeft: "2px solid #38BDF8" }}
                        >
                            <div className="min-w-0">
                                <div className="text-xs text-sky-600 font-medium">
                                    Replying to {replyingTo.senderName}
                                </div>
                                <div className="text-xs text-slate-500 truncate">
                                    {replyingTo.content}
                                </div>
                            </div>
                            <button
                                onClick={() => setReplyingTo(null)}
                                className="text-slate-400 hover:text-slate-700 ml-2 shrink-0">
                                <X size={14} />
                            </button>
                        </div>
                    )}

                    {/* Input */}
                    <div className="border-t border-slate-200 px-4 py-3 bg-white shrink-0">
                        <div className="flex items-center gap-3">
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
                                className="
                            flex-1 bg-white border border-slate-200 rounded-xl
                            px-4 py-3 text-sm text-slate-900 outline-none
                            focus:border-sky-400 placeholder:text-slate-400
                            disabled:opacity-50 transition-colors"
                            />
                            <button
                                onClick={sendMessage}
                                disabled={!messageInput.trim() || sending || !ready}
                                className="
                            p-3 rounded-xl bg-sky-500 hover:bg-sky-600 text-white
                            disabled:opacity-40 disabled:cursor-not-allowed
                            transition-colors shrink-0">
                                <Send size={17} />
                            </button>
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