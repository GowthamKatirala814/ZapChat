import { useState } from "react";
import { useForm } from "react-hook-form";
import { useDispatch } from "react-redux";
import { useNavigate, Link } from "react-router-dom";
import { register as registerUser } from "../../api/authApi";
import { loginSuccess } from "../../store/authSlice";

// ── Types ──────────────────────────────────────────────────────────────────────
interface Step1Form {
    fullName: string;
    email: string;
    department: string;
    branch: string;
}

interface Step2Form {
    password: string;
    confirmPassword: string;
}

// ── Helpers ────────────────────────────────────────────────────────────────────
function getPasswordStrength(pw: string): {
    score: number;
    label: string;
    color: string;
} {
    let score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw)) score++;
    if (/[0-9]/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;

    if (score <= 1) return { score, label: "Weak", color: "#ef4444" };
    if (score === 2) return { score, label: "Fair", color: "#f59e0b" };
    if (score === 3) return { score, label: "Good", color: "#06b6d4" };
    return { score, label: "Strong", color: "#22c55e" };
}

const DEPARTMENTS = [
    "Engineering",
    "Product",
    "Design",
    "Marketing",
    "Sales",
    "HR",
    "Finance",
    "Operations",
    "Legal",
    "Other",
];

const BRANCHES = [
    "Headquarters",
    "North Branch",
    "South Branch",
    "East Branch",
    "West Branch",
    "Remote",
    "International",
];

