import { useEffect, useRef } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Check, CheckCheck, Trash2, X } from "lucide-react";
import type { RootState, AppDispatch } from "../store/store";
import {
    markOneRead,
    markAllReadLocal,
    removeNotification
} from "../store/notificationSlice";
import {
    markAsRead,
    markAllAsRead,
    deleteNotification
} from "../api/notificationApi";

interface Props {
    onClose: () => void;
}

export default function NotificationPanel({ onClose }: Props) {
    const dispatch = useDispatch<AppDispatch>();
    const { items } = useSelector(
        (s: RootState) => s.notifications
    );
    const userId = localStorage.getItem("userId") ?? "";
    const panelRef = useRef<HTMLDivElement>(null);

    // Close on outside click
    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (
                panelRef.current &&
                !panelRef.current.contains(e.target as Node)
            ) {
                onClose();
            }
        };
        document.addEventListener("mousedown", handler);
        return () =>
            document.removeEventListener("mousedown", handler);
    }, [onClose]);

    const handleMarkOne = async (id: string) => {
        dispatch(markOneRead(id));
        await markAsRead(id);
    };

    const handleMarkAll = async () => {
        dispatch(markAllReadLocal());
        await markAllAsRead(userId);
    };

    const handleDelete = async (id: string) => {
        dispatch(removeNotification(id));
        await deleteNotification(id);
    };

    return (
        <div
            ref={panelRef}
            className="
                absolute right-0 top-12 z-50
                w-96 max-h-[480px] overflow-y-auto
                bg-slate-900 border border-slate-700
                rounded-xl shadow-2xl
                flex flex-col"
        >
            {/* Header */}
            <div className="
                flex items-center justify-between
                px-4 py-3
                border-b border-slate-700">
                <span className="font-semibold text-sm">
                    Notifications
                </span>
                <div className="flex items-center gap-3">
                    {items.some(n => !n.isRead) && (
                        <button
                            onClick={handleMarkAll}
                            title="Mark all as read"
                            className="
                                text-xs text-blue-400
                                hover:text-blue-300
                                flex items-center gap-1">
                            <CheckCheck size={14} />
                            All read
                        </button>
                    )}
                    <button
                        onClick={onClose}
                        className="text-slate-400 hover:text-white">
                        <X size={16} />
                    </button>
                </div>
            </div>

            {/* List */}
            <div className="flex-1">
                {items.length === 0 ? (
                    <div className="
                        text-center text-slate-500
                        text-sm py-10">
                        No notifications
                    </div>
                ) : (
                    items.map(n => (
                        <div
                            key={n.id}
                            className={`
                                flex items-start gap-3
                                px-4 py-3
                                border-b border-slate-800
                                hover:bg-slate-800
                                transition-colors
                                ${!n.isRead ? "bg-slate-800/60" : ""}
                            `}
                        >
                            {/* Unread dot */}
                            <div className="mt-1.5 shrink-0">
                                {!n.isRead ? (
                                    <span className="
                                        block w-2 h-2 rounded-full
                                        bg-blue-500" />
                                ) : (
                                    <span className="
                                        block w-2 h-2 rounded-full
                                        bg-transparent" />
                                )}
                            </div>

                            <div className="flex-1 min-w-0">
                                <div className="
                                    text-sm font-medium
                                    text-white truncate">
                                    {n.title}
                                </div>
                                <div className="
                                    text-xs text-slate-400
                                    mt-0.5">
                                    {n.message}
                                </div>
                                <div className="
                                    text-xs text-slate-600 mt-1">
                                    {new Date(n.createdAt)
                                        .toLocaleString()}
                                </div>
                            </div>

                            <div className="
                                flex items-center gap-1
                                shrink-0 ml-2">
                                {!n.isRead && (
                                    <button
                                        onClick={() => handleMarkOne(n.id)}
                                        title="Mark as read"
                                        className="
                                            text-slate-400
                                            hover:text-green-400
                                            p-1 rounded">
                                        <Check size={14} />
                                    </button>
                                )}
                                <button
                                    onClick={() => handleDelete(n.id)}
                                    title="Delete"
                                    className="
                                        text-slate-400
                                        hover:text-red-400
                                        p-1 rounded">
                                    <Trash2 size={14} />
                                </button>
                            </div>
                        </div>
                    ))
                )}
            </div>
        </div>
    );
}
