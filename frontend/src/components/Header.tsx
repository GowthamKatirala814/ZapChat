import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Bell, Search, Hash, Users } from "lucide-react";
import type { RootState, AppDispatch } from "../store/store";
import { setNotifications, addNotification } from "../store/notificationSlice";
import { getNotifications } from "../api/notificationApi";
import { getNotificationConnection } from "../hubs/notificationHub";
import NotificationPanel from "./NotificationPanel";
import type { Notification } from "../types/Notification";

interface Props {
    roomName: string;
    memberCount?: number;
}

export default function Header({ roomName, memberCount }: Props) {
    const dispatch = useDispatch<AppDispatch>();
    const [panelOpen, setPanelOpen] = useState(false);
    const unreadCount = useSelector(
        (s: RootState) => s.notifications.unreadCount
    );
    const userId = localStorage.getItem("userId") ?? "";

    useEffect(() => {
        if (!userId) return;

        getNotifications(userId)
            .then(data => dispatch(setNotifications(data)))
            .catch(() => { /* silent */ });

        const conn = getNotificationConnection();
        conn.off("ReceiveNotification");
        conn.on("ReceiveNotification", (n: Notification) => {
            dispatch(addNotification(n));
        });

        if (conn.state === "Disconnected") {
            conn.start().catch(console.error);
        }

        return () => { conn.off("ReceiveNotification"); };
    }, [dispatch, userId]);

    return (
        <div className="
            h-14 border-b border-slate-800
            px-5 flex items-center justify-between
            bg-slate-950 shrink-0">

            {/* Left: room info */}
            <div className="flex items-center gap-2.5">
                <Hash size={18} className="text-slate-500" />
                <div>
                    <span className="font-semibold text-white text-sm">
                        {roomName}
                    </span>
                    {memberCount !== undefined && (
                        <span className="ml-2 text-xs text-slate-500">
                            <Users size={11} className="inline mr-1" />
                            {memberCount}
                        </span>
                    )}
                </div>
                <div className="
                    h-4 w-px bg-slate-800 mx-1" />
                <span className="text-xs text-slate-500 hidden lg:block">
                    Enterprise anonymous workspace
                </span>
            </div>

            {/* Right: actions */}
            <div className="flex items-center gap-1">
                <button className="
                    p-2 rounded-lg text-slate-500
                    hover:text-white hover:bg-slate-800
                    transition-colors">
                    <Search size={17} />
                </button>

                {/* Bell with badge */}
                <div className="relative">
                    <button
                        onClick={() => setPanelOpen(prev => !prev)}
                        className="
                            relative p-2 rounded-lg text-slate-500
                            hover:text-white hover:bg-slate-800
                            transition-colors">
                        <Bell size={17} />
                        {unreadCount > 0 && (
                            <span className="
                                absolute top-1 right-1
                                bg-red-500 text-white text-[9px]
                                font-bold w-3.5 h-3.5 rounded-full
                                flex items-center justify-center">
                                {unreadCount > 9 ? "9+" : unreadCount}
                            </span>
                        )}
                    </button>

                    {panelOpen && (
                        <NotificationPanel
                            onClose={() => setPanelOpen(false)}
                        />
                    )}
                </div>
            </div>
        </div>
    );
}