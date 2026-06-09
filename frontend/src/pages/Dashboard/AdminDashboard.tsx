import { useState } from "react";
import {
    LayoutDashboard,
    Users,
    MessageSquare,
    Shield,
    BarChart3,
    Settings,
    LogOut,
    Hash,
    TrendingUp,
    AlertTriangle,
    ChevronRight,
    Activity,
} from "lucide-react";
import { logout, getAnonymousName } from "../../utils/auth";

// ── Types ──────────────────────────────────────────────────────────────────────
type AdminSection =
    | "overview"
    | "users"
    | "rooms"
    | "moderation"
    | "analytics"
    | "settings";

// ── Nav items ──────────────────────────────────────────────────────────────────
const NAV_ITEMS: {
    id: AdminSection;
    label: string;
    icon: typeof LayoutDashboard;
    badge?: string;
}[] = [
    { id: "overview",   label: "Overview",     icon: LayoutDashboard },
    { id: "users",      label: "User Mgmt",    icon: Users },
    { id: "rooms",      label: "Room Mgmt",    icon: Hash },
    { id: "moderation", label: "Moderation",   icon: Shield, badge: "!" },
    { id: "analytics",  label: "Analytics",    icon: BarChart3 },
    { id: "settings",   label: "Settings",     icon: Settings },
];

// ── Shared UI helpers ─────────────────────────────────────────────────────────
function PendingBadge() {
    return (
        <span
            className="inline-flex items-center gap-1 text-[10px] font-semibold px-2 py-0.5 rounded-full"
            style={{
                background: "rgba(251,191,36,0.12)",
                border: "1px solid rgba(251,191,36,0.25)",
                color: "#fbbf24",
            }}
        >
            <Activity size={8} />
            Waiting for backend integration
        </span>
    );
}

function SectionCard({
    title,
    icon: Icon,
    children,
}: {
    title: string;
    icon: typeof LayoutDashboard;
    children: React.ReactNode;
}) {
    return (
        <div
            className="rounded-2xl p-6"
            style={{
                background: "rgba(255,255,255,0.03)",
                border: "1px solid rgba(255,255,255,0.07)",
            }}
        >
            <div className="flex items-center gap-2.5 mb-5">
                <div
                    className="w-8 h-8 rounded-lg flex items-center justify-center"
                    style={{ background: "rgba(139,92,246,0.15)" }}
                >
                    <Icon size={15} style={{ color: "#a78bfa" }} />
                </div>
                <h3 className="text-sm font-bold text-white">{title}</h3>
            </div>
            {children}
        </div>
    );
}

function PlaceholderTable({ columns, rows = 4 }: { columns: string[]; rows?: number }) {
    return (
        <div>
            {/* Header */}
            <div
                className="grid gap-3 px-4 py-2.5 rounded-t-xl text-[10px] font-bold uppercase tracking-wider"
                style={{
                    gridTemplateColumns: columns.map(() => "1fr").join(" "),
                    background: "rgba(255,255,255,0.04)",
                    color: "#475569",
                }}
            >
                {columns.map(c => <span key={c}>{c}</span>)}
            </div>
            {/* Skeleton rows */}
            {Array.from({ length: rows }).map((_, i) => (
                <div
                    key={i}
                    className="grid gap-3 px-4 py-3"
                    style={{
                        gridTemplateColumns: columns.map(() => "1fr").join(" "),
                        borderBottom: "1px solid rgba(255,255,255,0.04)",
                    }}
                >
                    {columns.map(c => (
                        <div
                            key={c}
                            className="h-3 rounded-full"
                            style={{
                                background: "rgba(255,255,255,0.06)",
                                width: `${40 + (i * 13 + c.length * 7) % 40}%`,
                            }}
                        />
                    ))}
                </div>
            ))}
            {/* Footer note */}
            <div
                className="px-4 py-3 rounded-b-xl flex items-center justify-center"
                style={{ background: "rgba(255,255,255,0.02)" }}
            >
                <PendingBadge />
            </div>
        </div>
    );
}

