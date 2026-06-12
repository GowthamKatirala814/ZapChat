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
} from "lucide-react";
import { useDispatch } from "react-redux";
import { logout } from "../../store/authSlice";

const NAV = [
    { to: "/admin", icon: LayoutDashboard, label: "Dashboard", end: true },
    { to: "/admin/users", icon: Users, label: "Users" },
    { to: "/admin/reports", icon: ShieldAlert, label: "Reports" },
    { to: "/admin/analytics", icon: BarChart3, label: "Analytics" },
    { to: "/admin/rooms", icon: Building2, label: "Rooms" },
    { to: "/admin/audit-logs", icon: ScrollText, label: "Audit Logs" },
];

export default function AdminLayout() {
    const dispatch = useDispatch();
    const navigate = useNavigate();

    const handleLogout = () => {
        dispatch(logout());
        navigate("/");
    };

    return (
        <div className="flex h-screen bg-slate-950 text-white overflow-hidden">
            {/* Sidebar */}
            <aside className="w-60 shrink-0 flex flex-col border-r border-slate-800"
                style={{ background: "rgba(2,6,23,0.98)" }}>

                {/* Brand */}
                <div className="px-5 py-5 border-b border-slate-800 flex items-center gap-3">
                    <div className="w-8 h-8 rounded-lg flex items-center justify-center shrink-0"
                        style={{ background: "linear-gradient(135deg,#0EA5E9,#0284C7)", boxShadow: "0 0 16px rgba(14,165,233,0.35)" }}>
                        <span className="text-sm font-black text-white">Z</span>
                    </div>
                    <div>
                        <p className="text-sm font-bold text-white leading-none">Zap<span style={{ color: "#38BDF8" }}>Com</span></p>
                        <p className="text-[10px] font-semibold uppercase tracking-widest mt-0.5" style={{ color: "#38BDF8" }}>Admin</p>
                    </div>
                </div>

                {/* Nav */}
                <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
                    {NAV.map(({ to, icon: Icon, label, end }) => (
                        <NavLink
                            key={to}
                            to={to}
                            end={end}
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
                <div className="px-3 py-4 border-t border-slate-800">
                    <button
                        onClick={handleLogout}
                        className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-all"
                    >
                        <LogOut size={16} />
                        Sign Out
                    </button>
                </div>
            </aside>

            {/* Main content */}
            <main className="flex-1 overflow-y-auto">
                <Outlet />
            </main>
        </div>
    );
}
