import { useEffect, useRef, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
    Bell,
    LogOut,
    ChevronDown,
    Hash,
    Users,
} from "lucide-react";
import type { RootState, AppDispatch } from "../store/store";
import { setNotifications, addNotification } from "../store/notificationSlice";
import { getNotifications } from "../api/notificationApi";
import { getNotificationConnection } from "../hubs/notificationHub";
import { connection as chatHubConnection } from "../hubs/chatHub";
import { logout, getAnonymousName } from "../utils/auth";
import NotificationPanel from "./NotificationPanel";
import type { Notification } from "../types/Notification";

interface Props {
    /** Active channel / page title */
    title: string;
    /** Optional member count shown next to room name */
    memberCount?: number;
    /** Optional subtitle shown under the title */
    subtitle?: string;
}

export default function AppHeader({ title, memberCount, subtitle }: Props) {
    const dispatch = useDispatch<AppDispatch>();
    const [bellOpen, setBellOpen]       = useState(false);
    const [profileOpen, setProfileOpen] = useState(false);

    const profileRef = useRef<HTMLDivElement>(null);
    const bellRef    = useRef<HTMLDivElement>(null);

    const unreadCount = useSelector((s: RootState) => s.notifications.unreadCount);
    const userId  = localStorage.getItem("userId") ?? "";
    const myName  = getAnonymousName();
    const myEmail = localStorage.getItem("email") ?? "";
    const myRole  = localStorage.getItem("role") ?? "user";

    // Boot notification hub once
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

        chatHubConnection.off("GlobalNotification");
        chatHubConnection.on("GlobalNotification", (n: Notification) => {
            dispatch(addNotification(n));
        });

        return () => { 
            conn.off("ReceiveNotification"); 
            chatHubConnection.off("GlobalNotification");
        };
    }, [dispatch, userId]);

    // Close dropdowns on outside click
    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (profileRef.current && !profileRef.current.contains(e.target as Node)) {
                setProfileOpen(false);
            }
            if (bellRef.current && !bellRef.current.contains(e.target as Node)) {
                setBellOpen(false);
            }
        };
        document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, []);

    return (
        <header
            className="shrink-0 flex items-center justify-between px-5 gap-4 z-30"
            style={{
                height: "56px",
                background: "rgba(13,22,40,0.95)",
                borderBottom: "1px solid rgba(255,255,255,0.06)",
                backdropFilter: "blur(12px)",
            }}
        >
            {/* ── LEFT: Channel / room info ───────────────────────── */}
            <div className="flex items-center gap-2.5 min-w-0 flex-shrink-0" style={{ maxWidth: "260px" }}>
                <div
                    className="w-7 h-7 rounded-lg flex items-center justify-center shrink-0"
                    style={{ background: "rgba(6,182,212,0.15)" }}
                >
                    <Hash size={14} style={{ color: "#06b6d4" }} />
                </div>
                <div className="min-w-0">
                    <div className="flex items-center gap-2">
                        <span className="font-bold text-white text-sm truncate">
                            {title}
                        </span>
                        {memberCount !== undefined && (
                            <span
                                className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md shrink-0"
                                style={{
                                    background: "rgba(255,255,255,0.06)",
                                    color: "#64748b",
                                }}
                            >
                                <Users size={10} />
                                {memberCount}
                            </span>
                        )}
                    </div>
                    {subtitle ? (
                        <div className="text-[10px] text-slate-500 truncate">{subtitle}</div>
                    ) : (
                        <div className="text-[10px] text-slate-600 hidden lg:block">
                            Enterprise anonymous workspace
                        </div>
                    )}
                </div>
            </div>

            
            {/* ── RIGHT: Actions ───────────────────────────────────── */}
            <div className="flex items-center gap-1.5 shrink-0">

                {/* Notification Bell */}
                <div ref={bellRef} className="relative">
                    <button
                        id="notification-bell"
                        onClick={() => {
                            setBellOpen(p => !p);
                            setProfileOpen(false);
                        }}
                        className="relative p-2 rounded-lg transition-all duration-150"
                        style={{ color: bellOpen ? "#06b6d4" : "#64748b" }}
                        onMouseEnter={e => {
                            e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                            if (!bellOpen) e.currentTarget.style.color = "#94a3b8";
                        }}
                        onMouseLeave={e => {
                            e.currentTarget.style.background = "transparent";
                            if (!bellOpen) e.currentTarget.style.color = "#64748b";
                        }}
                    >
                        <Bell size={17} />
                        {unreadCount > 0 && (
                            <span
                                className="absolute top-1 right-1 min-w-[16px] h-4 px-0.5 text-white text-[9px] font-bold rounded-full flex items-center justify-center leading-none"
                                style={{ background: "#ef4444" }}
                            >
                                {unreadCount > 9 ? "9+" : unreadCount}
                            </span>
                        )}
                    </button>

                    {bellOpen && (
                        <NotificationPanel onClose={() => setBellOpen(false)} />
                    )}
                </div>

                {/* Divider */}
                <div
                    className="w-px h-5 mx-1 shrink-0"
                    style={{ background: "rgba(255,255,255,0.08)" }}
                />

                {/* Profile avatar + dropdown */}
                <div ref={profileRef} className="relative">
                    <button
                        onClick={() => {
                            setProfileOpen(p => !p);
                            setBellOpen(false);
                        }}
                        className="flex items-center gap-2 px-2 py-1.5 rounded-lg transition-all duration-150"
                        style={profileOpen ? {
                            background: "rgba(255,255,255,0.07)",
                        } : {}}
                        onMouseEnter={e => {
                            e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                        }}
                        onMouseLeave={e => {
                            if (!profileOpen) e.currentTarget.style.background = "transparent";
                        }}
                    >
                        {/* Avatar */}
                        <div
                            className="w-7 h-7 rounded-full shrink-0 flex items-center justify-center text-xs font-bold text-white"
                            style={{
                                background: "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                boxShadow: "0 0 8px rgba(6,182,212,0.3)",
                            }}
                        >
                            {myName.charAt(0).toUpperCase()}
                        </div>
                        <div className="hidden lg:block text-left min-w-0">
                            <div className="text-xs font-semibold text-white leading-none truncate max-w-[90px]">
                                {myName}
                            </div>
                            {myRole === "admin" && (
                                <div
                                    className="text-[9px] mt-0.5 font-bold uppercase tracking-wider"
                                    style={{ color: "#a78bfa" }}
                                >
                                    Admin
                                </div>
                            )}
                        </div>
                        <ChevronDown
                            size={11}
                            className="hidden lg:block transition-transform duration-200"
                            style={{
                                color: "#475569",
                                transform: profileOpen ? "rotate(180deg)" : "rotate(0deg)",
                            }}
                        />
                    </button>

                    {/* Dropdown */}
                    {profileOpen && (
                        <div
                            className="absolute right-0 top-11 z-50 w-60 rounded-xl overflow-hidden shadow-2xl"
                            style={{
                                background: "#111827",
                                border: "1px solid rgba(255,255,255,0.1)",
                                boxShadow: "0 20px 50px rgba(0,0,0,0.5)",
                            }}
                        >
                            {/* Profile header */}
                            <div
                                className="px-4 py-4"
                                style={{ borderBottom: "1px solid rgba(255,255,255,0.07)" }}
                            >
                                <div className="flex items-center gap-3">
                                    <div
                                        className="w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold text-white shrink-0"
                                        style={{
                                            background: "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                            boxShadow: "0 0 12px rgba(6,182,212,0.3)",
                                        }}
                                    >
                                        {myName.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="min-w-0">
                                        <div className="text-sm font-semibold text-white truncate">
                                            {myName}
                                        </div>
                                        {myEmail && (
                                            <div className="text-xs text-slate-500 truncate mt-0.5">
                                                {myEmail}
                                            </div>
                                        )}
                                        <div className="flex items-center gap-1 mt-1">
                                            <span
                                                className="w-1.5 h-1.5 rounded-full"
                                                style={{ background: "#22c55e" }}
                                            />
                                            <span className="text-[10px] text-slate-500">
                                                {myRole === "admin" ? "Administrator" : "Active · Anonymous"}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            {/* Actions */}
                            <div className="py-1.5">
                                <button
                                    onClick={logout}
                                    className="w-full flex items-center gap-3 px-4 py-2.5 text-sm transition-colors"
                                    style={{ color: "#f87171" }}
                                    onMouseEnter={e => (e.currentTarget.style.background = "rgba(239,68,68,0.08)")}
                                    onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
                                >
                                    <LogOut size={14} />
                                    Sign out
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </header>
    );
}