// ── Section renderers ─────────────────────────────────────────────────────────
function Overview() {
    const stats = [
        {
            label: "Total Users",
            value: "—",
            icon: Users,
            color: "#06b6d4",
            bg: "rgba(6,182,212,0.1)",
            border: "rgba(6,182,212,0.2)",
            change: "Waiting for backend",
        },
        {
            label: "Active Today",
            value: "—",
            icon: Activity,
            color: "#22c55e",
            bg: "rgba(34,197,94,0.1)",
            border: "rgba(34,197,94,0.2)",
            change: "Waiting for backend",
        },
        {
            label: "Rooms",
            value: "4",
            icon: Hash,
            color: "#a78bfa",
            bg: "rgba(167,139,250,0.1)",
            border: "rgba(167,139,250,0.2)",
            change: "General, HR Issues, Hyderabad, Bangalore",
        },
        {
            label: "Messages Today",
            value: "—",
            icon: MessageSquare,
            color: "#f59e0b",
            bg: "rgba(245,158,11,0.1)",
            border: "rgba(245,158,11,0.2)",
            change: "Waiting for backend",
        },
    ];

    return (
        <div className="space-y-6">
            {/* Stat cards */}
            <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
                {stats.map(s => (
                    <div
                        key={s.label}
                        className="rounded-2xl p-5 transition-all"
                        style={{
                            background: s.bg,
                            border: `1px solid ${s.border}`,
                        }}
                    >
                        <div className="flex items-center justify-between mb-3">
                            <span className="text-xs font-semibold" style={{ color: "#64748b" }}>
                                {s.label}
                            </span>
                            <s.icon size={16} style={{ color: s.color }} />
                        </div>
                        <div
                            className="text-3xl font-black mb-1"
                            style={{ color: s.value === "—" ? "#334155" : "#fff" }}
                        >
                            {s.value}
                        </div>
                        <p className="text-[10px]" style={{ color: "#475569" }}>
                            {s.change}
                        </p>
                    </div>
                ))}
            </div>

            {/* Recent activity placeholder */}
            <SectionCard title="Recent Activity" icon={TrendingUp}>
                <div
                    className="flex flex-col items-center justify-center py-10 rounded-xl"
                    style={{
                        background: "rgba(255,255,255,0.02)",
                        border: "1px dashed rgba(255,255,255,0.08)",
                    }}
                >
                    <TrendingUp size={32} className="mb-3" style={{ color: "#1e293b" }} />
                    <p className="text-sm font-medium mb-2" style={{ color: "#334155" }}>
                        Activity feed coming soon
                    </p>
                    <PendingBadge />
                </div>
            </SectionCard>
        </div>
    );
}

function UserManagement() {
    return (
        <div className="space-y-5">
            <SectionCard title="All Users" icon={Users}>
                <PlaceholderTable
                    columns={["Anonymous Name", "Email", "Department", "Branch", "Joined", "Status"]}
                    rows={5}
                />
            </SectionCard>
        </div>
    );
}

function RoomManagement() {
    const rooms = [
        { name: "General Chat", members: "—", messages: "—", status: "Active" },
        { name: "HR Issues",    members: "—", messages: "—", status: "Active" },
        { name: "Hyderabad",    members: "—", messages: "—", status: "Active" },
        { name: "Bangalore",    members: "—", messages: "—", status: "Active" },
    ];

    return (
        <div className="space-y-5">
            <SectionCard title="Room Management" icon={Hash}>
                <div className="space-y-2">
                    {rooms.map(r => (
                        <div
                            key={r.name}
                            className="flex items-center justify-between px-4 py-3.5 rounded-xl"
                            style={{
                                background: "rgba(255,255,255,0.03)",
                                border: "1px solid rgba(255,255,255,0.06)",
                            }}
                        >
                            <div className="flex items-center gap-3">
                                <div
                                    className="w-8 h-8 rounded-lg flex items-center justify-center text-xs font-bold"
                                    style={{ background: "rgba(6,182,212,0.15)", color: "#06b6d4" }}
                                >
                                    #
                                </div>
                                <div>
                                    <div className="text-sm font-semibold text-white">{r.name}</div>
                                    <div className="text-xs mt-0.5" style={{ color: "#475569" }}>
                                        Members: {r.members} · Messages: {r.messages}
                                    </div>
                                </div>
                            </div>
                            <div className="flex items-center gap-3">
                                <PendingBadge />
                                <ChevronRight size={14} style={{ color: "#334155" }} />
                            </div>
                        </div>
                    ))}
                </div>
            </SectionCard>
        </div>
    );
}

