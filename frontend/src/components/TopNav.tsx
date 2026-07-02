import { useEffect, useRef, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useNavigate, useLocation, Link } from "react-router-dom";
import {
    Bell, ChevronDown, LogOut, User,
    LayoutDashboard, BarChart3, Menu, X, Sun, Moon,
} from "lucide-react";
import type { RootState, AppDispatch } from "../store/store";
import { setNotifications, addNotification, removeNotification } from "../store/notificationSlice";
import { getNotifications } from "../api/notificationApi";
import { getNotificationConnection } from "../hubs/notificationHub";
import { connection as chatHubConnection } from "../hubs/chatHub";
import { logout, getAnonymousName } from "../utils/auth";
import NotificationPanel from "./NotificationPanel";
import type { Notification } from "../types/Notification";
import { useTheme } from "../context/ThemeContext";

interface Props {
    onMenuClick?: () => void;
}

export default function TopNav({ onMenuClick }: Props) {
    const dispatch   = useDispatch<AppDispatch>();
    const navigate   = useNavigate();
    const location   = useLocation();
    const { isDark, toggleTheme } = useTheme();

    const [bellOpen, setBellOpen]       = useState(false);
    const [profileOpen, setProfileOpen] = useState(false);
    const [mobileNavOpen, setMobileNavOpen] = useState(false);

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
        conn.off("NotificationDeleted");
        conn.on("ReceiveNotification", (n: Notification) => { dispatch(addNotification(n)); });
        conn.on("NotificationDeleted", (data: { id: string }) => { dispatch(removeNotification(data.id)); });
        
        if (conn.state === "Disconnected") conn.start().catch(console.error);
        
        chatHubConnection.off("GlobalNotification");
        chatHubConnection.on("GlobalNotification", (n: Notification) => { dispatch(addNotification(n)); });
        
        return () => {
            conn.off("ReceiveNotification");
            conn.off("NotificationDeleted");
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

    // Theme-aware styles
    const navBg     = isDark ? "#0f172a" : "#ffffff";
    const navBorder = isDark ? "rgba(255,255,255,0.07)" : "#e2e8f0";
    const navText   = isDark ? "#f1f5f9" : "#0f172a";
    const navSub    = isDark ? "#94a3b8" : "#475569";
    const hoverBg   = isDark ? "rgba(255,255,255,0.06)" : "#f1f5f9";
    const activeBg  = isDark ? "rgba(14,165,233,0.15)" : "#eff6ff";
    const activeText = isDark ? "#38bdf8" : "#0284c7";
    const dropdownBg = isDark ? "#0f172a" : "#ffffff";
    const dropdownBorder = isDark ? "rgba(255,255,255,0.08)" : "#e2e8f0";
    const mobileBg  = isDark ? "#0c1426" : "#f8fafc";

    return (
        <>
            <header
                className="h-14 shrink-0 flex items-center justify-between px-4 sm:px-5 z-40 relative"
                style={{
                    background: navBg,
                    borderBottom: `1px solid ${navBorder}`,
                    boxShadow: isDark ? "0 1px 3px rgba(0,0,0,0.3)" : "0 1px 3px rgba(0,0,0,0.04)",
                }}
            >
                {/* ── LEFT: Hamburger (mobile) + Logo + Nav ─────────────── */}
                <div className="flex items-center gap-2 sm:gap-4">
                    {/* Hamburger for mobile sidebar */}
                    <button
                        onClick={onMenuClick}
                        className="md:hidden p-2 rounded-lg transition-colors"
                        style={{ color: navSub }}
                        aria-label="Open menu"
                        id="topnav-hamburger"
                    >
                        <Menu size={20} />
                    </button>

                    <Link to="/dashboard" className="flex items-center gap-2 shrink-0">
                        <div
                            className="w-8 h-8 rounded-xl flex items-center justify-center shadow-md"
                            style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                        >
                            <span className="text-sm font-black text-white select-none">Z</span>
                        </div>
                        <span className="text-[15px] font-bold leading-none select-none" style={{ color: navText }}>
                            Zap<span style={{ color: "#0EA5E9" }}>Chat</span>
                        </span>
                    </Link>

                    {/* Desktop nav */}
                    <nav className="hidden md:flex items-center gap-0.5">
                        {NAV_ITEMS.map(({ label, path, icon: Icon }) => {
                            const active = isActive(path);
                            return (
                                <button
                                    key={path}
                                    onClick={() => navigate(path)}
                                    className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg text-sm font-medium transition-all duration-150"
                                    style={active ? {
                                        background: activeBg,
                                        color: activeText,
                                    } : {
                                        color: navSub,
                                    }}
                                    onMouseEnter={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = hoverBg;
                                            e.currentTarget.style.color = navText;
                                        }
                                    }}
                                    onMouseLeave={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "transparent";
                                            e.currentTarget.style.color = navSub;
                                        }
                                    }}
                                >
                                    <Icon size={14} style={{ color: active ? "#0EA5E9" : navSub }} />
                                    {label}
                                </button>
                            );
                        })}
                    </nav>
                </div>

                {/* ── RIGHT: Theme Toggle + Bell + Profile ──────────── */}
                <div className="flex items-center gap-1 sm:gap-1.5 shrink-0">

                    {/* Theme Toggle */}
                    <button
                        onClick={toggleTheme}
                        id="theme-toggle"
                        className="p-2 rounded-lg transition-colors"
                        style={{ color: navSub }}
                        title={isDark ? "Switch to light mode" : "Switch to dark mode"}
                        onMouseEnter={e => { e.currentTarget.style.background = hoverBg; }}
                        onMouseLeave={e => { e.currentTarget.style.background = "transparent"; }}
                    >
                        {isDark ? <Sun size={16} /> : <Moon size={16} />}
                    </button>

                    {/* Notification Bell */}
                    <div ref={bellRef} className="relative">
                        <button
                            id="notification-bell"
                            onClick={() => { setBellOpen(p => !p); setProfileOpen(false); }}
                            className="relative p-2 rounded-lg transition-colors"
                            style={{ color: bellOpen ? "#0EA5E9" : navSub }}
                            onMouseEnter={e => {
                                e.currentTarget.style.background = hoverBg;
                                if (!bellOpen) e.currentTarget.style.color = navText;
                            }}
                            onMouseLeave={e => {
                                e.currentTarget.style.background = "transparent";
                                if (!bellOpen) e.currentTarget.style.color = navSub;
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
                    <div className="w-px h-5 mx-0.5 hidden sm:block" style={{ background: navBorder }} />

                    {/* Profile Avatar + Dropdown */}
                    <div ref={profileRef} className="relative">
                        <button
                            onClick={() => { setProfileOpen(p => !p); setBellOpen(false); }}
                            className="flex items-center gap-2 px-2 sm:px-2.5 py-1.5 rounded-lg transition-all duration-150"
                            style={profileOpen ? { background: activeBg } : {}}
                            onMouseEnter={e => { if (!profileOpen) e.currentTarget.style.background = hoverBg; }}
                            onMouseLeave={e => { if (!profileOpen) e.currentTarget.style.background = "transparent"; }}
                        >
                            <div
                                className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold text-white shrink-0"
                                style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                            >
                                {myName.charAt(0).toUpperCase()}
                            </div>
                            <div className="hidden lg:block text-left min-w-0">
                                <div className="text-xs font-semibold leading-none truncate max-w-[90px]" style={{ color: navText }}>
                                    {myName}
                                </div>
                                {myRole === "admin" && (
                                    <div className="text-[9px] font-bold uppercase tracking-wider mt-0.5" style={{ color: "#0EA5E9" }}>
                                        Admin
                                    </div>
                                )}
                            </div>
                            <ChevronDown
                                size={11}
                                className="hidden lg:block transition-transform duration-200"
                                style={{
                                    color: navSub,
                                    transform: profileOpen ? "rotate(180deg)" : "rotate(0deg)",
                                }}
                            />
                        </button>

                        {profileOpen && (
                            <div
                                className="absolute right-0 top-11 z-50 w-60 rounded-xl overflow-hidden"
                                style={{
                                    background: dropdownBg,
                                    border: `1px solid ${dropdownBorder}`,
                                    boxShadow: "0 10px 40px rgba(0,0,0,0.2)",
                                }}
                            >
                                {/* Profile header */}
                                <div className="px-4 py-4" style={{ borderBottom: `1px solid ${dropdownBorder}` }}>
                                    <div className="flex items-center gap-3">
                                        <div
                                            className="w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold text-white shrink-0"
                                            style={{ background: "linear-gradient(135deg, #0EA5E9, #38BDF8)" }}
                                        >
                                            {myName.charAt(0).toUpperCase()}
                                        </div>
                                        <div className="min-w-0">
                                            <div className="text-sm font-semibold truncate" style={{ color: navText }}>{myName}</div>
                                            {myEmail && (
                                                <div className="text-xs truncate mt-0.5" style={{ color: navSub }}>{myEmail}</div>
                                            )}
                                            <div className="flex items-center gap-1 mt-1">
                                                <span className="w-1.5 h-1.5 rounded-full" style={{ background: "#22C55E" }} />
                                                <span className="text-[10px]" style={{ color: navSub }}>
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
                                        style={{ color: navSub }}
                                        onMouseEnter={e => (e.currentTarget.style.background = hoverBg)}
                                        onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
                                    >
                                        <User size={14} style={{ color: navSub }} />
                                        View Profile
                                    </button>
                                    {/* Theme toggle in dropdown */}
                                    <button
                                        onClick={toggleTheme}
                                        className="w-full flex items-center gap-3 px-4 py-2.5 text-sm transition-colors"
                                        style={{ color: navSub }}
                                        onMouseEnter={e => (e.currentTarget.style.background = hoverBg)}
                                        onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
                                    >
                                        {isDark ? <Sun size={14} style={{ color: navSub }} /> : <Moon size={14} style={{ color: navSub }} />}
                                        {isDark ? "Light Mode" : "Dark Mode"}
                                    </button>
                                    <div className="mx-4 my-1 h-px" style={{ background: dropdownBorder }} />
                                    <button
                                        onClick={logout}
                                        className="w-full flex items-center gap-3 px-4 py-2.5 text-sm transition-colors"
                                        style={{ color: "#EF4444" }}
                                        onMouseEnter={e => (e.currentTarget.style.background = isDark ? "rgba(239,68,68,0.08)" : "#fef2f2")}
                                        onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
                                    >
                                        <LogOut size={14} />
                                        Sign out
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Mobile menu button */}
                    <button
                        onClick={() => setMobileNavOpen(v => !v)}
                        className="md:hidden p-2 rounded-lg transition-colors ml-1"
                        style={{ color: navSub }}
                        aria-label="Toggle navigation"
                        id="topnav-mobile-menu"
                    >
                        {mobileNavOpen ? <X size={18} /> : <ChevronDown size={18} />}
                    </button>
                </div>
            </header>

            {/* ── Mobile nav dropdown ─────────────────────────────────── */}
            {mobileNavOpen && (
                <>
                    <div
                        className="fixed inset-0 z-30 md:hidden"
                        onClick={() => setMobileNavOpen(false)}
                    />
                    <div
                        className="absolute left-0 right-0 z-40 md:hidden px-4 pb-4 pt-2 shadow-2xl"
                        style={{
                            background: mobileBg,
                            borderBottom: `1px solid ${navBorder}`,
                            top: "56px",
                        }}
                    >
                        {NAV_ITEMS.map(({ label, path, icon: Icon }) => {
                            const active = isActive(path);
                            return (
                                <button
                                    key={path}
                                    onClick={() => { navigate(path); setMobileNavOpen(false); }}
                                    className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-150 mb-1"
                                    style={active ? {
                                        background: activeBg,
                                        color: activeText,
                                    } : {
                                        color: navSub,
                                    }}
                                >
                                    <Icon size={16} style={{ color: active ? "#0EA5E9" : navSub }} />
                                    {label}
                                </button>
                            );
                        })}
                    </div>
                </>
            )}
        </>
    );
}
