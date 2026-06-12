import { useState } from "react";
import Sidebar from "../../components/Sidebar";
import TopNav from "../../components/TopNav";
import OnlineUsers from "../../components/OnlineUsers";
import ChatWindow from "../../components/ChatWindow";
import AdminDashboard from "./AdminDashboard";
import { Hash } from "lucide-react";

// ── Room metadata ────────────────────────────────────────────────────────────
const ROOM_SUBTITLES: Record<string, string> = {
    "General Chat": "Company-wide announcements & discussions",
    "HR Issues":    "Human resources & policy discussions",
    "Hyderabad":    "Hyderabad office channel",
    "Bangalore":    "Bangalore office channel",
};

// ── Main Dashboard ───────────────────────────────────────────────────────────
export default function DashboardPage() {
    // null = show welcome screen; string = active room selected by user
    const [selectedRoom, setSelectedRoom] = useState<string | null>(null);

    // Role-based routing: admin → AdminDashboard
    const role = localStorage.getItem("role");
    if (role === "admin") {
        return <AdminDashboard />;
    }

    const subtitle = selectedRoom ? (ROOM_SUBTITLES[selectedRoom] ?? "Group channel") : "";

    return (
        <div className="h-screen flex flex-col overflow-hidden" style={{ background: "#F8FAFC" }}>
            {/* ── Top Navigation Bar ─────────────────────────────── */}
            <TopNav />

            <div className="flex-1 flex overflow-hidden">
                {/* ── Left Sidebar ───────────────────────────────── */}
                <div className="w-64 shrink-0 flex flex-col overflow-hidden">
                    <Sidebar
                        selectedRoom={selectedRoom}
                        setSelectedRoom={setSelectedRoom}
                    />
                </div>

                {/* ── Center: Welcome screen or Channel Chat ──────── */}
                {selectedRoom === null ? (
                    // ── Welcome Screen ──────────────────────────────
                    <div
                        className="flex-1 flex items-center justify-center overflow-hidden"
                        style={{
                            borderLeft: "1px solid #E2E8F0",
                            borderRight: "1px solid #E2E8F0",
                            background: "linear-gradient(160deg, #f0f7ff 0%, #e8f4fb 50%, #f5faff 100%)",
                        }}
                    >
                        <div className="flex flex-col items-center text-center px-8 max-w-lg">
                            {/* Logo icon */}
                            <div
                                className="w-16 h-16 rounded-2xl flex items-center justify-center mb-5 shadow-lg"
                                style={{
                                    background: "linear-gradient(135deg, #0EA5E9, #38BDF8)",
                                    boxShadow: "0 8px 24px rgba(14,165,233,0.25)",
                                }}
                            >
                                <span className="text-3xl font-black text-white select-none">Z</span>
                            </div>

                            {/* Heading */}
                            <h1 className="text-2xl font-bold text-slate-800 mb-2">
                                Welcome to{" "}
                                <span style={{ color: "#0EA5E9" }}>ZapCom</span>
                            </h1>

                            {/* Subtitle */}
                            <p className="text-slate-500 text-sm leading-relaxed mb-8 max-w-xs">
                                Professional workspace communication platform. Stay connected with
                                your team anonymously.
                            </p>

                            {/* Feature pills */}
                            <div className="flex flex-wrap items-center justify-center gap-3">
                                {[
                                    { icon: "💬", label: "Join discussions" },
                                    { icon: "📊", label: "Participate in polls" },
                                    { icon: "👥", label: "Connect with colleagues" },
                                    { icon: "🔔", label: "Stay updated" },
                                ].map(({ icon, label }) => (
                                    <div
                                        key={label}
                                        className="flex items-center gap-2 px-4 py-2.5 rounded-full text-sm font-medium"
                                        style={{
                                            background: "rgba(255,255,255,0.85)",
                                            border: "1px solid rgba(14,165,233,0.2)",
                                            color: "#334155",
                                            backdropFilter: "blur(8px)",
                                            boxShadow: "0 2px 8px rgba(14,165,233,0.06)",
                                        }}
                                    >
                                        <span className="text-base">{icon}</span>
                                        {label}
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                ) : (
                    // ── Active Room: Channel Header + Chat ──────────
                    <div
                        className="flex-1 flex flex-col min-w-0 overflow-hidden"
                        style={{ borderLeft: "1px solid #E2E8F0", borderRight: "1px solid #E2E8F0" }}
                    >
                        {/* Channel header */}
                        <div
                            className="h-12 shrink-0 flex items-center px-4 gap-3"
                            style={{ background: "#FFFFFF", borderBottom: "1px solid #E2E8F0" }}
                        >
                            <div
                                className="w-6 h-6 rounded-md flex items-center justify-center shrink-0"
                                style={{ background: "#EFF6FF" }}
                            >
                                <Hash size={12} style={{ color: "#0EA5E9" }} />
                            </div>
                            <div className="min-w-0">
                                <div className="text-sm font-semibold text-slate-900 leading-none">
                                    {selectedRoom}
                                </div>
                                {subtitle && (
                                    <div className="text-[11px] text-slate-500 mt-0.5">{subtitle}</div>
                                )}
                            </div>
                        </div>

                        {/* Chat fills the remaining height — untouched */}
                        <div className="flex-1 overflow-hidden">
                            <ChatWindow roomName={selectedRoom} />
                        </div>
                    </div>
                )}

                {/* ── Right Panel — Members / Room Info ──────────── */}
                {selectedRoom !== null && (
                    <div className="w-64 shrink-0 overflow-hidden hidden xl:block">
                        <OnlineUsers roomName={selectedRoom} />
                    </div>
                )}
            </div>
        </div>
    );
}