function Moderation() {
    return (
        <div className="space-y-5">
            <SectionCard title="Reported Messages" icon={AlertTriangle}>
                <div
                    className="flex flex-col items-center justify-center py-10 rounded-xl"
                    style={{
                        background: "rgba(255,255,255,0.02)",
                        border: "1px dashed rgba(239,68,68,0.15)",
                    }}
                >
                    <Shield size={32} className="mb-3" style={{ color: "#1e293b" }} />
                    <p className="text-sm font-medium mb-2" style={{ color: "#334155" }}>
                        No reports yet
                    </p>
                    <PendingBadge />
                </div>
            </SectionCard>
        </div>
    );
}

function Analytics() {
    return (
        <div className="space-y-5">
            {[
                { title: "Messages Over Time", icon: TrendingUp },
                { title: "Active Users",       icon: Users },
                { title: "Room Activity",      icon: BarChart3 },
            ].map(c => (
                <SectionCard key={c.title} title={c.title} icon={c.icon}>
                    <div
                        className="flex flex-col items-center justify-center py-12 rounded-xl"
                        style={{
                            background: "rgba(255,255,255,0.02)",
                            border: "1px dashed rgba(255,255,255,0.06)",
                        }}
                    >
                        <BarChart3 size={32} className="mb-3" style={{ color: "#1e293b" }} />
                        <p className="text-sm font-medium mb-2" style={{ color: "#334155" }}>
                            Chart placeholder
                        </p>
                        <PendingBadge />
                    </div>
                </SectionCard>
            ))}
        </div>
    );
}

function SettingsSection() {
    const fields = [
        { label: "System Name",      value: "ZapPulse" },
        { label: "Max Room Members", value: "—" },
        { label: "JWT Expiry",       value: "—" },
        { label: "Anonymous Mode",   value: "Enabled" },
        { label: "Admin Email",      value: "—" },
    ];

    return (
        <div className="space-y-5">
            <SectionCard title="System Settings" icon={Settings}>
                <div className="space-y-2">
                    {fields.map(f => (
                        <div
                            key={f.label}
                            className="flex items-center justify-between px-4 py-3 rounded-xl"
                            style={{
                                background: "rgba(255,255,255,0.03)",
                                border: "1px solid rgba(255,255,255,0.06)",
                            }}
                        >
                            <span className="text-sm" style={{ color: "#64748b" }}>{f.label}</span>
                            <div className="flex items-center gap-3">
                                <span
                                    className="text-sm font-semibold"
                                    style={{ color: f.value === "—" ? "#334155" : "#e2e8f0" }}
                                >
                                    {f.value}
                                </span>
                                {f.value === "—" && <PendingBadge />}
                            </div>
                        </div>
                    ))}
                </div>
            </SectionCard>
        </div>
    );
}