// ── Component ──────────────────────────────────────────────────────────────────
export default function RegisterPage() {
    const navigate = useNavigate();
    const dispatch = useDispatch();

    const [step, setStep] = useState<1 | 2>(1);
    const [step1Data, setStep1Data] = useState<Step1Form | null>(null);
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [apiError, setApiError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);
    const [pwValue, setPwValue] = useState("");

    // Step 1 form
    const {
        register: r1,
        handleSubmit: hs1,
        formState: { errors: e1 },
    } = useForm<Step1Form>();

    // Step 2 form
    const {
        register: r2,
        handleSubmit: hs2,
        watch: w2,
        formState: { errors: e2, isSubmitting },
    } = useForm<Step2Form>();

    const passwordWatch = w2("password", "");
    const strength = getPasswordStrength(pwValue);

    // Step 1 → Step 2
    const onStep1 = (data: Step1Form) => {
        setStep1Data(data);
        setStep(2);
    };

    // Final submit
    const onStep2 = async (data: Step2Form) => {
        if (!step1Data) return;
        setApiError(null);
        try {
            const result = await registerUser({
                fullName: step1Data.fullName,
                email: step1Data.email,
                password: data.password,
                department: step1Data.department,
                branch: step1Data.branch,
            });

            dispatch(
                loginSuccess({
                    token: result.token,
                    userId: result.userId,
                    anonymousName: result.anonymousName,
                    email: step1Data.email,
                    role: "user",
                })
            );

            setSuccess(true);
            setTimeout(() => navigate("/dashboard"), 1000);
        } catch (err: unknown) {
            const message =
                (err as { response?: { data?: { message?: string } } })?.response?.data
                    ?.message ?? "Registration failed. Please try again.";
            setApiError(message);
        }
    };

    const inputCls =
        "w-full rounded-xl px-4 py-3 text-white text-sm outline-none transition-all duration-200 placeholder-slate-600";
    const inputStyle = (hasError: boolean) => ({
        background: "rgba(255,255,255,0.05)",
        border: hasError
            ? "1px solid rgba(239,68,68,0.6)"
            : "1px solid rgba(255,255,255,0.1)",
        caretColor: "#06b6d4",
    });
    const focusStyle = {
        border: "1px solid rgba(6,182,212,0.7)",
        boxShadow: "0 0 0 3px rgba(6,182,212,0.1)",
    };

    return (
        <div className="min-h-screen flex bg-slate-950">
            {/* ── Left branding panel ──────────────────────────────────────────── */}
            <div className="hidden lg:flex lg:w-[42%] relative flex-col items-center justify-center overflow-hidden">
                <div
                    className="absolute inset-0"
                    style={{
                        background:
                            "linear-gradient(135deg, #020617 0%, #0c1a3a 35%, #0f2d5a 65%, #062030 100%)",
                    }}
                />
                <div
                    className="absolute w-80 h-80 rounded-full opacity-20 blur-3xl"
                    style={{
                        background: "radial-gradient(circle, #06b6d4, transparent)",
                        top: "5%",
                        right: "5%",
                        animation: "pulse 7s ease-in-out infinite",
                    }}
                />
                <div
                    className="absolute w-56 h-56 rounded-full opacity-15 blur-3xl"
                    style={{
                        background: "radial-gradient(circle, #0ea5e9, transparent)",
                        bottom: "10%",
                        left: "10%",
                        animation: "pulse 9s ease-in-out infinite reverse",
                    }}
                />
                <div
                    className="absolute inset-0 opacity-5"
                    style={{
                        backgroundImage:
                            "linear-gradient(rgba(6,182,212,0.5) 1px,transparent 1px),linear-gradient(90deg,rgba(6,182,212,0.5) 1px,transparent 1px)",
                        backgroundSize: "60px 60px",
                    }}
                />

                <div className="relative z-10 flex flex-col items-center text-center px-10 max-w-sm">
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

                    <h1 className="text-4xl font-black text-white mb-3 tracking-tight">
                        Join <span style={{ color: "#06b6d4" }}>ZapPulse</span>
                    </h1>
                    <p className="text-slate-400 text-base mb-10">
                        Create your enterprise account in under 2 minutes
                    </p>

                    {/* Progress visual */}
                    <div className="w-full mb-8">
                        <div className="flex items-center justify-center gap-3 mb-4">
                            {[1, 2].map((s) => (
                                <div key={s} className="flex items-center gap-3">
                                    <div
                                        className="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold transition-all duration-500"
                                        style={
                                            step >= s
                                                ? {
                                                    background:
                                                        "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                                    color: "#fff",
                                                    boxShadow: "0 0 15px rgba(6,182,212,0.5)",
                                                }
                                                : {
                                                    background: "rgba(255,255,255,0.08)",
                                                    color: "#64748b",
                                                }
                                        }
                                    >
                                        {s < step ? "✓" : s}
                                    </div>
                                    {s < 2 && (
                                        <div
                                            className="w-12 h-0.5 rounded transition-all duration-500"
                                            style={{
                                                background:
                                                    step > s
                                                        ? "linear-gradient(90deg,#0ea5e9,#06b6d4)"
                                                        : "rgba(255,255,255,0.1)",
                                            }}
                                        />
                                    )}
                                </div>
                            ))}
                        </div>
                        <p className="text-slate-400 text-sm">
                            Step {step} of 2 —{" "}
                            <span className="text-cyan-400 font-medium">
                                {step === 1 ? "Your Profile" : "Set Password"}
                            </span>
                        </p>
                    </div>

                    {/* Step descriptions */}
                    <div
                        className="w-full p-5 rounded-xl text-left"
                        style={{
                            background: "rgba(255,255,255,0.04)",
                            border: "1px solid rgba(255,255,255,0.08)",
                        }}
                    >
                        {step === 1 ? (
                            <>
                                <p className="text-white font-semibold text-sm mb-2">
                                    📋 Profile Information
                                </p>
                                <ul className="text-slate-400 text-xs space-y-1.5">
                                    <li>• Full name &amp; work email</li>
                                    <li>• Your department &amp; branch</li>
                                    <li>• We never share your data</li>
                                </ul>
                            </>
                        ) : (
                            <>
                                <p className="text-white font-semibold text-sm mb-2">
                                    🔐 Secure Password
                                </p>
                                <ul className="text-slate-400 text-xs space-y-1.5">
                                    <li>• Minimum 8 characters</li>
                                    <li>• Use uppercase &amp; numbers</li>
                                    <li>• Add symbols for extra security</li>
                                </ul>
                            </>
                        )}
                    </div>
                </div>
            </div>

            {/* ── Right: Form panel ───────────────────────────────────────────── */}
            <div className="flex-1 flex items-center justify-center px-6 py-12 relative">
                <div
                    className="absolute inset-0"
                    style={{
                        background:
                            "radial-gradient(ellipse at 80% 50%, rgba(6,182,212,0.05) 0%, transparent 60%)",
                    }}
                />

                <div className="relative w-full max-w-md">
                    {/* Mobile header */}
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
                            Zap<span style={{ color: "#06b6d4" }}>Pulse</span>
                        </span>
                    </div>

                    {/* Mobile step indicator */}
                    <div className="flex lg:hidden items-center gap-2 mb-6">
                        {[1, 2].map((s) => (
                            <div key={s} className="flex items-center gap-2">
                                <div
                                    className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-all duration-300"
                                    style={
                                        step >= s
                                            ? {
                                                background: "linear-gradient(135deg,#0ea5e9,#06b6d4)",
                                                color: "#fff",
                                            }
                                            : { background: "rgba(255,255,255,0.08)", color: "#64748b" }
                                    }
                                >
                                    {s < step ? "✓" : s}
                                </div>
                                {s < 2 && (
                                    <div
                                        className="flex-1 h-0.5 w-16 rounded"
                                        style={{
                                            background:
                                                step > s
                                                    ? "linear-gradient(90deg,#0ea5e9,#06b6d4)"
                                                    : "rgba(255,255,255,0.1)",
                                        }}
                                    />
                                )}
                            </div>
                        ))}
                        <span className="text-slate-400 text-xs ml-2">
                            Step {step} / 2
                        </span>
                    </div>

                    {/* Heading */}
                    <div className="mb-7">
                        <h2 className="text-3xl font-bold text-white mb-2">
                            {step === 1 ? "Create your account" : "Secure your account"}
                        </h2>
                        <p className="text-slate-400">
                            {step === 1
                                ? "Tell us a bit about yourself"
                                : "Choose a strong password to protect your account"}
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
                        {/* Success */}
                        {success && (
                            <div className="flex flex-col items-center gap-4 py-8 text-center">
                                <div
                                    className="w-20 h-20 rounded-full flex items-center justify-center text-4xl"
                                    style={{
                                        background: "rgba(6,182,212,0.12)",
                                        border: "2px solid #06b6d4",
                                        boxShadow: "0 0 30px rgba(6,182,212,0.3)",
                                        animation: "fadeIn 0.4s ease",
                                    }}
                                >
                                    🎉
                                </div>
                                <div>
                                    <p className="text-white font-bold text-xl mb-1">
                                        Account Created!
                                    </p>
                                    <p className="text-slate-400 text-sm">
                                        Welcome to ZapPulse. Redirecting to dashboard…
                                    </p>
                                </div>
                            </div>
                        )}

                        {/* API Error */}
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

                        {/* ── STEP 1 ───────────────────────────────────────────────────────── */}
                        {!success && step === 1 && (
                            <form
                                id="register-step1-form"
                                onSubmit={hs1(onStep1)}
                                className="space-y-5"
                            >
                                {/* Full Name */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Full Name
                                    </label>
                                    <input
                                        {...r1("fullName", {
                                            required: "Full name is required",
                                            minLength: {
                                                value: 2,
                                                message: "Name must be at least 2 characters",
                                            },
                                        })}
                                        id="input-fullname"
                                        type="text"
                                        placeholder="John Doe"
                                        autoComplete="name"
                                        className={inputCls}
                                        style={inputStyle(!!e1.fullName)}
                                        onFocus={(e) =>
                                            Object.assign(e.target.style, focusStyle)
                                        }
                                        onBlur={(e) => {
                                            e.target.style.border = e1.fullName
                                                ? "1px solid rgba(239,68,68,0.6)"
                                                : "1px solid rgba(255,255,255,0.1)";
                                            e.target.style.boxShadow = "none";
                                        }}
                                    />
                                    {e1.fullName && (
                                        <p className="text-red-400 text-xs mt-1.5">
                                            {e1.fullName.message}
                                        </p>
                                    )}
                                </div>

                                {/* Email */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Work Email
                                    </label>
                                    <input
                                        {...r1("email", {
                                            required: "Email is required",
                                            pattern: {
                                                value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                                                message: "Enter a valid email address",
                                            },
                                        })}
                                        id="input-reg-email"
                                        type="email"
                                        placeholder="you@company.com"
                                        autoComplete="email"
                                        className={inputCls}
                                        style={inputStyle(!!e1.email)}
                                        onFocus={(e) =>
                                            Object.assign(e.target.style, focusStyle)
                                        }
                                        onBlur={(e) => {
                                            e.target.style.border = e1.email
                                                ? "1px solid rgba(239,68,68,0.6)"
                                                : "1px solid rgba(255,255,255,0.1)";
                                            e.target.style.boxShadow = "none";
                                        }}
                                    />
                                    {e1.email && (
                                        <p className="text-red-400 text-xs mt-1.5">
                                            {e1.email.message}
                                        </p>
                                    )}
                                </div>

                                {/* Department + Branch row */}
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">
                                            Department
                                        </label>
                                        <select
                                            {...r1("department", {
                                                required: "Please select a department",
                                            })}
                                            id="input-department"
                                            className={inputCls + " cursor-pointer"}
                                            style={{
                                                ...inputStyle(!!e1.department),
                                                appearance: "none",
                                                WebkitAppearance: "none",
                                                backgroundImage:
                                                    "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%2364748b' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E\")",
                                                backgroundRepeat: "no-repeat",
                                                backgroundPosition: "right 14px center",
                                                paddingRight: "40px",
                                            }}
                                        >
                                            <option value="" style={{ background: "#0f172a" }}>
                                                Select…
                                            </option>
                                            {DEPARTMENTS.map((d) => (
                                                <option
                                                    key={d}
                                                    value={d}
                                                    style={{ background: "#0f172a" }}
                                                >
                                                    {d}
                                                </option>
                                            ))}
                                        </select>
                                        {e1.department && (
                                            <p className="text-red-400 text-xs mt-1.5">
                                                {e1.department.message}
                                            </p>
                                        )}
                                    </div>

                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">
                                            Branch
                                        </label>
                                        <select
                                            {...r1("branch", {
                                                required: "Please select a branch",
                                            })}
                                            id="input-branch"
                                            className={inputCls + " cursor-pointer"}
                                            style={{
                                                ...inputStyle(!!e1.branch),
                                                appearance: "none",
                                                WebkitAppearance: "none",
                                                backgroundImage:
                                                    "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%2364748b' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E\")",
                                                backgroundRepeat: "no-repeat",
                                                backgroundPosition: "right 14px center",
                                                paddingRight: "40px",
                                            }}
                                        >
                                            <option value="" style={{ background: "#0f172a" }}>
                                                Select…
                                            </option>
                                            {BRANCHES.map((b) => (
                                                <option
                                                    key={b}
                                                    value={b}
                                                    style={{ background: "#0f172a" }}
                                                >
                                                    {b}
                                                </option>
                                            ))}
                                        </select>
                                        {e1.branch && (
                                            <p className="text-red-400 text-xs mt-1.5">
                                                {e1.branch.message}
                                            </p>
                                        )}
                                    </div>
                                </div>

                                <button
                                    type="submit"
                                    id="step1-next"
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
                                    style={{
                                        background: "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: "0 8px 25px rgba(6,182,212,0.35)",
                                    }}
                                    onMouseEnter={(e) => {
                                        (e.target as HTMLElement).style.boxShadow =
                                            "0 12px 30px rgba(6,182,212,0.5)";
                                        (e.target as HTMLElement).style.transform = "translateY(-1px)";
                                    }}
                                    onMouseLeave={(e) => {
                                        (e.target as HTMLElement).style.boxShadow =
                                            "0 8px 25px rgba(6,182,212,0.35)";
                                        (e.target as HTMLElement).style.transform = "translateY(0)";
                                    }}
                                >
                                    Continue →
                                </button>

                                <p className="text-center text-slate-500 text-sm pt-1">
                                    Already have an account?{" "}
                                    <Link
                                        to="/"
                                        id="link-login"
                                        className="font-semibold"
                                        style={{ color: "#06b6d4" }}
                                    >
                                        Sign in
                                    </Link>
                                </p>
                            </form>
                        )}

                        {/* ── STEP 2 ───────────────────────────────────────────────────────── */}
                        {!success && step === 2 && (
                            <form
                                id="register-step2-form"
                                onSubmit={hs2(onStep2)}
                                className="space-y-5"
                            >
                                {/* Summary of step 1 */}
                                {step1Data && (
                                    <div
                                        className="flex items-center gap-3 px-4 py-3 rounded-xl mb-2"
                                        style={{
                                            background: "rgba(6,182,212,0.06)",
                                            border: "1px solid rgba(6,182,212,0.2)",
                                        }}
                                    >
                                        <div
                                            className="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold shrink-0"
                                            style={{
                                                background: "linear-gradient(135deg,#0ea5e9,#06b6d4)",
                                                color: "#fff",
                                            }}
                                        >
                                            {step1Data.fullName.charAt(0).toUpperCase()}
                                        </div>
                                        <div className="min-w-0">
                                            <p className="text-white text-sm font-semibold truncate">
                                                {step1Data.fullName}
                                            </p>
                                            <p className="text-slate-400 text-xs truncate">
                                                {step1Data.email} · {step1Data.department}
                                            </p>
                                        </div>
                                        <button
                                            type="button"
                                            id="step2-back"
                                            onClick={() => setStep(1)}
                                            className="ml-auto text-xs shrink-0 transition-colors"
                                            style={{ color: "#06b6d4" }}
                                            onMouseEnter={(e) =>
                                                ((e.target as HTMLElement).style.color = "#22d3ee")
                                            }
                                            onMouseLeave={(e) =>
                                                ((e.target as HTMLElement).style.color = "#06b6d4")
                                            }
                                        >
                                            Edit
                                        </button>
                                    </div>
                                )}

                                {/* Password */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Password
                                    </label>
                                    <div className="relative">
                                        <input
                                            {...r2("password", {
                                                required: "Password is required",
                                                minLength: {
                                                    value: 8,
                                                    message: "Minimum 8 characters",
                                                },
                                                onChange: (e) => setPwValue(e.target.value),
                                            })}
                                            id="input-reg-password"
                                            type={showPassword ? "text" : "password"}
                                            placeholder="Create a strong password"
                                            autoComplete="new-password"
                                            className={inputCls + " pr-12"}
                                            style={inputStyle(!!e2.password)}
                                            onFocus={(e) =>
                                                Object.assign(e.target.style, focusStyle)
                                            }
                                            onBlur={(e) => {
                                                e.target.style.border = e2.password
                                                    ? "1px solid rgba(239,68,68,0.6)"
                                                    : "1px solid rgba(255,255,255,0.1)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                        <button
                                            type="button"
                                            id="toggle-reg-password"
                                            onClick={() => setShowPassword((v) => !v)}
                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors text-lg leading-none"
                                        >
                                            {showPassword ? "🙈" : "👁️"}
                                        </button>
                                    </div>
                                    {e2.password && (
                                        <p className="text-red-400 text-xs mt-1.5">
                                            {e2.password.message}
                                        </p>
                                    )}

                                    {/* Strength bar */}
                                    {pwValue.length > 0 && (
                                        <div className="mt-3">
                                            <div className="flex gap-1 mb-1.5">
                                                {[1, 2, 3, 4].map((i) => (
                                                    <div
                                                        key={i}
                                                        className="flex-1 h-1 rounded-full transition-all duration-300"
                                                        style={{
                                                            background:
                                                                i <= strength.score
                                                                    ? strength.color
                                                                    : "rgba(255,255,255,0.1)",
                                                        }}
                                                    />
                                                ))}
                                            </div>
                                            <p
                                                className="text-xs font-medium"
                                                style={{ color: strength.color }}
                                            >
                                                {strength.label} password
                                            </p>
                                        </div>
                                    )}
                                </div>

                                {/* Confirm Password */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Confirm Password
                                    </label>
                                    <div className="relative">
                                        <input
                                            {...r2("confirmPassword", {
                                                required: "Please confirm your password",
                                                validate: (v) =>
                                                    v === passwordWatch ||
                                                    "Passwords do not match",
                                            })}
                                            id="input-confirm-password"
                                            type={showConfirm ? "text" : "password"}
                                            placeholder="Repeat your password"
                                            autoComplete="new-password"
                                            className={inputCls + " pr-12"}
                                            style={inputStyle(!!e2.confirmPassword)}
                                            onFocus={(e) =>
                                                Object.assign(e.target.style, focusStyle)
                                            }
                                            onBlur={(e) => {
                                                e.target.style.border = e2.confirmPassword
                                                    ? "1px solid rgba(239,68,68,0.6)"
                                                    : "1px solid rgba(255,255,255,0.1)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                        <button
                                            type="button"
                                            id="toggle-confirm-password"
                                            onClick={() => setShowConfirm((v) => !v)}
                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors text-lg leading-none"
                                        >
                                            {showConfirm ? "🙈" : "👁️"}
                                        </button>
                                    </div>
                                    {e2.confirmPassword && (
                                        <p className="text-red-400 text-xs mt-1.5">
                                            {e2.confirmPassword.message}
                                        </p>
                                    )}
                                </div>

                                {/* Password requirements */}
                                <div
                                    className="px-4 py-3 rounded-xl"
                                    style={{
                                        background: "rgba(255,255,255,0.03)",
                                        border: "1px solid rgba(255,255,255,0.07)",
                                    }}
                                >
                                    <p className="text-slate-400 text-xs font-medium mb-2">
                                        Password requirements:
                                    </p>
                                    <div className="grid grid-cols-2 gap-1.5">
                                        {[
                                            { label: "8+ characters", met: pwValue.length >= 8 },
                                            {
                                                label: "Uppercase letter",
                                                met: /[A-Z]/.test(pwValue),
                                            },
                                            { label: "Number", met: /[0-9]/.test(pwValue) },
                                            {
                                                label: "Special character",
                                                met: /[^A-Za-z0-9]/.test(pwValue),
                                            },
                                        ].map((req) => (
                                            <div
                                                key={req.label}
                                                className="flex items-center gap-1.5 text-xs"
                                                style={{
                                                    color: req.met ? "#22c55e" : "#64748b",
                                                }}
                                            >
                                                <span>{req.met ? "✓" : "○"}</span>
                                                {req.label}
                                            </div>
                                        ))}
                                    </div>
                                </div>

                                {/* Submit */}
                                <button
                                    type="submit"
                                    id="submit-register"
                                    disabled={isSubmitting}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
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
                                            Creating Account…
                                        </span>
                                    ) : (
                                        "🚀 Create Account"
                                    )}
                                </button>

                                <p className="text-center text-slate-500 text-sm pt-1">
                                    Already have an account?{" "}
                                    <Link
                                        to="/"
                                        id="link-login-step2"
                                        className="font-semibold"
                                        style={{ color: "#06b6d4" }}
                                    >
                                        Sign in
                                    </Link>
                                </p>
                            </form>
                        )}
                    </div>

                    {/* Footer */}
                    <p className="text-center text-slate-600 text-xs mt-6">
                        By creating an account, you agree to ZapPulse{" "}
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
                    50%       { transform: scale(1.12); opacity: 0.25; }
                }
                @keyframes fadeIn {
                    from { opacity: 0; transform: translateY(10px); }
                    to   { opacity: 1; transform: translateY(0); }
                }
                input::placeholder,
                select::placeholder { color: #475569; }
            `}</style>
        </div>
    );
}