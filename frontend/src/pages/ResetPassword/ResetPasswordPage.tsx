import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { resetPassword } from "../../api/authApi";

export default function ResetPasswordPage() {
    const navigate = useNavigate();
    const location = useLocation();
    const resetToken = (location.state as { resetToken?: string })?.resetToken ?? "";

    const [newPassword, setNewPassword]         = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [showNew, setShowNew]                 = useState(false);
    const [showConfirm, setShowConfirm]         = useState(false);
    const [error, setError]                     = useState<string | null>(null);
    const [loading, setLoading]                 = useState(false);
    const [success, setSuccess]                 = useState(false);

    // Redirect if we have no token in state
    if (!resetToken) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-slate-950">
                <div className="text-center">
                    <p className="text-red-400 mb-4">Invalid or missing reset token.</p>
                    <Link to="/forgot-password" style={{ color: "#06b6d4" }}>Start over</Link>
                </div>
            </div>
        );
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (newPassword.length < 6) {
            setError("Password must be at least 6 characters.");
            return;
        }
        if (newPassword !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        setLoading(true);
        try {
            const result = await resetPassword(resetToken, newPassword, confirmPassword);
            if (result.success) {
                setSuccess(true);
                setTimeout(() => navigate("/login"), 2000);
            } else {
                setError(result.message || "Failed to reset password. Please start over.");
            }
        } catch {
            setError("Something went wrong. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex bg-slate-950">
            {/* ── Left branding panel ── */}
            <div className="hidden lg:flex lg:w-1/2 relative flex-col items-center justify-center overflow-hidden">
                <div
                    className="absolute inset-0"
                    style={{
                        background:
                            "linear-gradient(135deg, #020617 0%, #0c1a3a 35%, #0f2d5a 65%, #062030 100%)",
                    }}
                />
                <div
                    className="absolute w-96 h-96 rounded-full opacity-20 blur-3xl"
                    style={{
                        background: "radial-gradient(circle, #06b6d4, transparent)",
                        top: "10%", left: "5%",
                        animation: "pulse 6s ease-in-out infinite",
                    }}
                />
                <div
                    className="absolute inset-0 opacity-5"
                    style={{
                        backgroundImage:
                            "linear-gradient(rgba(6,182,212,0.5) 1px, transparent 1px), linear-gradient(90deg, rgba(6,182,212,0.5) 1px, transparent 1px)",
                        backgroundSize: "60px 60px",
                    }}
                />
                <div className="relative z-10 flex flex-col items-center text-center px-12 max-w-lg">
                    <div
                        className="w-20 h-20 rounded-2xl flex items-center justify-center mb-8 shadow-2xl"
                        style={{
                            background: "linear-gradient(135deg, #0ea5e9 0%, #06b6d4 50%, #0891b2 100%)",
                            boxShadow: "0 0 40px rgba(6,182,212,0.4)",
                        }}
                    >
                        <span className="text-4xl">🔐</span>
                    </div>
                    <h1 className="text-4xl font-black text-white mb-3 tracking-tight">
                        Set a new<br />
                        <span style={{ color: "#06b6d4" }}>password</span>
                    </h1>
                    <p className="text-slate-400 text-base">
                        Choose a strong password of at least 6 characters. You'll use it next time you log in.
                    </p>
                </div>
            </div>

            {/* ── Right form panel ── */}
            <div className="flex-1 flex items-center justify-center px-6 py-12 relative">
                <div
                    className="absolute inset-0"
                    style={{
                        background:
                            "radial-gradient(ellipse at 80% 50%, rgba(6,182,212,0.06) 0%, transparent 60%)",
                    }}
                />

                <div className="relative w-full max-w-md">
                    {/* Mobile logo */}
                    <div className="flex lg:hidden items-center gap-3 mb-8 justify-center">
                        <div
                            className="w-10 h-10 rounded-xl flex items-center justify-center"
                            style={{
                                background: "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                boxShadow: "0 0 20px rgba(6,182,212,0.3)",
                            }}
                        >
                            <span className="text-xl font-black text-white">Z</span>
                        </div>
                        <span className="text-2xl font-black text-white">
                            Zap<span style={{ color: "#38BDF8" }}>Chat</span>
                        </span>
                    </div>

                    <div className="mb-8">
                        <h2 className="text-3xl font-bold text-white mb-2">New password</h2>
                        <p className="text-slate-400">Almost there — choose a password you'll remember.</p>
                    </div>

                    <div
                        className="rounded-2xl p-8"
                        style={{
                            background: "rgba(15,23,42,0.8)",
                            border: "1px solid rgba(255,255,255,0.08)",
                            backdropFilter: "blur(20px)",
                            boxShadow: "0 25px 60px rgba(0,0,0,0.5)",
                        }}
                    >
                        {/* Success state */}
                        {success && (
                            <div className="flex flex-col items-center gap-3 py-6 text-center" style={{ animation: "fadeIn 0.3s ease" }}>
                                <div
                                    className="w-16 h-16 rounded-full flex items-center justify-center text-3xl"
                                    style={{
                                        background: "rgba(6,182,212,0.15)",
                                        border: "2px solid #06b6d4",
                                        boxShadow: "0 0 30px rgba(6,182,212,0.3)",
                                    }}
                                >
                                    ✓
                                </div>
                                <p className="text-white font-semibold text-lg">Password updated!</p>
                                <p className="text-slate-400 text-sm">Redirecting you to login…</p>
                            </div>
                        )}

                        {/* Error banner */}
                        {error && !success && (
                            <div
                                className="flex items-start gap-3 px-4 py-3 rounded-xl mb-5"
                                style={{
                                    background: "rgba(239,68,68,0.1)",
                                    border: "1px solid rgba(239,68,68,0.3)",
                                }}
                            >
                                <span className="text-red-400 text-base mt-0.5">⚠</span>
                                <p className="text-red-400 text-sm">{error}</p>
                            </div>
                        )}

                        {!success && (
                            <form onSubmit={handleSubmit} className="space-y-5">
                                {/* Hint */}
                                <div
                                    className="px-4 py-2.5 rounded-xl text-xs"
                                    style={{
                                        background: "rgba(6,182,212,0.06)",
                                        border: "1px solid rgba(6,182,212,0.15)",
                                        color: "#94a3b8",
                                    }}
                                >
                                    🔒 Minimum 6 characters
                                </div>

                                {/* New Password */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        New Password
                                    </label>
                                    <div className="relative">
                                        <input
                                            id="input-new-password"
                                            type={showNew ? "text" : "password"}
                                            value={newPassword}
                                            onChange={e => setNewPassword(e.target.value)}
                                            placeholder="••••••••"
                                            autoComplete="new-password"
                                            className="w-full rounded-xl px-4 py-3 pr-12 text-white text-sm outline-none transition-all duration-200"
                                            style={{
                                                background: "rgba(255,255,255,0.05)",
                                                border: "1px solid rgba(255,255,255,0.1)",
                                                caretColor: "#06b6d4",
                                            }}
                                            onFocus={e => {
                                                e.target.style.border = "1px solid rgba(6,182,212,0.7)";
                                                e.target.style.boxShadow = "0 0 0 3px rgba(6,182,212,0.1)";
                                            }}
                                            onBlur={e => {
                                                e.target.style.border = "1px solid rgba(255,255,255,0.1)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                        <button
                                            type="button"
                                            id="toggle-new-password"
                                            onClick={() => setShowNew(v => !v)}
                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors text-lg leading-none"
                                        >
                                            {showNew ? "🙈" : "👁️"}
                                        </button>
                                    </div>
                                </div>

                                {/* Confirm Password */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Confirm Password
                                    </label>
                                    <div className="relative">
                                        <input
                                            id="input-confirm-password"
                                            type={showConfirm ? "text" : "password"}
                                            value={confirmPassword}
                                            onChange={e => setConfirmPassword(e.target.value)}
                                            placeholder="••••••••"
                                            autoComplete="new-password"
                                            className="w-full rounded-xl px-4 py-3 pr-12 text-white text-sm outline-none transition-all duration-200"
                                            style={{
                                                background: "rgba(255,255,255,0.05)",
                                                border: confirmPassword && confirmPassword !== newPassword
                                                    ? "1px solid rgba(239,68,68,0.6)"
                                                    : "1px solid rgba(255,255,255,0.1)",
                                                caretColor: "#06b6d4",
                                            }}
                                            onFocus={e => {
                                                e.target.style.border = "1px solid rgba(6,182,212,0.7)";
                                                e.target.style.boxShadow = "0 0 0 3px rgba(6,182,212,0.1)";
                                            }}
                                            onBlur={e => {
                                                e.target.style.border = confirmPassword && confirmPassword !== newPassword
                                                    ? "1px solid rgba(239,68,68,0.6)"
                                                    : "1px solid rgba(255,255,255,0.1)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                        <button
                                            type="button"
                                            id="toggle-confirm-password"
                                            onClick={() => setShowConfirm(v => !v)}
                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors text-lg leading-none"
                                        >
                                            {showConfirm ? "🙈" : "👁️"}
                                        </button>
                                    </div>
                                    {confirmPassword && confirmPassword !== newPassword && (
                                        <p className="text-red-400 text-xs mt-1.5">Passwords do not match</p>
                                    )}
                                </div>

                                <button
                                    type="submit"
                                    id="submit-reset"
                                    disabled={loading}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
                                    style={{
                                        background: loading
                                            ? "rgba(6,182,212,0.4)"
                                            : "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: loading ? "none" : "0 8px 25px rgba(6,182,212,0.35)",
                                        cursor: loading ? "not-allowed" : "pointer",
                                    }}
                                >
                                    {loading ? (
                                        <span className="flex items-center justify-center gap-2">
                                            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                                            </svg>
                                            Resetting…
                                        </span>
                                    ) : (
                                        "Reset Password →"
                                    )}
                                </button>

                                <p className="text-center text-slate-500 text-sm pt-1">
                                    <Link
                                        to="/login"
                                        className="transition-colors"
                                        style={{ color: "#475569" }}
                                        onMouseEnter={e => ((e.target as HTMLElement).style.color = "#94a3b8")}
                                        onMouseLeave={e => ((e.target as HTMLElement).style.color = "#475569")}
                                    >
                                        Back to Login
                                    </Link>
                                </p>
                            </form>
                        )}
                    </div>
                </div>
            </div>

            <style>{`
                @keyframes pulse {
                    0%, 100% { transform: scale(1); opacity: 0.15; }
                    50%       { transform: scale(1.1); opacity: 0.25; }
                }
                @keyframes fadeIn {
                    from { opacity: 0; transform: translateY(8px); }
                    to   { opacity: 1; transform: translateY(0); }
                }
                input::placeholder { color: #475569; }
            `}</style>
        </div>
    );
}
