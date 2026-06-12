import { useEffect, useRef, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useNavigate, useLocation, Link } from "react-router-dom";
import {
    Bell, ChevronDown, LogOut, User,
    LayoutDashboard, BarChart3,
} from "lucide-react";
import type { RootState, AppDispatch } from "../store/store";
import { setNotifications, addNotification } from "../store/notificationSlice";
import { getNotifications } from "../api/notificationApi";
import { getNotificationConnection } from "../hubs/notificationHub";
import { connection as chatHubConnection } from "../hubs/chatHub";
import { logout, getAnonymousName } from "../utils/auth";
import NotificationPanel from "./NotificationPanel";
import type { Notification } from "../types/Notification";

export default function TopNav() {
    const dispatch   = useDispatch<AppDispatch>();
    const navigate   = useNavigate();
    const location   = useLocation();
    const [bellOpen, setBellOpen]       = useState(false);
    const [profileOpen, setProfileOpen] = useState(false);
    const profileRef = useRef<HTMLDivElement>(null);
    const bellRef    = useRef<HTMLDivElement>(null);

    const unreadCount = useSelector((s: RootState) => s.notifications.unreadCount);
    const userId  = localStorage.getItem("userId") ?? "";
    const myName  = getAnonymousName();
    const myEmail = localStorage.getItem("email") ?? "";
    const myRole  = localStorage.getItem("role") ?? "user";

    // Boot notification hub once per session
    useEffect(() => {
        if (!userId) return;
        getNotifications(userId)
            .then(data => dispatch(setNotifications(data)))
            .catch(() => {});
        const conn = getNotificationConnection();
        conn.off("ReceiveNotification");
        conn.on("ReceiveNotification", (n: Notification) => { dispatch(addNotification(n)); });
        if (conn.state === "Disconnected") conn.start().catch(console.error);
        chatHubConnection.off("GlobalNotification");
        chatHubConnection.on("GlobalNotification", (n: Notification) => { dispatch(addNotification(n)); });
        return () => {
            conn.off("ReceiveNotification");
            chatHubConnection.off("GlobalNotification");
        };
    }, [dispatch, userId]);

    // Close dropdowns on outside click
    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (profileRef.current && !profileRef.current.contains(e.target as Node))
                setProfileOpen(false);
            if (bellRef.current && !bellRef.current.contains(e.target as Node))
                setBellOpen(false);
        };
        document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, []);

    const isActive = (path: string) => location.pathname === path;

    const NAV_ITEMS = [
        { label: "Workspace", path: "/dashboard", icon: LayoutDashboard },
        { label: "Polls",     path: "/polls",     icon: BarChart3 },
        { label: "Profile",   path: "/profile",   icon: User },
    ];

    return (
        <header
            className="h-14 shrink-0 flex items-center justify-between px-5 z-40"
            style={{
                background: "#FFFFFF",
                borderBottom: "1px solid #E2E8F0",
                boxShadow: "0 1px 3px rgba(0,0,0,0.04)",
            }}
        >
            {/* ── LEFT: Logo + Navigation ─────────────────────────────── */}
            <div className="flex items-center gap-6">
                <Link to="/dashboard" className="flex items-center gap-2.5 shrink-0">
                    <div
                        className="w-8 h-8 rounded-xl flex items-center justify-center shadow-md"
                        style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                    >
                        <span className="text-sm font-black text-white select-none">Z</span>
                    </div>
                    <span className="text-[15px] font-bold text-slate-900 leading-none select-none">
                        Zap<span style={{ color: "#0EA5E9" }}>Com</span>
                    </span>
                </Link>

                <nav className="hidden md:flex items-center gap-0.5">
                    {NAV_ITEMS.map(({ label, path, icon: Icon }) => {
                        const active = isActive(path);
                        return (
                            <button
                                key={path}
                                onClick={() => navigate(path)}
                                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg text-sm font-medium transition-all duration-150"
                                style={active ? {
                                    background: "#EFF6FF",
                                    color: "#0284C7",
                                } : {
                                    color: "#475569",
                                }}
                                onMouseEnter={e => {
                                    if (!active) {
                                        e.currentTarget.style.background = "#F1F5F9";
                                        e.currentTarget.style.color = "#0F172A";
                                    }
                                }}
                                onMouseLeave={e => {
                                    if (!active) {
                                        e.currentTarget.style.background = "transparent";
                                        e.currentTarget.style.color = "#475569";
                                    }
                                }}
                            >
                                <Icon size={14} style={{ color: active ? "#0EA5E9" : "#94A3B8" }} />
                                {label}
                            </button>
                        );
                    })}
                </nav>
            </div>

            {/* ── RIGHT: Bell + Profile ────────────────────────────────── */}
            <div className="flex items-center gap-1.5 shrink-0">

                {/* Notification Bell */}
                <div ref={bellRef} className="relative">
                    <button
                        id="notification-bell"
                        onClick={() => { setBellOpen(p => !p); setProfileOpen(false); }}
                        className="relative p-2 rounded-lg transition-colors"
                        style={{ color: bellOpen ? "#0EA5E9" : "#64748B" }}
                        onMouseEnter={e => {
                            e.currentTarget.style.background = "#F1F5F9";
                            if (!bellOpen) e.currentTarget.style.color = "#334155";
                        }}
                        onMouseLeave={e => {
                            e.currentTarget.style.background = "transparent";
                            if (!bellOpen) e.currentTarget.style.color = "#64748B";
                        }}
                    >
                        <Bell size={17} />
                        {unreadCount > 0 && (
                            <span
                                className="absolute top-1 right-1 min-w-[15px] h-[15px] px-0.5 text-white text-[9px] font-bold rounded-full flex items-center justify-center leading-none"
                                style={{ background: "#EF4444" }}
                            >
                                {unreadCount > 9 ? "9+" : unreadCount}
                            </span>
                        )}
                    </button>
                    {bellOpen && <NotificationPanel onClose={() => setBellOpen(false)} />}
                </div>

                {/* Divider */}
                <div className="w-px h-5 mx-0.5" style={{ background: "#E2E8F0" }} />

                {/* Profile Avatar + Dropdown */}
                <div ref={profileRef} className="relative">
                    <button
                        onClick={() => { setProfileOpen(p => !p); setBellOpen(false); }}
                        className="flex items-center gap-2 px-2.5 py-1.5 rounded-lg transition-all duration-150"
                        style={profileOpen ? { background: "#EFF6FF" } : {}}
                        onMouseEnter={e => { if (!profileOpen) e.currentTarget.style.background = "#F1F5F9"; }}
                        onMouseLeave={e => { if (!profileOpen) e.currentTarget.style.background = "transparent"; }}
                    >
                        <div
                            className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold text-white shrink-0"
                            style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                        >
                            {myName.charAt(0).toUpperCase()}
                        </div>
                        <div className="hidden lg:block text-left min-w-0">
                            <div className="text-xs font-semibold text-slate-800 leading-none truncate max-w-[90px]">
                                {myName}
                            </div>
                            {myRole === "admin" && (
                                <div className="text-[9px] font-bold uppercase tracking-wider mt-0.5"
                                    style={{ color: "#0EA5E9" }}>
                                    Admin
                                </div>
                            )}
                        </div>
                        <ChevronDown
                            size={11}
                            className="hidden lg:block transition-transform duration-200"
                            style={{
                                color: "#94A3B8",
                                transform: profileOpen ? "rotate(180deg)" : "rotate(0deg)",
                            }}
                        />
                    </button>

                    {profileOpen && (
                        <div
                            className="absolute right-0 top-11 z-50 w-60 rounded-xl overflow-hidden"
                            style={{
                                background: "#FFFFFF",
                                border: "1px solid #E2E8F0",
                                boxShadow: "0 10px 40px rgba(0,0,0,0.12)",
                            }}
                        >
                            {/* Profile header */}
                            <div className="px-4 py-4" style={{ borderBottom: "1px solid #F1F5F9" }}>
                                <div className="flex items-center gap-3">
                                    <div
                                        className="w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold text-white shrink-0"
                                        style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                                    >
                                        {myName.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="min-w-0">
                                        <div className="text-sm font-semibold text-slate-900 truncate">{myName}</div>
                                        {myEmail && (
                                            <div className="text-xs text-slate-500 truncate mt-0.5">{myEmail}</div>
                                        )}
                                        <div className="flex items-center gap-1 mt-1">
                                            <span className="w-1.5 h-1.5 rounded-full" style={{ background: "#22C55E" }} />
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
                                    onClick={() => { navigate("/profile"); setProfileOpen(false); }}
                                    className="w-full flex items-center gap-3 px-4 py-2.5 text-sm transition-colors"
                                    style={{ color: "#334155" }}
                                    onMouseEnter={e => (e.currentTarget.style.background = "#F8FAFC")}
                                    onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
                                >
                                    <User size={14} style={{ color: "#94A3B8" }} />
                                    View Profile
                                </button>
                                <div className="mx-4 my-1 h-px" style={{ background: "#F1F5F9" }} />
                                <button
                                    onClick={logout}
                                    className="w-full flex items-center gap-3 px-4 py-2.5 text-sm transition-colors"
                                    style={{ color: "#EF4444" }}
                                    onMouseEnter={e => (e.currentTarget.style.background = "#FEF2F2")}
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
