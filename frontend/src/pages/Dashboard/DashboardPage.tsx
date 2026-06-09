import { useState } from "react";
import Sidebar from "../../components/Sidebar";
import AppHeader from "../../components/AppHeader";
import OnlineUsers from "../../components/OnlineUsers";
import ChatWindow from "../../components/ChatWindow";
import AdminDashboard from "./AdminDashboard";

// ── Room metadata ────────────────────────────────────────────────────────────
const ROOM_SUBTITLES: Record<string, string> = {
    "General Chat": "Company-wide announcements & discussions",
    "HR Issues":    "Human resources & policy discussions",
    "Hyderabad":    "Hyderabad office channel",
    "Bangalore":    "Bangalore office channel",
};

// ── Main Dashboard ───────────────────────────────────────────────────────────
export default function DashboardPage() {
    const [selectedRoom, setSelectedRoom] = useState("General Chat");

    // Role-based routing: admin → AdminDashboard
    const role = localStorage.getItem("role");
    if (role === "admin") {
        return <AdminDashboard />;
    }

    const subtitle = ROOM_SUBTITLES[selectedRoom] ?? "Group channel";

    return (
        <div
            className="h-screen text-white flex overflow-hidden"
            style={{ background: "#080e1a" }}
        >
            {/* ── Left Sidebar — 240px ───────────────────────────── */}
            <div className="w-60 shrink-0 flex flex-col overflow-hidden">
                <Sidebar
                    selectedRoom={selectedRoom}
                    setSelectedRoom={setSelectedRoom}
                />
            </div>

            {/* ── Center: Header + Chat ──────────────────────────── */}
            <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
                <AppHeader
                    title={selectedRoom}
                    subtitle={subtitle}
                />

                {/* Chat fills the remaining height */}
                <div className="flex-1 overflow-hidden">
                    <ChatWindow roomName={selectedRoom} />
                </div>
            </div>

            {/* ── Right Panel — Room Info / Members — 220px ─────── */}
            <div className="w-52 shrink-0 overflow-hidden hidden lg:block">
                <OnlineUsers roomName={selectedRoom} />
            </div>
        </div>
    );
}