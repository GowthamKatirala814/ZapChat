import { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Bell, CheckCheck, Trash2 } from "lucide-react";
import { useNavigate } from "react-router-dom";
import type { RootState, AppDispatch } from "../../store/store";
import {
    setNotifications,
    markOneRead,
    markAllReadLocal,
    removeNotification
} from "../../store/notificationSlice";
import {
    getNotifications,
    markAsRead,
    markAllAsRead,
    deleteNotification
} from "../../api/notificationApi";

export default function NotificationsPage() {
    const dispatch = useDispatch<AppDispatch>();
    const navigate = useNavigate();
    const { items } = useSelector(
        (s: RootState) => s.notifications
    );
    const userId = localStorage.getItem("userId") ?? "";

    useEffect(() => {
        if (!userId) return;
        getNotifications(userId).then(data =>
            dispatch(setNotifications(data))
        );
    }, [dispatch, userId]);

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

    const unreadCount = items.filter(n => !n.isRead).length;

    return (
        <div className="
            min-h-screen bg-slate-950 text-white
            flex flex-col">

            {/* Header */}
            <div className="
                border-b border-slate-800 px-6 py-4
                flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <button
                        onClick={() => navigate("/dashboard")}
                        className="
                            text-slate-400 hover:text-white
                            text-sm">
                        ← Dashboard
                    </button>
                    <div className="flex items-center gap-2">
                        <Bell size={20} className="text-blue-400" />
                        <h1 className="text-xl font-bold">
                            Notifications
                        </h1>
                        {unreadCount > 0 && (
                            <span className="
                                bg-blue-600 text-white
                                text-xs rounded-full
                                px-2 py-0.5">
                                {unreadCount}
                            </span>
                        )}
                    </div>
                </div>

                {unreadCount > 0 && (
                    <button
                        onClick={handleMarkAll}
                        className="
                            flex items-center gap-2
                            text-sm text-blue-400
                            hover:text-blue-300
                            transition-colors">
                        <CheckCheck size={16} />
                        Mark all as read
                    </button>
                )}
            </div>

            {/* List */}
            <div className="flex-1 max-w-2xl w-full mx-auto px-4 py-6 space-y-3">
                {items.length === 0 ? (
                    <div className="
                        text-center text-slate-500
                        py-20 space-y-3">
                        <Bell
                            size={48}
                            className="mx-auto opacity-30"
                        />
                        <p className="text-lg">
                            No notifications yet
                        </p>
                        <p className="text-sm">
                            You'll see room and DM activity here
                        </p>
                    </div>
                ) : (
                    items.map(n => (
                        <div
                            key={n.id}
                            className={`
                                flex items-start gap-4
                                p-4 rounded-xl border
                                transition-colors
                                ${!n.isRead
                                    ? "bg-slate-900 border-blue-500/30"
                                    : "bg-slate-900/50 border-slate-800"
                                }
                            `}
                        >
                            {/* Indicator */}
                            <div className="mt-1 shrink-0">
                                {!n.isRead ? (
                                    <span className="
                                        block w-2.5 h-2.5
                                        rounded-full bg-blue-500" />
                                ) : (
                                    <span className="
                                        block w-2.5 h-2.5
                                        rounded-full bg-slate-700" />
                                )}
                            </div>

                            {/* Content */}
                            <div className="flex-1 min-w-0">
                                <p className="
                                    font-semibold text-sm text-white">
                                    {n.title}
                                </p>
                                <p className="
                                    text-sm text-slate-400 mt-0.5">
                                    {n.message}
                                </p>
                                <p className="
                                    text-xs text-slate-600 mt-2">
                                    {new Date(n.createdAt)
                                        .toLocaleString()}
                                </p>
                            </div>

                            {/* Actions */}
                            <div className="flex items-center gap-2 shrink-0">
                                {!n.isRead && (
                                    <button
                                        onClick={() =>
                                            handleMarkOne(n.id)
                                        }
                                        title="Mark as read"
                                        className="
                                            text-slate-400
                                            hover:text-green-400
                                            p-1.5 rounded-lg
                                            hover:bg-slate-800
                                            transition-colors">
                                        <CheckCheck size={16} />
                                    </button>
                                )}
                                <button
                                    onClick={() => handleDelete(n.id)}
                                    title="Delete"
                                    className="
                                        text-slate-400
                                        hover:text-red-400
                                        p-1.5 rounded-lg
                                        hover:bg-slate-800
                                        transition-colors">
                                    <Trash2 size={16} />
                                </button>
                            </div>
                        </div>
                    ))
                )}
            </div>
        </div>
    );
}
