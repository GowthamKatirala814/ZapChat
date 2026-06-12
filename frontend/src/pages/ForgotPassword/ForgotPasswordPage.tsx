import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { forgotPassword } from "../../api/authApi";

export default function ForgotPasswordPage() {
    const navigate = useNavigate();

    const [email, setEmail]       = useState("");
    const [error, setError]       = useState<string | null>(null);
    const [loading, setLoading]   = useState(false);
    const [success, setSuccess]   = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!email.trim()) {
            setError("Please enter your email address.");
            return;
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            setError("Enter a valid email address.");
            return;
        }

        setLoading(true);
        try {
            await forgotPassword(email.trim());
            setSuccess(true);
            // Navigate to verify-otp, pass email via router state
            setTimeout(() => navigate("/verify-otp", { state: { email: email.trim() } }), 1200);
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
                        <span className="text-4xl">🔑</span>
                    </div>
                    <h1 className="text-4xl font-black text-white mb-3 tracking-tight">
                        Forgot your<br />
                        <span style={{ color: "#06b6d4" }}>password?</span>
                    </h1>
                    <p className="text-slate-400 text-base">
                        No worries — we'll send a 6-digit code to your email so you can reset it quickly and securely.
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
                            Zap<span style={{ color: "#38BDF8" }}>Pulse</span>
                        </span>
                    </div>

                    <div className="mb-8">
                        <h2 className="text-3xl font-bold text-white mb-2">Reset Password</h2>
                        <p className="text-slate-400">
                            Enter your registered email and we'll send you a reset code.
                        </p>
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
                                    ✉️
                                </div>
                                <p className="text-white font-semibold text-lg">Code sent!</p>
                                <p className="text-slate-400 text-sm">Redirecting you to verify…</p>
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
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Email address
                                    </label>
                                    <input
                                        id="input-email"
                                        type="email"
                                        value={email}
                                        onChange={e => setEmail(e.target.value)}
                                        placeholder="you@company.com"
                                        autoComplete="email"
                                        className="w-full rounded-xl px-4 py-3 text-white text-sm outline-none transition-all duration-200"
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
                                </div>

                                <button
                                    type="submit"
                                    id="submit-forgot"
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
                                            Sending code…
                                        </span>
                                    ) : (
                                        "Send Reset Code →"
                                    )}
                                </button>

                                <p className="text-center text-slate-500 text-sm pt-1">
                                    Remembered it?{" "}
                                    <Link
                                        to="/login"
                                        className="font-semibold transition-colors"
                                        style={{ color: "#06b6d4" }}
                                        onMouseEnter={e => ((e.target as HTMLElement).style.color = "#22d3ee")}
                                        onMouseLeave={e => ((e.target as HTMLElement).style.color = "#06b6d4")}
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
