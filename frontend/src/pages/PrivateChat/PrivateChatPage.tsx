import { useEffect, useRef, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, Send, X } from "lucide-react";
import { getUserById } from "../../api/authApi";
import { createConversation, getConversation } from "../../api/privateChatApi";
import { getPrivateChatConnection } from "../../hubs/privateChatHub";
import type { User } from "../../types/User";
import type { PrivateMessage, PrivateMessageReaction } from "../../types/PrivateMessage";
import type { Message } from "../../types/Message";
import type { HubConnection } from "@microsoft/signalr";
import MessageBubble from "../../components/MessageBubble";

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
}

export default function PrivateChatPage() {
    const { userId: receiverUserId } = useParams<{ userId: string }>();
    const navigate = useNavigate();

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
        if (!receiverUserId) return;
        getUserById(receiverUserId).then(setReceiver).catch(console.error);
    }, [receiverUserId]);

    // Main init: conversation + history + SignalR
    useEffect(() => {
        if (!receiverUserId || !currentUserId) return;
        let isMounted = true;

        const init = async () => {
            try {
                // Get or create conversation (normalized server-side)
                const conv = await createConversation(currentUserId, receiverUserId);
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

                conn.off("ReceivePrivateMessage", handleReceive);
                conn.on("ReceivePrivateMessage", handleReceive);

                conn.off("ReactionAdded", handleReactionAdded);
                conn.on("ReactionAdded", handleReactionAdded);

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
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [receiverUserId, currentUserId]);

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
        reactions: m.reactions
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

    // Helper to map PrivateMessage to Message so we can reuse MessageBubble
    const toMessageProps = (m: PrivateMessage): Message => ({
        id: m.id,
        anonymousName: m.senderName,
        message: m.content,
        sentAt: m.sentAt,
        userId: m.senderId,
        parentMessageId: m.parentMessageId,
        reactions: m.reactions?.map(r => ({ anonymousName: r.senderName, reaction: r.reaction })),
        attachmentUrl: m.attachmentUrl,
        fileName: m.fileName
    });

    return (
        <div className="h-screen bg-slate-950 text-white flex flex-col overflow-hidden">

            {/* Header */}
            <div className="border-b border-slate-800 px-5 py-3 flex items-center gap-3 bg-slate-900 shrink-0">
                <button
                    onClick={() => navigate("/dashboard")}
                    className="text-slate-400 hover:text-white transition-colors p-1 rounded">
                    <ArrowLeft size={18} />
                </button>

                <div className="w-9 h-9 rounded-full bg-gradient-to-br from-blue-500 to-violet-600 flex items-center justify-center font-bold text-sm shrink-0">
                    {receiver?.anonymousName?.charAt(0).toUpperCase() ?? "?"}
                </div>

                <div>
                    <div className="font-semibold text-white text-sm">
                        {receiver?.anonymousName ?? "Loading…"}
                    </div>
                    <div className="text-xs text-green-500">Private · Encrypted</div>
                </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto px-5 py-4 space-y-2">
                {messages.length === 0 && ready && (
                    <div className="flex flex-col items-center justify-center h-full text-slate-600 gap-2">
                        <span className="text-4xl">🔒</span>
                        <p className="text-sm">Start your private conversation</p>
                        <p className="text-xs">Messages are anonymous</p>
                    </div>
                )}

                {messages.map((m, index) => (
                    <MessageBubble
                        key={m.id ?? index}
                        message={toMessageProps(m)}
                        onReply={() => {
                            setReplyingTo(m);
                            inputRef.current?.focus();
                        }}
                        onReact={(emoji) => handleReaction(m.id!, emoji)}
                    />
                ))}

                <div ref={messagesEndRef} />
            </div>

            {/* Reply preview */}
            {replyingTo && (
                <div className="mx-4 mb-1 px-3 py-2 bg-slate-800 border-l-2 border-blue-500 rounded flex items-center justify-between">
                    <div className="min-w-0">
                        <div className="text-xs text-blue-400 font-medium">
                            Replying to {replyingTo.senderName}
                        </div>
                        <div className="text-xs text-slate-400 truncate">
                            {replyingTo.content}
                        </div>
                    </div>
                    <button
                        onClick={() => setReplyingTo(null)}
                        className="text-slate-500 hover:text-white ml-2 shrink-0">
                        <X size={14} />
                    </button>
                </div>
            )}

            {/* Input */}
            <div className="border-t border-slate-800 px-4 py-3 bg-slate-900 shrink-0">
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
                            flex-1 bg-slate-800 border border-slate-700 rounded-xl
                            px-4 py-3 text-sm text-white outline-none
                            focus:border-blue-500 placeholder:text-slate-500
                            disabled:opacity-50 transition-colors"
                    />
                    <button
                        onClick={sendMessage}
                        disabled={!messageInput.trim() || sending || !ready}
                        className="
                            p-3 rounded-xl bg-blue-600 hover:bg-blue-700
                            disabled:opacity-40 disabled:cursor-not-allowed
                            transition-colors shrink-0">
                        <Send size={17} />
                    </button>
                </div>
            </div>
        </div>
    );
}