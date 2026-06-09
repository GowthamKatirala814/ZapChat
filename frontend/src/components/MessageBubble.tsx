import { useState } from "react";
import type { Message } from "../types/Message";
import { getAnonymousName } from "../utils/auth";
import { Reply, SmilePlus } from "lucide-react";

interface Props {
    message: Message;
    onReply?: () => void;
    onReact?: (emoji: string) => void;
}

const EMOJI_LIST = ["👍", "❤️", "😂", "🔥", "🎉"];

// Deterministic gradient per anonymous name for visual identity
const AVATAR_COLORS = [
    "from-blue-500 to-cyan-500",
    "from-violet-500 to-purple-600",
    "from-emerald-500 to-teal-600",
    "from-orange-500 to-amber-600",
    "from-pink-500 to-rose-600",
    "from-sky-500 to-indigo-600",
];

function colorFor(name: string): string {
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
        hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
}

// Group reactions by emoji and count them
function groupReactions(reactions: Message["reactions"]) {
    if (!reactions || reactions.length === 0) return [];
    const map = new Map<string, string[]>();
    for (const r of reactions) {
        if (!map.has(r.reaction)) map.set(r.reaction, []);
        map.get(r.reaction)!.push(r.anonymousName);
    }
    return Array.from(map.entries()).map(([emoji, names]) => ({ emoji, names, count: names.length }));
}

export default function MessageBubble({ message, onReply, onReact }: Props) {
    const [showPicker, setShowPicker] = useState(false);
    const myName = getAnonymousName();
    const isMe = message.anonymousName === myName;
    const color = colorFor(message.anonymousName ?? "");
    const initial = (message.anonymousName ?? "?").charAt(0).toUpperCase();

    const formattedTime = message.sentAt
        ? new Date(message.sentAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
        : "";

    const reactionGroups = groupReactions(message.reactions);

    return (
        <div
            className={`group flex gap-3 mb-2 ${isMe ? "flex-row-reverse" : ""}`}
            onMouseLeave={() => setShowPicker(false)}
        >
            {/* Avatar */}
            <div className={`
                w-8 h-8 rounded-full shrink-0 mt-0.5 self-end
                bg-gradient-to-br ${color}
                flex items-center justify-center
                text-xs font-bold text-white`}>
                {initial}
            </div>

            {/* Content */}
            <div className={`max-w-xs lg:max-w-lg flex flex-col ${isMe ? "items-end" : "items-start"}`}>

                {/* Author + time */}
                <div className={`flex items-baseline gap-2 mb-0.5 ${isMe ? "flex-row-reverse" : ""}`}>
                    <span className={`text-xs font-semibold ${isMe ? "text-blue-400" : "text-violet-400"}`}>
                        {isMe ? "You" : message.anonymousName}
                    </span>
                    <span className="text-xs text-slate-600">{formattedTime}</span>
                </div>

                {/* Reply quote */}
                {message.parentMessageId && (
                    <div className="
                        mb-1 px-3 py-1.5 rounded-lg
                        bg-slate-700/60 border-l-2 border-slate-500
                        text-xs text-slate-400 max-w-full truncate">
                        ↩ Replying to a message
                    </div>
                )}

                {/* Bubble + action buttons */}
                <div className="relative flex items-end gap-1">
                    {/* Action buttons — show on hover */}
                    {!isMe && (
                        <div className="
                            flex items-center gap-1 opacity-0 group-hover:opacity-100
                            transition-opacity mb-1">
                            <button
                                onClick={() => setShowPicker(p => !p)}
                                title="React"
                                className="
                                    p-1 rounded-lg text-slate-500
                                    hover:text-white hover:bg-slate-700
                                    transition-colors">
                                <SmilePlus size={14} />
                            </button>
                            {onReply && (
                                <button
                                    onClick={onReply}
                                    title="Reply"
                                    className="
                                        p-1 rounded-lg text-slate-500
                                        hover:text-white hover:bg-slate-700
                                        transition-colors">
                                    <Reply size={14} />
                                </button>
                            )}
                        </div>
                    )}

                    {/* Emoji picker popover */}
                    {showPicker && (
                        <div className="
                            absolute bottom-10 left-0 z-20
                            bg-slate-800 border border-slate-700
                            rounded-xl px-2 py-1.5 flex gap-1
                            shadow-xl">
                            {EMOJI_LIST.map(emoji => (
                                <button
                                    key={emoji}
                                    onClick={() => {
                                        onReact?.(emoji);
                                        setShowPicker(false);
                                    }}
                                    className="
                                        text-lg hover:scale-125
                                        transition-transform px-0.5">
                                    {emoji}
                                </button>
                            ))}
                        </div>
                    )}

                    <div className={`
                        px-4 py-2.5 rounded-2xl text-sm leading-relaxed break-words
                        ${isMe
                            ? "bg-blue-600 text-white rounded-br-sm"
                            : "bg-slate-800 text-slate-100 rounded-bl-sm"
                        }`}>
                        {message.message}
                    </div>

                    {/* My message actions */}
                    {isMe && (
                        <div className="
                            flex items-center gap-1 opacity-0 group-hover:opacity-100
                            transition-opacity mb-1">
                            <button
                                onClick={() => setShowPicker(p => !p)}
                                title="React"
                                className="
                                    p-1 rounded-lg text-slate-500
                                    hover:text-white hover:bg-slate-700
                                    transition-colors">
                                <SmilePlus size={14} />
                            </button>
                            {onReply && (
                                <button
                                    onClick={onReply}
                                    title="Reply"
                                    className="
                                        p-1 rounded-lg text-slate-500
                                        hover:text-white hover:bg-slate-700
                                        transition-colors">
                                    <Reply size={14} />
                                </button>
                            )}
                        </div>
                    )}
                </div>

                {/* Reactions row */}
                {reactionGroups.length > 0 && (
                    <div className="flex flex-wrap gap-1 mt-1">
                        {reactionGroups.map(({ emoji, count, names }) => {
                            const iReacted = names.includes(myName);
                            return (
                                <button
                                    key={emoji}
                                    onClick={() => onReact?.(emoji)}
                                    title={names.join(", ")}
                                    className={`
                                        flex items-center gap-1 text-xs
                                        px-2 py-0.5 rounded-full border
                                        transition-colors
                                        ${iReacted
                                            ? "bg-blue-600/20 border-blue-500/40 text-blue-300"
                                            : "bg-slate-800 border-slate-700 text-slate-300 hover:border-slate-500"
                                        }`}>
                                    <span>{emoji}</span>
                                    <span className="font-medium">{count}</span>
                                </button>
                            );
                        })}
                    </div>
                )}
            </div>
        </div>
    );
}
