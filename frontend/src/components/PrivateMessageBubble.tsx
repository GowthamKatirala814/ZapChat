import { useState } from "react";
import type { PrivateMessage } from "../types/PrivateMessage";
import { getAnonymousName } from "../utils/auth";
import { Reply, SmilePlus, Flag, Trash2 } from "lucide-react";
import ReportMessageModal from "./ReportMessageModal";

const EMOJI_LIST = ["👍", "❤️", "😂", "🔥", "🎉"];

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

function groupReactions(reactions: PrivateMessage["reactions"]) {
    if (!reactions || reactions.length === 0) return [];
    const map = new Map<string, string[]>();
    for (const r of reactions) {
        if (!map.has(r.reaction)) map.set(r.reaction, []);
        map.get(r.reaction)!.push(r.senderName);
    }
    return Array.from(map.entries()).map(([emoji, names]) => ({ emoji, names, count: names.length }));
}

interface Props {
    message: PrivateMessage;
    onReply?: () => void;
    onReact?: (emoji: string) => void;
    onDelete?: () => void;
}

export default function PrivateMessageBubble({ message, onReply, onReact, onDelete }: Props) {
    const [showPicker, setShowPicker] = useState(false);
    const [showReport, setShowReport] = useState(false);
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
    const myName = getAnonymousName();
    const isMe = message.senderName === myName;

    const canDelete = isMe
        && !message.isDeleted
        && !!message.sentAt
        && (Date.now() - new Date(message.sentAt).getTime()) < 24 * 60 * 60 * 1000;
    const color = colorFor(message.senderName ?? "");
    const initial = (message.senderName ?? "?").charAt(0).toUpperCase();

    const formattedTime = message.sentAt
        ? new Date(message.sentAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
        : "";

    const reactionGroups = groupReactions(message.reactions);

    return (
        <>
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
                    <span className={`text-xs font-semibold ${isMe ? "text-sky-500" : "text-slate-500"}`}>
                        {isMe ? "You" : message.senderName}
                    </span>
                    <span className="text-xs text-slate-400">{formattedTime}</span>
                </div>

                {/* Reply quote */}
                {message.parentMessageId && (
                    <div className="
                        mb-1 px-3 py-1.5 rounded-lg
                        bg-sky-50 border-l-2 border-sky-300
                        text-xs text-slate-500 max-w-full truncate">
                        ↩ Replying to a message
                    </div>
                )}

                {message.isDeleted ? (
                    <div className="px-4 py-2.5 rounded-2xl text-sm italic"
                        style={{
                            background: isMe ? '#DBEAFE' : '#F1F5F9',
                            color: '#94A3B8'
                        }}>
                    {isMe ? "You deleted this message" : "Message removed by moderation."}
                    </div>
                ) : (
                    <div className="relative flex items-end gap-1">
                        {/* Actions — other user messages */}
                        {!isMe && (
                            <div className="
                                flex items-center gap-1 opacity-0 group-hover:opacity-100
                                transition-opacity mb-1">
                                <button
                                    onClick={() => setShowPicker(p => !p)}
                                    title="React"
                                    className="
                                        p-1 rounded-lg text-slate-400
                                        hover:text-slate-700 hover:bg-slate-100
                                        transition-colors">
                                    <SmilePlus size={14} />
                                </button>
                                {onReply && (
                                    <button
                                        onClick={onReply}
                                        title="Reply"
                                        className="
                                            p-1 rounded-lg text-slate-400
                                            hover:text-slate-700 hover:bg-slate-100
                                            transition-colors">
                                        <Reply size={14} />
                                    </button>
                                )}
                                <button
                                    onClick={() => setShowReport(true)}
                                    title="Report"
                                    className="
                                        p-1 rounded-lg text-slate-400
                                        hover:text-red-500 hover:bg-red-50
                                        transition-colors">
                                    <Flag size={14} />
                                </button>
                            </div>
                        )}

                        {/* Emoji picker popover */}
                        {showPicker && (
                            <div className="
                                absolute bottom-10 left-0 z-20
                                bg-white border border-slate-200
                                rounded-xl px-2 py-1.5 flex gap-1
                                shadow-lg">
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
                                ? "bg-sky-500 text-white rounded-br-sm shadow-sm"
                                : "bg-white text-slate-800 rounded-bl-sm shadow-sm border border-slate-200"
                            }`}>
                            {message.content}
                        </div>

                        {/* My message action buttons */}
                        {isMe && (
                            <div className="
                                flex items-center gap-1 opacity-0 group-hover:opacity-100
                                transition-opacity mb-1">
                                <button
                                    onClick={() => setShowPicker(p => !p)}
                                    title="React"
                                    className="
                                        p-1 rounded-lg text-slate-400
                                        hover:text-slate-700 hover:bg-slate-100
                                        transition-colors">
                                    <SmilePlus size={14} />
                                </button>
                                {onReply && (
                                    <button
                                        onClick={onReply}
                                        title="Reply"
                                        className="
                                            p-1 rounded-lg text-slate-400
                                            hover:text-slate-700 hover:bg-slate-100
                                            transition-colors">
                                        <Reply size={14} />
                                    </button>
                                )}
                                {canDelete && (
                                    <button
                                        onClick={() => setShowDeleteConfirm(true)}
                                        title="Delete"
                                        className="
                                            p-1 rounded-lg text-slate-400
                                            hover:text-red-500 hover:bg-red-50
                                            transition-colors">
                                        <Trash2 size={14} />
                                    </button>
                                )}
                            </div>
                        )}
                    </div>
                )}

                {/* Delete confirmation inline */}
                {showDeleteConfirm && (
                    <div className="mt-1 flex items-center gap-2 px-3 py-2 rounded-xl bg-white border border-red-200"
                        style={{ boxShadow: "0 2px 8px rgba(0,0,0,0.06)" }}>
                        <span className="text-xs text-slate-600">Delete this message?</span>
                        <button
                            onClick={() => { onDelete?.(); setShowDeleteConfirm(false); }}
                            className="text-xs text-red-500 hover:text-red-600 font-medium transition-colors">
                            Delete
                        </button>
                        <button
                            onClick={() => setShowDeleteConfirm(false)}
                            className="text-xs text-slate-400 hover:text-slate-600 transition-colors">
                            Cancel
                        </button>
                    </div>
                )}

                {/* Reactions row */}
                {reactionGroups.length > 0 && !message.isDeleted && (
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
                                            ? "bg-sky-50 border-sky-300 text-sky-700"
                                            : "bg-white border-slate-200 text-slate-600 hover:border-slate-300"
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

            {showReport && message.id && (
                <ReportMessageModal
                    messageId={message.id}
                    messageType={1}
                    onClose={() => setShowReport(false)}
                />
            )}
        </>
    );
}