// ── Main AdminDashboard ───────────────────────────────────────────────────────
export default function AdminDashboard() {
    const [activeSection, setActiveSection] = useState<AdminSection>("overview");
    const myName = getAnonymousName();

    const renderSection = () => {
        switch (activeSection) {
            case "overview":   return <Overview />;
            case "users":      return <UserManagement />;
            case "rooms":      return <RoomManagement />;
            case "moderation": return <Moderation />;
            case "analytics":  return <Analytics />;
            case "settings":   return <SettingsSection />;
        }
    };

    const currentNav = NAV_ITEMS.find(n => n.id === activeSection)!;

    return (
        <div
            className="h-screen flex overflow-hidden"
            style={{ background: "#080e1a" }}
        >
            {/* ── Admin Sidebar ─────────────────────────────────────── */}
            <div
                className="w-56 shrink-0 flex flex-col"
                style={{
                    background: "linear-gradient(180deg, #0d1628 0%, #0a1120 100%)",
                    borderRight: "1px solid rgba(255,255,255,0.06)",
                }}
            >
                {/* Logo */}
                <div
                    className="px-4 py-5 flex items-center gap-3 shrink-0"
                    style={{ borderBottom: "1px solid rgba(255,255,255,0.06)" }}
                >
                    <div
                        className="w-9 h-9 rounded-xl flex items-center justify-center shrink-0"
                        style={{
                            background: "linear-gradient(135deg, #7c3aed, #6d28d9)",
                            boxShadow: "0 0 16px rgba(124,58,237,0.4)",
                        }}
                    >
                        <Shield size={16} className="text-white" />
                    </div>
                    <div>
                        <div className="text-xs font-bold text-white">ZapPulse</div>
                        <div className="text-[10px] mt-0.5 font-semibold uppercase tracking-wider"
                            style={{ color: "#a78bfa" }}>
                            Admin Console
                        </div>
                    </div>
                </div>

                {/* Nav */}
                <nav className="flex-1 py-4 px-2 space-y-0.5 overflow-y-auto">
                    {NAV_ITEMS.map(item => {
                        const active = activeSection === item.id;
                        return (
                            <button
                                key={item.id}
                                onClick={() => setActiveSection(item.id)}
                                className="w-full flex items-center gap-2.5 px-3 py-2.5 rounded-xl text-left text-[13px] font-medium transition-all duration-150"
                                style={active ? {
                                    background: "rgba(139,92,246,0.15)",
                                    color: "#a78bfa",
                                } : { color: "#64748b" }}
                                onMouseEnter={e => {
                                    if (!active) {
                                        e.currentTarget.style.background = "rgba(255,255,255,0.05)";
                                        e.currentTarget.style.color = "#94a3b8";
                                    }
                                }}
                                onMouseLeave={e => {
                                    if (!active) {
                                        e.currentTarget.style.background = "transparent";
                                        e.currentTarget.style.color = "#64748b";
                                    }
                                }}
                            >
                                <item.icon size={15} className="shrink-0" />
                                <span className="flex-1">{item.label}</span>
                                {item.badge && (
                                    <span
                                        className="w-4 h-4 rounded-full flex items-center justify-center text-[9px] font-bold shrink-0"
                                        style={{ background: "#ef4444", color: "#fff" }}
                                    >
                                        {item.badge}
                                    </span>
                                )}
                            </button>
                        );
                    })}
                </nav>

                {/* Footer */}
                <div
                    className="px-3 py-3 shrink-0"
                    style={{ borderTop: "1px solid rgba(255,255,255,0.06)" }}
                >
                    <div
                        className="flex items-center gap-2.5 p-2.5 rounded-xl"
                        style={{ background: "rgba(255,255,255,0.04)" }}
                    >
                        <div
                            className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white shrink-0"
                            style={{ background: "linear-gradient(135deg, #7c3aed, #6d28d9)" }}
                        >
                            {myName.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex-1 min-w-0">
                            <div className="text-xs font-semibold text-white truncate">{myName}</div>
                            <div className="text-[10px]" style={{ color: "#a78bfa" }}>Administrator</div>
                        </div>
                        <button
                            onClick={logout}
                            className="p-1 rounded transition-colors shrink-0"
                            style={{ color: "#475569" }}
                            onMouseEnter={e => (e.currentTarget.style.color = "#f87171")}
                            onMouseLeave={e => (e.currentTarget.style.color = "#475569")}
                            title="Sign out"
                        >
                            <LogOut size={13} />
                        </button>
                    </div>
                </div>
            </div>

            {/* ── Main content ──────────────────────────────────────── */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* Top header */}
                <header
                    className="h-14 shrink-0 flex items-center justify-between px-6"
                    style={{
                        background: "rgba(13,22,40,0.95)",
                        borderBottom: "1px solid rgba(255,255,255,0.06)",
                        backdropFilter: "blur(12px)",
                    }}
                >
                    <div className="flex items-center gap-3">
                        <currentNav.icon size={18} style={{ color: "#a78bfa" }} />
                        <h1 className="text-base font-bold text-white">{currentNav.label}</h1>
                        <span
                            className="text-[10px] px-2 py-0.5 rounded-full font-semibold"
                            style={{
                                background: "rgba(139,92,246,0.15)",
                                color: "#a78bfa",
                                border: "1px solid rgba(139,92,246,0.25)",
                            }}
                        >
                            Admin View
                        </span>
                    </div>

                    {/* Backend warning banner */}
                    <div
                        className="hidden md:flex items-center gap-2 px-3 py-1.5 rounded-lg"
                        style={{
                            background: "rgba(251,191,36,0.08)",
                            border: "1px solid rgba(251,191,36,0.2)",
                        }}
                    >
                        <AlertTriangle size={12} style={{ color: "#fbbf24" }} />
                        <span className="text-[11px] font-medium" style={{ color: "#92400e" }}>
                            Admin backend integration pending
                        </span>
                    </div>
                </header>

                {/* Section content */}
                <div className="flex-1 overflow-y-auto px-6 py-6">
                    {renderSection()}
                </div>
            </div>
        </div>
    );
}
