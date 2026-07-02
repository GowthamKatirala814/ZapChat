import { useState, useEffect } from "react";
import Sidebar from "../../components/Sidebar";
import TopNav from "../../components/TopNav";
import OnlineUsers from "../../components/OnlineUsers";
import ChatWindow from "../../components/ChatWindow";
import AdminDashboard from "./AdminDashboard";
import { Hash, Menu, Users } from "lucide-react";
import { subscribeToPush } from "../../api/notificationApi";
import { useTheme } from "../../context/ThemeContext";

// ── Room metadata ────────────────────────────────────────────────────────────
const ROOM_SUBTITLES: Record<string, string> = {
    "General Chat": "Company-wide announcements & discussions",
    "HR Issues":    "Human resources & policy discussions",
    "Hyderabad":    "Hyderabad office channel",
    "Bangalore":    "Bangalore office channel",
};

// ── Main Dashboard ───────────────────────────────────────────────────────────
export default function DashboardPage() {
    const { isDark } = useTheme();
    const [selectedRoom, setSelectedRoom] = useState<string | null>(null);
    const [sidebarOpen, setSidebarOpen] = useState(false);
    const [membersOpen, setMembersOpen] = useState(false);

    // Close sidebar/members on resize to desktop
    useEffect(() => {
        const onResize = () => {
            if (window.innerWidth >= 768) {
                setSidebarOpen(false);
                setMembersOpen(false);
            }
        };
        window.addEventListener("resize", onResize);
        return () => window.removeEventListener("resize", onResize);
    }, []);



    // Register Push Notifications
    useEffect(() => {
        const registerPush = async () => {
            const currentUserId = localStorage.getItem("userId");
            if (!currentUserId || !("serviceWorker" in navigator) || !("PushManager" in window)) return;
            try {
                const permission = await Notification.requestPermission();
                if (permission === "granted") {
                    const registration = await navigator.serviceWorker.register("/sw.js");
                    const vapidPublicKey = "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuB-3qOXGIV-kfO8wUo-iYcb9M";
                    const convertedVapidKey = urlBase64ToUint8Array(vapidPublicKey);
                    const subscription = await registration.pushManager.subscribe({
                        userVisibleOnly: true,
                        applicationServerKey: convertedVapidKey
                    });
                    const jsonSub = subscription.toJSON();
                    if (jsonSub.endpoint && jsonSub.keys?.p256dh && jsonSub.keys?.auth) {
                        await subscribeToPush({
                            userId: currentUserId,
                            endpoint: jsonSub.endpoint,
                            p256dh: jsonSub.keys.p256dh,
                            auth: jsonSub.keys.auth
                        });
                    }
                }
            } catch (err) {
                console.error("Failed to register push notifications", err);
            }
        };
        registerPush();
    }, []);

    function urlBase64ToUint8Array(base64String: string) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) outputArray[i] = rawData.charCodeAt(i);
        return outputArray;
    }

    const role = localStorage.getItem("role");
    if (role === "admin") return <AdminDashboard />;

    const subtitle = selectedRoom ? (ROOM_SUBTITLES[selectedRoom] ?? "Group channel") : "";

    // Theme colors
    const pageBg       = isDark ? "#0c1220" : "#f0f9ff";
    const headerBg     = isDark ? "#0f172a" : "#ffffff";
    const headerBorder = isDark ? "rgba(255,255,255,0.07)" : "#e2e8f0";
    const headerText   = isDark ? "#f1f5f9" : "#0f172a";
    const headerSub    = isDark ? "#64748b" : "#64748b";
    const channelBg    = isDark ? "#0c1426" : "#f0f9ff";

    return (
        <div className="h-screen flex flex-col overflow-hidden" style={{ background: pageBg }}>
            {/* ── Top Navigation Bar ─────────────────────────────── */}
            <div className="relative z-40">
                <TopNav onMenuClick={() => setSidebarOpen(v => !v)} />
            </div>

            {/* ── Mobile Sidebar Backdrop ──────────────────────── */}
            {sidebarOpen && (
                <div
                    className="fixed inset-0 z-40 md:hidden drawer-backdrop"
                    onClick={() => setSidebarOpen(false)}
                />
            )}

            {/* ── Members Panel Backdrop (mobile) ──────────────── */}
            {membersOpen && selectedRoom && (
                <div
                    className="fixed inset-0 z-40 md:hidden drawer-backdrop"
                    onClick={() => setMembersOpen(false)}
                />
            )}

            <div className="flex-1 flex overflow-hidden relative">
                {/* ── Left Sidebar ─────────────────────────────── */}
                {/* Desktop: always visible | Mobile: slide-in drawer */}
                <div
                    className={`
                        sidebar-drawer
                        fixed md:relative inset-y-0 left-0 z-50 md:z-auto
                        w-64 shrink-0 flex flex-col overflow-hidden
                        ${sidebarOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"}
                    `}
                    style={{ top: "56px", height: "calc(100% - 56px)" }}
                >
                    <Sidebar
                        selectedRoom={selectedRoom}
                        setSelectedRoom={(room: string | null) => {
                            setSelectedRoom(room);
                            setSidebarOpen(false); // auto-close on mobile
                        }}
                    />
                </div>

                {/* ── Center: Welcome screen or Channel Chat ──────── */}
                {selectedRoom === null ? (
                    <div
                        className="flex-1 flex items-center justify-center overflow-hidden"
                        style={{
                            borderLeft: `1px solid ${headerBorder}`,
                            borderRight: `1px solid ${headerBorder}`,
                            background: isDark
                                ? "linear-gradient(160deg, #0c1426 0%, #0f1e3d 50%, #0c1426 100%)"
                                : "linear-gradient(160deg, #f0f9ff 0%, #e0f2fe 50%, #f8fafc 100%)",
                        }}
                    >
                        <div className="flex flex-col items-center text-center px-6 sm:px-8 max-w-lg w-full">
                            <div
                                className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl flex items-center justify-center mb-4 sm:mb-5 shadow-lg"
                                style={{
                                    background: "linear-gradient(135deg, #0EA5E9, #38BDF8)",
                                    boxShadow: "0 8px 24px rgba(14,165,233,0.25)",
                                }}
                            >
                                <span className="text-2xl sm:text-3xl font-black text-white select-none">Z</span>
                            </div>
                            <h1 className="text-xl sm:text-2xl font-bold mb-2" style={{ color: headerText }}>
                                Welcome to{" "}
                                <span style={{ color: "#0EA5E9" }}>ZapChat</span>
                            </h1>
                            <p className="text-sm leading-relaxed mb-6 sm:mb-8 max-w-xs" style={{ color: headerSub }}>
                                Professional workspace communication platform. Stay connected with your team anonymously.
                            </p>
                            <div className="flex flex-wrap items-center justify-center gap-2 sm:gap-3">
                                {[
                                    { icon: "💬", label: "Join discussions" },
                                    { icon: "📊", label: "Participate in polls" },
                                    { icon: "👥", label: "Connect with colleagues" },
                                    { icon: "🔔", label: "Stay updated" },
                                ].map(({ icon, label }) => (
                                    <div
                                        key={label}
                                        className="flex items-center gap-2 px-3 py-2 sm:px-4 sm:py-2.5 rounded-full text-xs sm:text-sm font-medium"
                                        style={{
                                            background: isDark ? "rgba(14,165,233,0.1)" : "rgba(255,255,255,0.85)",
                                            border: `1px solid ${isDark ? "rgba(14,165,233,0.2)" : "rgba(14,165,233,0.2)"}`,
                                            color: isDark ? "#94a3b8" : "#334155",
                                        }}
                                    >
                                        <span className="text-base">{icon}</span>
                                        {label}
                                    </div>
                                ))}
                            </div>
                            {/* Mobile: prompt to open sidebar */}
                            <button
                                className="md:hidden mt-6 flex items-center gap-2 px-5 py-3 rounded-xl text-sm font-semibold text-white"
                                style={{ background: "linear-gradient(135deg, #0ea5e9, #06b6d4)" }}
                                onClick={() => setSidebarOpen(true)}
                            >
                                <Menu size={16} />
                                Open Channels
                            </button>
                        </div>
                    </div>
                ) : (
                    <div
                        className="flex-1 flex flex-col min-w-0 overflow-hidden"
                        style={{ borderLeft: `1px solid ${headerBorder}`, borderRight: `1px solid ${headerBorder}` }}
                    >
                        {/* Channel header */}
                        <div
                            className="h-12 shrink-0 flex items-center px-3 sm:px-4 gap-2 sm:gap-3"
                            style={{ background: headerBg, borderBottom: `1px solid ${headerBorder}` }}
                        >
                            {/* Mobile back/menu */}
                            <button
                                className="md:hidden p-1.5 rounded-lg transition-colors"
                                style={{ color: headerSub }}
                                onClick={() => setSidebarOpen(true)}
                                aria-label="Open sidebar"
                            >
                                <Menu size={16} />
                            </button>

                            <div
                                className="w-6 h-6 rounded-md flex items-center justify-center shrink-0"
                                style={{ background: isDark ? "rgba(14,165,233,0.15)" : "#EFF6FF" }}
                            >
                                <Hash size={12} style={{ color: "#0EA5E9" }} />
                            </div>
                            <div className="min-w-0 flex-1">
                                <div className="text-sm font-semibold leading-none truncate" style={{ color: headerText }}>
                                    {selectedRoom}
                                </div>
                                {subtitle && (
                                    <div className="text-[11px] mt-0.5 hidden sm:block truncate" style={{ color: headerSub }}>
                                        {subtitle}
                                    </div>
                                )}
                            </div>

                            {/* Members toggle (mobile) */}
                            <button
                                className="xl:hidden p-1.5 rounded-lg transition-colors"
                                style={{ color: headerSub }}
                                onClick={() => setMembersOpen(v => !v)}
                                aria-label="Toggle members"
                                title="Members"
                            >
                                <Users size={16} />
                            </button>

                        </div>

                        {/* Chat fills the remaining height */}
                        <div className="flex-1 overflow-hidden" style={{ background: channelBg }}>
                            <ChatWindow roomName={selectedRoom} />
                        </div>
                    </div>
                )}

                {/* ── Right Panel — Members / Room Info ──────────── */}
                {selectedRoom !== null && (
                    <>
                        {/* Desktop: always visible */}
                        <div className="w-64 shrink-0 overflow-hidden hidden xl:block">
                            <OnlineUsers roomName={selectedRoom} />
                        </div>

                        {/* Mobile/Tablet: slide-in drawer from right */}
                        {membersOpen && (
                            <div
                                className="sidebar-drawer fixed right-0 z-50 xl:hidden overflow-hidden"
                                style={{
                                    top: "56px",
                                    width: "280px",
                                    height: "calc(100vh - 56px)",
                                    transform: membersOpen ? "translateX(0)" : "translateX(100%)",
                                }}
                            >
                                <OnlineUsers roomName={selectedRoom} />
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}
