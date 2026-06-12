import { useState } from "react";
import { useForm } from "react-hook-form";
import { useDispatch } from "react-redux";
import { useNavigate, Link } from "react-router-dom";
import { login } from "../../api/authApi";
import { loginSuccess } from "../../store/authSlice";

interface LoginForm {
    email: string;
    password: string;
}

export default function LoginPage() {
    const navigate = useNavigate();
    const dispatch = useDispatch();

    const [showPassword, setShowPassword] = useState(false);
    const [apiError, setApiError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<LoginForm>();

    // Decode the role claim embedded in the JWT — determines redirect destination
    const decodeRole = (token: string): "admin" | "user" => {
        try {
            const payload = JSON.parse(atob(token.split(".")[1]));
            const roleKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
            const raw: string | string[] | undefined = payload[roleKey] ?? payload["role"];
            const roles = Array.isArray(raw) ? raw : raw ? [raw] : [];
            return roles.some((r: string) => r.toLowerCase() === "admin") ? "admin" : "user";
        } catch {
            return "user";
        }
    };

    const onSubmit = async (data: LoginForm) => {
        setApiError(null);
        try {
            const result = await login({
                email: data.email,
                password: data.password,
            });

            const actualRole = decodeRole(result.token);

            dispatch(
                loginSuccess({
                    token: result.token,
                    userId: result.userId,
                    anonymousName: result.anonymousName,
                    email: data.email,
                    role: actualRole,
                })
            );

            setSuccess(true);
            setTimeout(() => navigate(actualRole === "admin" ? "/admin" : "/dashboard"), 800);
        } catch (err: unknown) {
            const message =
                (err as { response?: { data?: { message?: string } } })?.response?.data
                    ?.message ?? "Invalid email or password. Please try again.";
            setApiError(message);
        }
    };

    return (
        <div className="min-h-screen flex bg-slate-950">
            {/* ── Left: Branding panel ── */}
            <div className="hidden lg:flex lg:w-1/2 relative flex-col items-center justify-center overflow-hidden">
                {/* Animated gradient background */}
                <div
                    className="absolute inset-0"
                    style={{
                        background:
                            "linear-gradient(135deg, #020617 0%, #0c1a3a 35%, #0f2d5a 65%, #062030 100%)",
                    }}
                />
                {/* Glowing orbs */}
                <div
                    className="absolute w-96 h-96 rounded-full opacity-20 blur-3xl"
                    style={{
                        background: "radial-gradient(circle, #06b6d4, transparent)",
                        top: "10%",
                        left: "5%",
                        animation: "pulse 6s ease-in-out infinite",
                    }}
                />
                <div
                    className="absolute w-64 h-64 rounded-full opacity-15 blur-3xl"
                    style={{
                        background: "radial-gradient(circle, #0ea5e9, transparent)",
                        bottom: "15%",
                        right: "10%",
                        animation: "pulse 8s ease-in-out infinite reverse",
                    }}
                />
                {/* Grid overlay */}
                <div
                    className="absolute inset-0 opacity-5"
                    style={{
                        backgroundImage:
                            "linear-gradient(rgba(6,182,212,0.5) 1px, transparent 1px), linear-gradient(90deg, rgba(6,182,212,0.5) 1px, transparent 1px)",
                        backgroundSize: "60px 60px",
                    }}
                />

                {/* Content */}
                <div className="relative z-10 flex flex-col items-center text-center px-12 max-w-lg">
                    {/* Logo */}
                    <div
                        className="w-20 h-20 rounded-2xl flex items-center justify-center mb-8 shadow-2xl"
                        style={{
                            background:
                                "linear-gradient(135deg, #0ea5e9 0%, #06b6d4 50%, #0891b2 100%)",
                            boxShadow: "0 0 40px rgba(6,182,212,0.4)",
                        }}
                    >
                        <span className="text-4xl font-black text-white select-none">Z</span>
                    </div>

                    <h1 className="text-5xl font-black text-white mb-3 tracking-tight">
                        Zap<span style={{ color: "#06b6d4" }}>Pulse</span>
                    </h1>
                    <p className="text-slate-400 text-lg font-medium mb-12">
                        Enterprise-grade anonymous messaging
                    </p>

                    {/* Feature pills */}
                    <div className="flex flex-col gap-5 w-full">
                        {[
                            {
                                icon: "🔒",
                                title: "End-to-End Encrypted",
                                desc: "Your messages stay private",
                            },
                            {
                                icon: "⚡",
                                title: "Real-Time Messaging",
                                desc: "Instant delivery with SignalR",
                            },
                            {
                                icon: "🎭",
                                title: "Anonymous Identity",
                                desc: "Professional anonymity by design",
                            },
                        ].map((f) => (
                            <div
                                key={f.title}
                                className="flex items-center gap-4 px-5 py-4 rounded-xl text-left"
                                style={{
                                    background: "rgba(255,255,255,0.04)",
                                    border: "1px solid rgba(255,255,255,0.08)",
                                    backdropFilter: "blur(10px)",
                                }}
                            >
                                <span className="text-2xl">{f.icon}</span>
                                <div>
                                    <div className="text-white font-semibold text-sm">
                                        {f.title}
                                    </div>
                                    <div className="text-slate-400 text-xs mt-0.5">{f.desc}</div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* ── Right: Form panel ── */}
            <div className="flex-1 flex items-center justify-center px-6 py-12 relative">
                {/* Subtle background gradient */}
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
                            Zap<span style={{ color: "#38BDF8" }}>Com</span>
                        </span>
                    </div>

                    {/* Header */}
                    <div className="mb-8">
                        <h2 className="text-3xl font-bold text-white mb-2">Welcome back</h2>
                        <p className="text-slate-400">
                            Sign in to your{" "}
                            <span style={{ color: "#0EA5E9" }} className="font-medium">
                                ZapCom
                            </span>{" "}
                            workspace
                        </p>
                    </div>

                    {/* Form card */}
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
                            <div
                                className="flex flex-col items-center gap-3 py-6 text-center"
                                style={{ animation: "fadeIn 0.3s ease" }}
                            >
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
                                <p className="text-white font-semibold text-lg">
                                    Signed in successfully!
                                </p>
                                <p className="text-slate-400 text-sm">
                                    Redirecting to dashboard…
                                </p>
                            </div>
                        )}

                        {/* Error banner */}
                        {apiError && !success && (
                            <div
                                className="flex items-start gap-3 px-4 py-3 rounded-xl mb-5"
                                style={{
                                    background: "rgba(239,68,68,0.1)",
                                    border: "1px solid rgba(239,68,68,0.3)",
                                }}
                            >
                                <span className="text-red-400 text-base mt-0.5">⚠</span>
                                <p className="text-red-400 text-sm">{apiError}</p>
                            </div>
                        )}

                        {!success && (
                            <form
                                id="login-form"
                                onSubmit={handleSubmit(onSubmit)}
                                className="space-y-5"
                            >
                                {/* Email */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Email address
                                    </label>
                                    <input
                                        {...register("email", {
                                            required: "Email is required",
                                            pattern: {
                                                value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                                                message: "Enter a valid email address",
                                            },
                                        })}
                                        id="input-email"
                                        type="email"
                                        placeholder="you@company.com"
                                        autoComplete="email"
                                        className="w-full rounded-xl px-4 py-3 text-white text-sm outline-none transition-all duration-200"
                                        style={{
                                            background: "rgba(255,255,255,0.05)",
                                            border: errors.email
                                                ? "1px solid rgba(239,68,68,0.6)"
                                                : "1px solid rgba(255,255,255,0.1)",
                                            caretColor: "#06b6d4",
                                        }}
                                        onFocus={(e) => {
                                            e.target.style.border = "1px solid rgba(6,182,212,0.7)";
                                            e.target.style.boxShadow = "0 0 0 3px rgba(6,182,212,0.1)";
                                        }}
                                        onBlur={(e) => {
                                            e.target.style.border = errors.email
                                                ? "1px solid rgba(239,68,68,0.6)"
                                                : "1px solid rgba(255,255,255,0.1)";
                                            e.target.style.boxShadow = "none";
                                        }}
                                    />
                                    {errors.email && (
                                        <p className="text-red-400 text-xs mt-1.5">
                                            {errors.email.message}
                                        </p>
                                    )}
                                </div>

                                {/* Password */}
                                <div>
                                    <div className="flex items-center justify-between mb-2">
                                        <label className="block text-sm font-medium text-slate-300">
                                            Password
                                        </label>
                                        <Link
                                            to="/forgot-password"
                                            id="forgot-password-link"
                                            className="text-xs transition-colors"
                                            style={{ color: "#06b6d4" }}
                                            onMouseEnter={(e) =>
                                                ((e.target as HTMLElement).style.color = "#22d3ee")
                                            }
                                            onMouseLeave={(e) =>
                                                ((e.target as HTMLElement).style.color = "#06b6d4")
                                            }
                                        >
                                            Forgot password?
                                        </Link>
                                    </div>
                                    <div className="relative">
                                        <input
                                            {...register("password", {
                                                required: "Password is required",
                                                minLength: {
                                                    value: 6,
                                                    message: "Password must be at least 6 characters",
                                                },
                                            })}
                                            id="input-password"
                                            type={showPassword ? "text" : "password"}
                                            placeholder="••••••••"
                                            autoComplete="current-password"
                                            className="w-full rounded-xl px-4 py-3 pr-12 text-white text-sm outline-none transition-all duration-200"
                                            style={{
                                                background: "rgba(255,255,255,0.05)",
                                                border: errors.password
                                                    ? "1px solid rgba(239,68,68,0.6)"
                                                    : "1px solid rgba(255,255,255,0.1)",
                                                caretColor: "#06b6d4",
                                            }}
                                            onFocus={(e) => {
                                                e.target.style.border = "1px solid rgba(6,182,212,0.7)";
                                                e.target.style.boxShadow = "0 0 0 3px rgba(6,182,212,0.1)";
                                            }}
                                            onBlur={(e) => {
                                                e.target.style.border = errors.password
                                                    ? "1px solid rgba(239,68,68,0.6)"
                                                    : "1px solid rgba(255,255,255,0.1)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                        <button
                                            type="button"
                                            id="toggle-password"
                                            onClick={() => setShowPassword((v) => !v)}
                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors text-lg leading-none"
                                            aria-label={showPassword ? "Hide password" : "Show password"}
                                        >
                                            {showPassword ? "🙈" : "👁️"}
                                        </button>
                                    </div>
                                    {errors.password && (
                                        <p className="text-red-400 text-xs mt-1.5">
                                            {errors.password.message}
                                        </p>
                                    )}
                                </div>

                                {/* Submit */}
                                <button
                                    type="submit"
                                    id="submit-login"
                                    disabled={isSubmitting}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200 relative overflow-hidden"
                                    style={{
                                        background: isSubmitting
                                            ? "rgba(6,182,212,0.4)"
                                            : "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: isSubmitting
                                            ? "none"
                                            : "0 8px 25px rgba(6,182,212,0.35)",
                                        cursor: isSubmitting ? "not-allowed" : "pointer",
                                    }}
                                >
                                    {isSubmitting ? (
                                        <span className="flex items-center justify-center gap-2">
                                            <svg
                                                className="animate-spin h-4 w-4"
                                                viewBox="0 0 24 24"
                                                fill="none"
                                            >
                                                <circle
                                                    className="opacity-25"
                                                    cx="12"
                                                    cy="12"
                                                    r="10"
                                                    stroke="currentColor"
                                                    strokeWidth="4"
                                                />
                                                <path
                                                    className="opacity-75"
                                                    fill="currentColor"
                                                    d="M4 12a8 8 0 018-8v8z"
                                                />
                                            </svg>
                                            Signing in…
                                        </span>
                                    ) : (
                                        "Sign In →"
                                    )}
                                </button>

                                {/* Register link */}
                                <p className="text-center text-slate-500 text-sm pt-1">
                                    Don't have an account?{" "}
                                    <Link
                                        to="/register"
                                        id="link-register"
                                        className="font-semibold transition-colors"
                                        style={{ color: "#06b6d4" }}
                                        onMouseEnter={(e) =>
                                            ((e.target as HTMLElement).style.color = "#22d3ee")
                                        }
                                        onMouseLeave={(e) =>
                                            ((e.target as HTMLElement).style.color = "#06b6d4")
                                        }
                                    >
                                        Create account
                                    </Link>
                                </p>
                            </form>
                        )}
                    </div>

                    {/* Footer note */}
                    <p className="text-center text-slate-600 text-xs mt-6">
                        By signing in, you agree to ZapCom{" "}
                        <a href="#" className="underline hover:text-slate-400 transition-colors">
                            Terms of Service
                        </a>{" "}
                        &amp;{" "}
                        <a href="#" className="underline hover:text-slate-400 transition-colors">
                            Privacy Policy
                        </a>
                    </p>
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