import { useEffect, useRef, useState, useCallback } from "react";
import { connection } from "../hubs/chatHub";
import { getRoomMessages, deleteMessage } from "../api/chatApi";
import MessageBubble from "./MessageBubble";
import type { Message } from "../types/Message";
import { Send, X } from "lucide-react";

interface Props {
    roomName: string;
}

export default function ChatWindow({ roomName }: Props) {
    const [messages, setMessages] = useState<Message[]>([]);
    const [message, setMessage] = useState("");
    const [currentRoom, setCurrentRoom] = useState("");
    const [typingUser, setTypingUser] = useState("");
    const [statusMsg, setStatusMsg] = useState("");
    const [replyingTo, setReplyingTo] = useState<Message | null>(null);

    const bottomRef = useRef<HTMLDivElement>(null);
    const typingTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
    const inputRef = useRef<HTMLInputElement>(null);

    // Auto-scroll
    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);

    // Load history when room changes
    const loadHistory = useCallback(async (room: string) => {
        try {
            const history = await getRoomMessages(room);
            setMessages(history);
        } catch (err) {
            console.error("[ChatWindow] History load failed:", err);
            setMessages([]);
        }
    }, []);

    // Mount: register all handlers once, start connection, join initial room
    useEffect(() => {
        const handleReceiveMessage = (data: Message) => {
            setMessages(prev => {
                if (prev.some(m => m.id === data.id)) return prev;
                return [...prev, data];
            });
        };

        const handleUserJoined = (msg: string) => {
            setStatusMsg(msg);
            setTimeout(() => setStatusMsg(""), 3000);
        };

        const handleUserLeft = (msg: string) => {
            setStatusMsg(msg);
            setTimeout(() => setStatusMsg(""), 3000);
        };

        const handleUserTyping = (name: string) => setTypingUser(name);
        const handleUserStoppedTyping = () => setTypingUser("");

        const handleReactionAdded = (data: {
            messageId: string;
            anonymousName: string;
            reaction: string;
        }) => {
            setMessages(prev => prev.map(m => {
                if (m.id !== data.messageId) return m;
                const existing = m.reactions ?? [];
                const filtered = existing.filter(
                    r => !(r.anonymousName === data.anonymousName && r.reaction === data.reaction)
                );
                const toggled = filtered.length === existing.length
                    ? [...existing, { anonymousName: data.anonymousName, reaction: data.reaction }]
                    : filtered;
                return { ...m, reactions: toggled };
            }));
        };

        const handleMessageDeleted = (data: { messageId: string; deletedAt: string }) => {
            setMessages(prev => prev.map(m =>
                m.id === data.messageId ? { ...m, isDeleted: true, deletedAt: data.deletedAt } : m
            ));
        };

        connection.on("ReceiveMessage", handleReceiveMessage);
        connection.on("UserJoined", handleUserJoined);
        connection.on("UserLeft", handleUserLeft);
        connection.on("UserTyping", handleUserTyping);
        connection.on("UserStoppedTyping", handleUserStoppedTyping);
        connection.on("ReactionAdded", handleReactionAdded);
        connection.on("MessageDeleted", handleMessageDeleted);

        const boot = async () => {
            try {
                const token = localStorage.getItem("token");
                if (!token) {
                    console.error("[ChatHub] No token — aborting connection");
                    return;
                }
                if (connection.state === "Disconnected") {
                    await connection.start();
                }
                await connection.invoke("JoinRoom", roomName);
                setCurrentRoom(roomName);
                await loadHistory(roomName);
            } catch (err) {
                console.error("[ChatHub] boot error:", err);
            }
        };

        boot();

        return () => {
            connection.off("ReceiveMessage", handleReceiveMessage);
            connection.off("UserJoined", handleUserJoined);
            connection.off("UserLeft", handleUserLeft);
            connection.off("UserTyping", handleUserTyping);
            connection.off("UserStoppedTyping", handleUserStoppedTyping);
            connection.off("ReactionAdded", handleReactionAdded);
            connection.off("MessageDeleted", handleMessageDeleted);
        };
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    // Room switch
    useEffect(() => {
        if (!currentRoom || currentRoom === roomName) return;

        const switchRoom = async () => {
            try {
                if (connection.state !== "Connected") return;
                await connection.invoke("LeaveRoom", currentRoom);
                await connection.invoke("JoinRoom", roomName);
                setCurrentRoom(roomName);
                setMessages([]);
                setReplyingTo(null);
                await loadHistory(roomName);
            } catch (err) {
                console.error("[ChatHub] room switch error:", err);
            }
        };

        switchRoom();
    }, [roomName, currentRoom, loadHistory]);

    const handleTyping = useCallback(async (value: string) => {
        setMessage(value);
        if (connection.state !== "Connected") return;
        try {
            if (value.trim()) {
                await connection.invoke("Typing", roomName);
                if (typingTimer.current) clearTimeout(typingTimer.current);
                typingTimer.current = setTimeout(async () => {
                    try { await connection.invoke("StopTyping", roomName); }
                    catch { /* ignore */ }
                }, 2000);
            } else {
                if (typingTimer.current) clearTimeout(typingTimer.current);
                await connection.invoke("StopTyping", roomName);
            }
        } catch { /* ignore */ }
    }, [roomName]);

    const sendMessage = useCallback(async () => {
        const text = message.trim();
        if (!text) return;

        try {
            if (connection.state === "Disconnected") {
                await connection.start();
                await connection.invoke("JoinRoom", roomName);
            }
            await connection.invoke(
                "SendMessage",
                roomName,
                text,
                replyingTo?.id ?? null
            );
            await connection.invoke("StopTyping", roomName);
            setMessage("");
            setReplyingTo(null);
        } catch (err) {
            console.error("[ChatHub] SendMessage error:", err);
        }
    }, [message, roomName, replyingTo]);

    const handleReaction = useCallback(async (messageId: string, emoji: string) => {
        if (connection.state !== "Connected") return;
        try {
            await connection.invoke("AddReaction", messageId, emoji);
        } catch (err) {
            console.error("[ChatHub] AddReaction error:", err);
        }
    }, []);

    return (
        <div className="h-full flex flex-col overflow-hidden" style={{ background: "#F8FAFC" }}>

            {/* System status toast */}
            {statusMsg && (
                <div className="px-6 py-1.5 text-xs text-slate-500 bg-white border-b border-slate-100 text-center">
                    {statusMsg}
                </div>
            )}

            {/* Messages */}
            <div className="flex-1 overflow-y-auto px-4 py-4 space-y-1">
                {messages.length === 0 && (
                    <div className="flex flex-col items-center justify-center h-full text-slate-400 gap-2">
                        <p className="text-4xl">💬</p>
                        <p className="text-sm">No messages yet in #{roomName}</p>
                        <p className="text-xs">Be the first to say something</p>
                    </div>
                )}

                {messages.map((m, i) => (
                    <MessageBubble
                        key={m.id ?? i}
                        message={m}
                        onReply={() => {
                            setReplyingTo(m);
                            inputRef.current?.focus();
                        }}
                        onReact={(emoji) => handleReaction(m.id!, emoji)}
                        onDelete={m.id ? async () => {
                            try {
                                await deleteMessage(m.id!);
                                setMessages(prev => prev.map(msg =>
                                    msg.id === m.id
                                        ? { ...msg, isDeleted: true, deletedAt: new Date().toISOString() }
                                        : msg
                                ));
                            } catch (err) {
                                console.error("[ChatWindow] DeleteMessage error:", err);
                            }
                        } : undefined}
                    />
                ))}

                {typingUser && (
                    <div className="text-xs text-slate-500 italic px-2 mt-1">
                        {typingUser} is typing…
                    </div>
                )}

                <div ref={bottomRef} />
            </div>

            {/* Reply preview */}
            {replyingTo && (
                <div
                    className="mx-4 mb-1 px-3 py-2 rounded flex items-center justify-between"
                    style={{ background: "#EFF6FF", borderLeft: "2px solid #38BDF8" }}
                >
                    <div className="min-w-0">
                        <div className="text-xs text-sky-600 font-medium">
                            Replying to {replyingTo.anonymousName}
                        </div>
                        <div className="text-xs text-slate-500 truncate">
                            {replyingTo.message}
                        </div>
                    </div>
                    <button
                        onClick={() => setReplyingTo(null)}
                        className="text-slate-400 hover:text-slate-700 ml-2 shrink-0">
                        <X size={14} />
                    </button>
                </div>
            )}

            {/* Input bar */}
            <div className="border-t border-slate-200 px-4 py-3 shrink-0 bg-white">
                <div className="flex items-center gap-3">
                    <input
                        ref={inputRef}
                        value={message}
                        onChange={e => handleTyping(e.target.value)}
                        onKeyDown={e => {
                            if (e.key === "Enter" && !e.shiftKey) {
                                e.preventDefault();
                                sendMessage();
                            }
                            if (e.key === "Escape") setReplyingTo(null);
                        }}
                        placeholder={replyingTo ? `Reply to ${replyingTo.anonymousName}…` : `Message #${roomName}`}
                        className="
                            flex-1 bg-white border border-slate-200 rounded-xl
                            px-4 py-3 text-sm text-slate-900 outline-none
                            focus:border-sky-400 placeholder:text-slate-400
                            transition-colors"
                    />
                    <button
                        onClick={sendMessage}
                        disabled={!message.trim()}
                        className="
                            p-3 rounded-xl bg-sky-500 hover:bg-sky-600 text-white
                            disabled:opacity-40 disabled:cursor-not-allowed
                            transition-colors shrink-0">
                        <Send size={17} />
                    </button>
                </div>
            </div>
        </div>
    );
}