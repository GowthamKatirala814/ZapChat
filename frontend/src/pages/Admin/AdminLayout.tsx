import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import {
    LayoutDashboard,
    Users,
    ShieldAlert,
    BarChart3,
    Building2,
    ScrollText,
    LogOut,
    ChevronRight,
    Activity,
    Menu,
    X,
    Sun,
    Moon,
} from "lucide-react";
import { useDispatch } from "react-redux";
import { logout } from "../../store/authSlice";
import { useTheme } from "../../context/ThemeContext";

const NAV = [
    { to: "/admin", icon: LayoutDashboard, label: "Dashboard", end: true },
    { to: "/admin/users", icon: Users, label: "Users" },
    { to: "/admin/reports", icon: ShieldAlert, label: "Reports" },
    { to: "/admin/ai-health", icon: Activity, label: "AI Moderation" },
    { to: "/admin/analytics", icon: BarChart3, label: "Analytics" },
    { to: "/admin/rooms", icon: Building2, label: "Rooms" },
    { to: "/admin/audit-logs", icon: ScrollText, label: "Audit Logs" },
];

export default function AdminLayout() {
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const { isDark, toggleTheme } = useTheme();
    const [sidebarOpen, setSidebarOpen] = useState(false);

    const handleLogout = async () => {
        try {
            await fetch("https://localhost:5000/api/auth/logout", {
                method: "POST",
                credentials: "include",
            });
        } catch {
            // Non-fatal
        }
        dispatch(logout());
        navigate("/");
    };

    return (
        <div className="flex h-screen overflow-hidden" style={{ background: isDark ? "#020617" : "#f0f9ff" }}>
            {/* ── Mobile backdrop ─────────────────────────────────────────── */}
            {sidebarOpen && (
                <div
                    className="fixed inset-0 z-40 md:hidden drawer-backdrop"
                    onClick={() => setSidebarOpen(false)}
                />
            )}

            {/* ── Sidebar ─────────────────────────────────────────────────── */}
            <aside
                className={`
                    sidebar-drawer
                    fixed md:relative inset-y-0 left-0 z-50 md:z-auto
                    w-60 shrink-0 flex flex-col border-r
                    ${sidebarOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"}
                `}
                style={{
                    background: isDark ? "rgba(2,6,23,0.98)" : "#0f172a",
                    borderColor: "rgba(255,255,255,0.07)",
                }}
            >
                {/* Brand */}
                <div className="px-5 py-5 flex items-center justify-between border-b" style={{ borderColor: "rgba(255,255,255,0.07)" }}>
                    <div className="flex items-center gap-3">
                        <div
                            className="w-8 h-8 rounded-lg flex items-center justify-center shrink-0"
                            style={{ background: "linear-gradient(135deg,#0EA5E9,#0284C7)", boxShadow: "0 0 16px rgba(14,165,233,0.35)" }}
                        >
                            <span className="text-sm font-black text-white">Z</span>
                        </div>
                        <div>
                            <p className="text-sm font-bold text-white leading-none">Zap<span style={{ color: "#38BDF8" }}>Chat</span></p>
                            <p className="text-[10px] font-semibold uppercase tracking-widest mt-0.5" style={{ color: "#38BDF8" }}>Admin</p>
                        </div>
                    </div>
                    {/* Close button on mobile */}
                    <button
                        onClick={() => setSidebarOpen(false)}
                        className="md:hidden p-1 text-slate-400 hover:text-white transition-colors"
                    >
                        <X size={16} />
                    </button>
                </div>

                {/* Nav */}
                <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
                    {NAV.map(({ to, icon: Icon, label, end }) => (
                        <NavLink
                            key={to}
                            to={to}
                            end={end}
                            onClick={() => setSidebarOpen(false)}
                            className={({ isActive }) =>
                                `flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 group
                                ${isActive
                                    ? "bg-sky-600/20 text-sky-300 border border-sky-500/30"
                                    : "text-slate-400 hover:text-slate-200 hover:bg-slate-800/60"
                                }`
                            }
                        >
                            {({ isActive }) => (
                                <>
                                    <Icon size={16} className={isActive ? "text-sky-400" : "text-slate-500 group-hover:text-slate-300"} />
                                    <span className="flex-1">{label}</span>
                                    {isActive && <ChevronRight size={12} className="text-sky-400" />}
                                </>
                            )}
                        </NavLink>
                    ))}
                </nav>

                {/* Footer */}
                <div className="px-3 py-4 border-t space-y-1" style={{ borderColor: "rgba(255,255,255,0.07)" }}>
                    {/* Theme toggle */}
                    <button
                        onClick={toggleTheme}
                        className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-400 hover:text-sky-300 hover:bg-slate-800/60 transition-all"
                    >
                        {isDark ? <Sun size={16} /> : <Moon size={16} />}
                        {isDark ? "Light Mode" : "Dark Mode"}
                    </button>
                    <button
                        onClick={handleLogout}
                        className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-all"
                    >
                        <LogOut size={16} />
                        Sign Out
                    </button>
                </div>
            </aside>

            {/* ── Main content ────────────────────────────────────────────── */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* Mobile top bar */}
                <div
                    className="md:hidden flex items-center justify-between px-4 py-3 shrink-0"
                    style={{
                        background: isDark ? "#0f172a" : "#0f172a",
                        borderBottom: "1px solid rgba(255,255,255,0.07)",
                    }}
                >
                    <button
                        onClick={() => setSidebarOpen(true)}
                        className="p-2 rounded-lg text-slate-400 hover:text-white transition-colors"
                        aria-label="Open admin menu"
                        id="admin-hamburger"
                    >
                        <Menu size={20} />
                    </button>
                    <div className="flex items-center gap-2">
                        <div
                            className="w-6 h-6 rounded-md flex items-center justify-center"
                            style={{ background: "linear-gradient(135deg,#0EA5E9,#0284C7)" }}
                        >
                            <span className="text-xs font-black text-white">Z</span>
                        </div>
                        <span className="text-sm font-bold text-white">Zap<span style={{ color: "#38BDF8" }}>Chat</span></span>
                        <span className="text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded" style={{ background: "rgba(56,189,248,0.15)", color: "#38BDF8" }}>Admin</span>
                    </div>
                    <button
                        onClick={toggleTheme}
                        className="p-2 rounded-lg text-slate-400 hover:text-white transition-colors"
                    >
                        {isDark ? <Sun size={16} /> : <Moon size={16} />}
                    </button>
                </div>

                <main className="flex-1 overflow-y-auto" style={{ color: isDark ? "#f1f5f9" : "#f1f5f9" }}>
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
