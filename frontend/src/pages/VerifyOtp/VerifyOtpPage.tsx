import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { verifyOtp, forgotPassword } from "../../api/authApi";

export default function VerifyOtpPage() {
    const navigate  = useNavigate();
    const location  = useLocation();
    const email     = (location.state as { email?: string })?.email ?? "";

    const [digits, setDigits]       = useState<string[]>(["", "", "", "", "", ""]);
    const [error, setError]         = useState<string | null>(null);
    const [loading, setLoading]     = useState(false);
    const [success, setSuccess]     = useState(false);
    const [resent, setResent]       = useState(false);
    const [resending, setResending] = useState(false);
    const [secondsLeft, setSecondsLeft] = useState(600); // 10 minutes

    const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

    // Redirect to /forgot-password if we have no email in state
    useEffect(() => {
        if (!email) navigate("/forgot-password", { replace: true });
    }, [email, navigate]);

    // Countdown timer
    useEffect(() => {
        if (secondsLeft <= 0) return;
        const id = setInterval(() => setSecondsLeft(s => s - 1), 1000);
        return () => clearInterval(id);
    }, [secondsLeft]);

    const minutes = String(Math.floor(secondsLeft / 60)).padStart(2, "0");
    const secs    = String(secondsLeft % 60).padStart(2, "0");

    const handleDigit = (idx: number, val: string) => {
        if (!/^\d?$/.test(val)) return;
        const next = [...digits];
        next[idx] = val;
        setDigits(next);
        if (val && idx < 5) inputRefs.current[idx + 1]?.focus();
    };

    const handleKeyDown = (idx: number, e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "Backspace" && !digits[idx] && idx > 0) {
            inputRefs.current[idx - 1]?.focus();
        }
    };

    const handlePaste = (e: React.ClipboardEvent) => {
        e.preventDefault();
        const pasted = e.clipboardData.getData("text").replace(/\D/g, "").slice(0, 6);
        const next = [...digits];
        pasted.split("").forEach((ch, i) => { if (i < 6) next[i] = ch; });
        setDigits(next);
        const lastFilled = Math.min(pasted.length, 5);
        inputRefs.current[lastFilled]?.focus();
    };

    const otpCode = digits.join("");

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        if (otpCode.length !== 6) {
            setError("Please enter all 6 digits.");
            return;
        }
        setLoading(true);
        try {
            const result = await verifyOtp(email, otpCode);
            if (result.success && result.resetToken) {
                setSuccess(true);
                setTimeout(() => navigate("/reset-password", { state: { resetToken: result.resetToken } }), 800);
            } else {
                setError(result.message || "Invalid or expired OTP.");
            }
        } catch {
            setError("Something went wrong. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    const handleResend = async () => {
        setResending(true);
        setError(null);
        try {
            await forgotPassword(email);
            setResent(true);
            setSecondsLeft(600);
            setDigits(["", "", "", "", "", ""]);
            inputRefs.current[0]?.focus();
            setTimeout(() => setResent(false), 4000);
        } catch {
            setError("Failed to resend code. Please try again.");
        } finally {
            setResending(false);
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
                        <span className="text-4xl">📬</span>
                    </div>
                    <h1 className="text-4xl font-black text-white mb-3 tracking-tight">
                        Check your<br />
                        <span style={{ color: "#06b6d4" }}>inbox</span>
                    </h1>
                    <p className="text-slate-400 text-base">
                        We've sent a 6-digit verification code to{" "}
                        <span className="text-slate-200 font-medium">{email}</span>.
                        It expires in 10 minutes.
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
                        <h2 className="text-3xl font-bold text-white mb-2">Enter your code</h2>
                        <p className="text-slate-400">
                            Sent to{" "}
                            <span className="text-slate-200 font-medium">{email}</span>
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
                                    ✓
                                </div>
                                <p className="text-white font-semibold text-lg">Code verified!</p>
                                <p className="text-slate-400 text-sm">Taking you to reset your password…</p>
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

                        {/* Resent notice */}
                        {resent && !success && (
                            <div
                                className="flex items-start gap-3 px-4 py-3 rounded-xl mb-5"
                                style={{
                                    background: "rgba(6,182,212,0.08)",
                                    border: "1px solid rgba(6,182,212,0.3)",
                                }}
                            >
                                <span className="text-cyan-400 text-base mt-0.5">✉️</span>
                                <p className="text-cyan-400 text-sm">A new code has been sent!</p>
                            </div>
                        )}

                        {!success && (
                            <form onSubmit={handleSubmit} className="space-y-6">
                                {/* Countdown */}
                                <div className="flex items-center justify-between">
                                    <span className="text-slate-400 text-xs">Code expires in</span>
                                    <span
                                        className="text-sm font-mono font-semibold"
                                        style={{ color: secondsLeft < 60 ? "#f87171" : "#06b6d4" }}
                                    >
                                        {minutes}:{secs}
                                    </span>
                                </div>

                                {/* 6 digit boxes */}
                                <div className="flex gap-2 justify-center" onPaste={handlePaste}>
                                    {digits.map((d, i) => (
                                        <input
                                            key={i}
                                            id={`otp-digit-${i}`}
                                            ref={el => { inputRefs.current[i] = el; }}
                                            type="text"
                                            inputMode="numeric"
                                            maxLength={1}
                                            value={d}
                                            onChange={e => handleDigit(i, e.target.value)}
                                            onKeyDown={e => handleKeyDown(i, e)}
                                            className="w-11 h-14 text-center text-xl font-bold text-white rounded-xl outline-none transition-all duration-150"
                                            style={{
                                                background: "rgba(255,255,255,0.07)",
                                                border: d
                                                    ? "1px solid rgba(6,182,212,0.8)"
                                                    : "1px solid rgba(255,255,255,0.12)",
                                                caretColor: "#06b6d4",
                                            }}
                                            onFocus={e => {
                                                e.target.style.border = "1px solid rgba(6,182,212,0.9)";
                                                e.target.style.boxShadow = "0 0 0 3px rgba(6,182,212,0.15)";
                                            }}
                                            onBlur={e => {
                                                e.target.style.border = d
                                                    ? "1px solid rgba(6,182,212,0.8)"
                                                    : "1px solid rgba(255,255,255,0.12)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                    ))}
                                </div>

                                <button
                                    type="submit"
                                    id="submit-otp"
                                    disabled={loading || secondsLeft <= 0}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
                                    style={{
                                        background: (loading || secondsLeft <= 0)
                                            ? "rgba(6,182,212,0.4)"
                                            : "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: (loading || secondsLeft <= 0) ? "none" : "0 8px 25px rgba(6,182,212,0.35)",
                                        cursor: (loading || secondsLeft <= 0) ? "not-allowed" : "pointer",
                                    }}
                                >
                                    {loading ? (
                                        <span className="flex items-center justify-center gap-2">
                                            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                                            </svg>
                                            Verifying…
                                        </span>
                                    ) : secondsLeft <= 0 ? "Code expired" : "Verify Code →"}
                                </button>

                                <div className="flex items-center justify-between text-sm pt-1">
                                    <button
                                        type="button"
                                        id="resend-code"
                                        onClick={handleResend}
                                        disabled={resending}
                                        className="transition-colors"
                                        style={{ color: resending ? "#334155" : "#06b6d4", cursor: resending ? "not-allowed" : "pointer" }}
                                        onMouseEnter={e => { if (!resending) (e.target as HTMLElement).style.color = "#22d3ee"; }}
                                        onMouseLeave={e => { if (!resending) (e.target as HTMLElement).style.color = "#06b6d4"; }}
                                    >
                                        {resending ? "Sending…" : "Resend Code"}
                                    </button>
                                    <Link
                                        to="/login"
                                        className="transition-colors"
                                        style={{ color: "#475569" }}
                                        onMouseEnter={e => ((e.target as HTMLElement).style.color = "#94a3b8")}
                                        onMouseLeave={e => ((e.target as HTMLElement).style.color = "#475569")}
                                    >
                                        Back to Login
                                    </Link>
                                </div>
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
