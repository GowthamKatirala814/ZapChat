import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, Link } from "react-router-dom";
import {
    initiateRegistration,
    verifyRegistrationOtp,
    completeRegistration,
} from "../../api/authApi";

// ── Types ───────────────────────────────────────────────────────────────────

interface Step1Form {
    fullName: string;
    email: string;
    department: string;
    branch: string;
}

interface Step3Form {
    password: string;
    confirmPassword: string;
}

// ── Constants (match existing RegisterPage dropdowns exactly) ────────────────

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

// ── Helpers ──────────────────────────────────────────────────────────────────

/** Masks email: g***m@gmail.com */
function maskEmail(email: string): string {
    const [local, domain] = email.split("@");
    if (!domain) return email;
    if (local.length <= 2) return `${local[0]}***@${domain}`;
    return `${local[0]}${"*".repeat(Math.max(1, local.length - 2))}${local[local.length - 1]}@${domain}`;
}

// ── Shared UI helpers ─────────────────────────────────────────────────────────

const inputCls =
    "w-full rounded-xl px-4 py-3 text-white text-sm outline-none transition-all duration-200 placeholder-slate-600";

const inputStyle = (hasError: boolean): React.CSSProperties => ({
    background: "rgba(255,255,255,0.05)",
    border: hasError ? "1px solid rgba(239,68,68,0.6)" : "1px solid rgba(255,255,255,0.1)",
    caretColor: "#06b6d4",
});

const focusStyle: React.CSSProperties = {
    border: "1px solid rgba(6,182,212,0.7)",
    boxShadow: "0 0 0 3px rgba(6,182,212,0.1)",
};

const selectExtraStyle: React.CSSProperties = {
    appearance: "none",
    WebkitAppearance: "none",
    backgroundImage:
        "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%2364748b' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E\")",
    backgroundRepeat: "no-repeat",
    backgroundPosition: "right 14px center",
    paddingRight: "40px",
    cursor: "pointer",
};

// ── Step indicator component ─────────────────────────────────────────────────

function StepIndicator({ step, total }: { step: number; total: number }) {
    return (
        <div className="flex items-center gap-2">
            {Array.from({ length: total }, (_, i) => i + 1).map((s) => (
                <div key={s} className="flex items-center gap-2">
                    <div
                        className="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-all duration-500"
                        style={
                            step > s
                                ? {
                                      background: "linear-gradient(135deg,#0ea5e9,#06b6d4)",
                                      color: "#fff",
                                      boxShadow: "0 0 12px rgba(6,182,212,0.4)",
                                  }
                                : step === s
                                ? {
                                      background: "linear-gradient(135deg,#0ea5e9,#06b6d4)",
                                      color: "#fff",
                                      boxShadow: "0 0 20px rgba(6,182,212,0.6)",
                                  }
                                : { background: "rgba(255,255,255,0.08)", color: "#64748b" }
                        }
                    >
                        {step > s ? "✓" : s}
                    </div>
                    {s < total && (
                        <div
                            className="w-10 h-0.5 rounded transition-all duration-500"
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
    );
}

// ── Error banner component ────────────────────────────────────────────────────

function ErrorBanner({ message }: { message: string }) {
    return (
        <div
            className="flex items-start gap-3 px-4 py-3 rounded-xl mb-5"
            style={{
                background: "rgba(239,68,68,0.1)",
                border: "1px solid rgba(239,68,68,0.3)",
            }}
        >
            <span className="text-red-400 text-base mt-0.5">⚠</span>
            <p className="text-red-400 text-sm">{message}</p>
        </div>
    );
}

// ── Main component ────────────────────────────────────────────────────────────

export default function RegisterPage() {
    const navigate = useNavigate();

    // Wizard state
    const [step, setStep] = useState<1 | 2 | 3>(1);
    const [step1Data, setStep1Data] = useState<Step1Form | null>(null);
    const [verificationToken, setVerificationToken] = useState<string>("");
    const [apiError, setApiError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);

    // ── Step 1 form ─────────────────────────────────────────────────────────
    const {
        register: r1,
        handleSubmit: hs1,
        formState: { errors: e1, isSubmitting: s1Submitting },
    } = useForm<Step1Form>();

    // ── Step 2 OTP state ────────────────────────────────────────────────────
    const [digits, setDigits] = useState<string[]>(["", "", "", "", "", ""]);
    const [otpLoading, setOtpLoading] = useState(false);
    const [resending, setResending] = useState(false);
    const [resendCooldown, setResendCooldown] = useState(0);
    const [resent, setResent] = useState(false);
    const [secondsLeft, setSecondsLeft] = useState(600);
    const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

    useEffect(() => {
        if (step !== 2) return;
        setSecondsLeft(600);
    }, [step]);

    useEffect(() => {
        if (step !== 2 || secondsLeft <= 0) return;
        const id = setInterval(() => setSecondsLeft((s) => s - 1), 1000);
        return () => clearInterval(id);
    }, [step, secondsLeft]);

    useEffect(() => {
        if (resendCooldown <= 0) return;
        const id = setInterval(() => setResendCooldown((c) => c - 1), 1000);
        return () => clearInterval(id);
    }, [resendCooldown]);

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

    // ── Step 3 form ─────────────────────────────────────────────────────────
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirm, setShowConfirm]   = useState(false);
    const [pwValue, setPwValue]           = useState("");

    const {
        register: r3,
        handleSubmit: hs3,
        watch: w3,
        formState: { errors: e3, isSubmitting: s3Submitting },
    } = useForm<Step3Form>();

    const passwordWatch = w3("password", "");

    // ── Handlers ─────────────────────────────────────────────────────────────

    const onStep1 = async (data: Step1Form) => {
        setApiError(null);
        try {
            const result = await initiateRegistration({
                fullName:   data.fullName,
                email:      data.email,
                department: data.department,
                branch:     data.branch,
            });

            if (!result.success) {
                setApiError(result.message);
                return;
            }

            setStep1Data(data);
            setDigits(["", "", "", "", "", ""]);
            setStep(2);
            setResendCooldown(30);
        } catch (err: unknown) {
            const msg =
                (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
                "Something went wrong. Please try again.";
            setApiError(msg);
        }
    };

    const onVerifyOtp = async () => {
        if (otpCode.length !== 6) {
            setApiError("Please enter all 6 digits.");
            return;
        }
        setApiError(null);
        setOtpLoading(true);
        try {
            const result = await verifyRegistrationOtp({
                email:   step1Data!.email,
                otpCode: otpCode,
            });

            if (!result.success || !result.verificationToken) {
                setApiError(result.message || "Invalid or expired code. Please try again.");
                return;
            }

            setVerificationToken(result.verificationToken);
            setStep(3);
        } catch (err: unknown) {
            const msg =
                (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
                "Verification failed. Please try again.";
            setApiError(msg);
        } finally {
            setOtpLoading(false);
        }
    };

    const onResend = async () => {
        if (!step1Data || resendCooldown > 0) return;
        setResending(true);
        setApiError(null);
        try {
            await initiateRegistration({
                fullName:   step1Data.fullName,
                email:      step1Data.email,
                department: step1Data.department,
                branch:     step1Data.branch,
            });
            setDigits(["", "", "", "", "", ""]);
            setSecondsLeft(600);
            setResent(true);
            setResendCooldown(30);
            inputRefs.current[0]?.focus();
            setTimeout(() => setResent(false), 3000);
        } catch {
            setApiError("Failed to resend code. Please try again.");
        } finally {
            setResending(false);
        }
    };

    const onStep3 = async (data: Step3Form) => {
        setApiError(null);
        try {
            const result = await completeRegistration({
                verificationToken: verificationToken,
                password:          data.password,
                confirmPassword:   data.confirmPassword,
            });

            if (!result.success) {
                setApiError(result.message);
                return;
            }

            setSuccess(true);
            setTimeout(() => navigate("/login"), 2000);
        } catch (err: unknown) {
            const msg =
                (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
                "Account creation failed. Please try again.";
            setApiError(msg);
        }
    };

    // ── Left panel step descriptions ─────────────────────────────────────────

    const leftPanelContent = {
        1: {
            icon: "📋",
            title: "Create your account",
            subtitle: "Join ZapChat in 3 easy steps",
            bullets: [
                "Full name & work email",
                "Your department & branch",
                "We never share your data",
            ],
        },
        2: {
            icon: "📬",
            title: "Verify your email",
            subtitle: "Prove it's really you",
            bullets: [
                "6-digit code sent to your email",
                "Code expires in 10 minutes",
                "No fake accounts allowed",
            ],
        },
        3: {
            icon: "🔐",
            title: "Set your password",
            subtitle: "Almost done!",
            bullets: [
                "Minimum 6 characters",
                "Account created only after this step",
                "Login immediately after",
            ],
        },
    }[step];

    // ── Render ────────────────────────────────────────────────────────────────

    return (
        <div className="min-h-screen flex bg-slate-950">
            {/* ── Left branding panel ────────────────────────────────────────── */}
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
                    {/* Logo */}
                    <div
                        className="w-20 h-20 rounded-2xl flex items-center justify-center mb-6 shadow-2xl"
                        style={{
                            background: "linear-gradient(135deg, #0ea5e9 0%, #06b6d4 50%, #0891b2 100%)",
                            boxShadow: "0 0 40px rgba(6,182,212,0.4)",
                        }}
                    >
                        <span className="text-4xl">{leftPanelContent.icon}</span>
                    </div>

                    <h1 className="text-3xl font-black text-white mb-2 tracking-tight">
                        <span style={{ color: "#06b6d4" }}>{leftPanelContent.title}</span>
                    </h1>
                    <p className="text-slate-400 text-sm mb-8">{leftPanelContent.subtitle}</p>

                    {/* Step indicator */}
                    <div className="mb-8">
                        <StepIndicator step={step} total={3} />
                        <p className="text-slate-400 text-xs mt-3">
                            Step {step} of 3 —{" "}
                            <span className="text-cyan-400 font-medium">
                                {step === 1 ? "Account Details" : step === 2 ? "Email Verification" : "Set Password"}
                            </span>
                        </p>
                    </div>

                    {/* Bullet list */}
                    <div
                        className="w-full p-5 rounded-xl text-left"
                        style={{
                            background: "rgba(255,255,255,0.04)",
                            border: "1px solid rgba(255,255,255,0.08)",
                        }}
                    >
                        <ul className="text-slate-400 text-xs space-y-2">
                            {leftPanelContent.bullets.map((b) => (
                                <li key={b}>• {b}</li>
                            ))}
                        </ul>
                    </div>
                </div>
            </div>

            {/* ── Right: form panel ──────────────────────────────────────────── */}
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
                    <div className="flex lg:hidden items-center gap-3 mb-6 justify-center">
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
                            Zap<span style={{ color: "#06b6d4" }}>Chat</span>
                        </span>
                    </div>

                    {/* Mobile step indicator */}
                    <div className="flex lg:hidden items-center gap-3 mb-6">
                        <StepIndicator step={step} total={3} />
                        <span className="text-slate-400 text-xs">Step {step} / 3</span>
                    </div>

                    {/* Heading */}
                    <div className="mb-7">
                        <h2 className="text-3xl font-bold text-white mb-2">
                            {step === 1 ? "Create your account" : step === 2 ? "Verify your email" : "Set your password"}
                        </h2>
                        <p className="text-slate-400">
                            {step === 1
                                ? "Tell us a bit about yourself"
                                : step === 2
                                ? `Code sent to ${step1Data ? maskEmail(step1Data.email) : ""}`
                                : `Almost done! Setting password for ${step1Data ? maskEmail(step1Data.email) : ""}`}
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
                        {/* ── Success screen ─────────────────────────────────── */}
                        {success && (
                            <div
                                className="flex flex-col items-center gap-4 py-8 text-center"
                                style={{ animation: "fadeIn 0.4s ease" }}
                            >
                                <div
                                    className="w-20 h-20 rounded-full flex items-center justify-center text-4xl"
                                    style={{
                                        background: "rgba(6,182,212,0.12)",
                                        border: "2px solid #06b6d4",
                                        boxShadow: "0 0 30px rgba(6,182,212,0.3)",
                                    }}
                                >
                                    🎉
                                </div>
                                <div>
                                    <p className="text-white font-bold text-xl mb-1">Account Created!</p>
                                    <p className="text-slate-400 text-sm">
                                        You can now login to ZapChat. Redirecting…
                                    </p>
                                </div>
                            </div>
                        )}

                        {/* ── API Error ──────────────────────────────────────── */}
                        {apiError && !success && <ErrorBanner message={apiError} />}

                        {/* ── STEP 1 — Account Details ───────────────────────── */}
                        {!success && step === 1 && (
                            <form id="register-step1-form" onSubmit={hs1(onStep1)} className="space-y-5">
                                {/* Full Name */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Full Name
                                    </label>
                                    <input
                                        {...r1("fullName", {
                                            required: "Full name is required",
                                            minLength: { value: 2, message: "Name must be at least 2 characters" },
                                        })}
                                        id="input-fullname"
                                        type="text"
                                        placeholder="John Doe"
                                        autoComplete="name"
                                        className={inputCls}
                                        style={inputStyle(!!e1.fullName)}
                                        onFocus={(e) => Object.assign(e.target.style, focusStyle)}
                                        onBlur={(e) => {
                                            e.target.style.border = e1.fullName
                                                ? "1px solid rgba(239,68,68,0.6)"
                                                : "1px solid rgba(255,255,255,0.1)";
                                            e.target.style.boxShadow = "none";
                                        }}
                                    />
                                    {e1.fullName && (
                                        <p className="text-red-400 text-xs mt-1.5">{e1.fullName.message}</p>
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
                                        onFocus={(e) => Object.assign(e.target.style, focusStyle)}
                                        onBlur={(e) => {
                                            e.target.style.border = e1.email
                                                ? "1px solid rgba(239,68,68,0.6)"
                                                : "1px solid rgba(255,255,255,0.1)";
                                            e.target.style.boxShadow = "none";
                                        }}
                                    />
                                    {e1.email && (
                                        <p className="text-red-400 text-xs mt-1.5">{e1.email.message}</p>
                                    )}
                                </div>

                                {/* Department + Branch */}
                                <div className="grid grid-cols-2 gap-4">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">
                                            Department
                                        </label>
                                        <select
                                            {...r1("department", { required: "Please select a department" })}
                                            id="input-department"
                                            className={inputCls}
                                            style={{ ...inputStyle(!!e1.department), ...selectExtraStyle }}
                                        >
                                            <option value="" style={{ background: "#0f172a" }}>Select…</option>
                                            {DEPARTMENTS.map((d) => (
                                                <option key={d} value={d} style={{ background: "#0f172a" }}>{d}</option>
                                            ))}
                                        </select>
                                        {e1.department && (
                                            <p className="text-red-400 text-xs mt-1.5">{e1.department.message}</p>
                                        )}
                                    </div>

                                    <div>
                                        <label className="block text-sm font-medium text-slate-300 mb-2">
                                            Branch
                                        </label>
                                        <select
                                            {...r1("branch", { required: "Please select a branch" })}
                                            id="input-branch"
                                            className={inputCls}
                                            style={{ ...inputStyle(!!e1.branch), ...selectExtraStyle }}
                                        >
                                            <option value="" style={{ background: "#0f172a" }}>Select…</option>
                                            {BRANCHES.map((b) => (
                                                <option key={b} value={b} style={{ background: "#0f172a" }}>{b}</option>
                                            ))}
                                        </select>
                                        {e1.branch && (
                                            <p className="text-red-400 text-xs mt-1.5">{e1.branch.message}</p>
                                        )}
                                    </div>
                                </div>

                                <button
                                    type="submit"
                                    id="step1-next"
                                    disabled={s1Submitting}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
                                    style={{
                                        background: s1Submitting
                                            ? "rgba(6,182,212,0.4)"
                                            : "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: s1Submitting ? "none" : "0 8px 25px rgba(6,182,212,0.35)",
                                        cursor: s1Submitting ? "not-allowed" : "pointer",
                                    }}
                                >
                                    {s1Submitting ? (
                                        <span className="flex items-center justify-center gap-2">
                                            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                                            </svg>
                                            Sending code…
                                        </span>
                                    ) : (
                                        "Continue →"
                                    )}
                                </button>

                                <p className="text-center text-slate-500 text-sm pt-1">
                                    Already have an account?{" "}
                                    <Link to="/" id="link-login" className="font-semibold" style={{ color: "#06b6d4" }}>
                                        Sign in
                                    </Link>
                                </p>
                            </form>
                        )}

                        {/* ── STEP 2 — Email Verification ────────────────────── */}
                        {!success && step === 2 && (
                            <div className="space-y-5">
                                {/* Resent notice */}
                                {resent && (
                                    <div
                                        className="flex items-center gap-3 px-4 py-3 rounded-xl"
                                        style={{
                                            background: "rgba(6,182,212,0.08)",
                                            border: "1px solid rgba(6,182,212,0.3)",
                                        }}
                                    >
                                        <span className="text-cyan-400">✉️</span>
                                        <p className="text-cyan-400 text-sm">A new code has been sent!</p>
                                    </div>
                                )}

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

                                {/* 6-digit boxes */}
                                <div className="flex gap-2 justify-center" onPaste={handlePaste}>
                                    {digits.map((d, i) => (
                                        <input
                                            key={i}
                                            id={`otp-digit-${i}`}
                                            ref={(el) => { inputRefs.current[i] = el; }}
                                            type="text"
                                            inputMode="numeric"
                                            maxLength={1}
                                            value={d}
                                            onChange={(e) => handleDigit(i, e.target.value)}
                                            onKeyDown={(e) => handleKeyDown(i, e)}
                                            className="w-11 h-14 text-center text-xl font-bold text-white rounded-xl outline-none transition-all duration-150"
                                            style={{
                                                background: "rgba(255,255,255,0.07)",
                                                border: d
                                                    ? "1px solid rgba(6,182,212,0.8)"
                                                    : "1px solid rgba(255,255,255,0.12)",
                                                caretColor: "#06b6d4",
                                            }}
                                            onFocus={(e) => {
                                                e.target.style.border = "1px solid rgba(6,182,212,0.9)";
                                                e.target.style.boxShadow = "0 0 0 3px rgba(6,182,212,0.15)";
                                            }}
                                            onBlur={(e) => {
                                                e.target.style.border = d
                                                    ? "1px solid rgba(6,182,212,0.8)"
                                                    : "1px solid rgba(255,255,255,0.12)";
                                                e.target.style.boxShadow = "none";
                                            }}
                                        />
                                    ))}
                                </div>

                                {/* Verify button */}
                                <button
                                    type="button"
                                    id="submit-otp"
                                    onClick={onVerifyOtp}
                                    disabled={otpLoading || secondsLeft <= 0}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
                                    style={{
                                        background: (otpLoading || secondsLeft <= 0)
                                            ? "rgba(6,182,212,0.4)"
                                            : "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: (otpLoading || secondsLeft <= 0) ? "none" : "0 8px 25px rgba(6,182,212,0.35)",
                                        cursor: (otpLoading || secondsLeft <= 0) ? "not-allowed" : "pointer",
                                    }}
                                >
                                    {otpLoading ? (
                                        <span className="flex items-center justify-center gap-2">
                                            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                                            </svg>
                                            Verifying…
                                        </span>
                                    ) : secondsLeft <= 0 ? (
                                        "Code expired"
                                    ) : (
                                        "Verify Email →"
                                    )}
                                </button>

                                {/* Resend + Back */}
                                <div className="flex items-center justify-between text-sm">
                                    <button
                                        type="button"
                                        id="resend-code"
                                        onClick={onResend}
                                        disabled={resending || resendCooldown > 0}
                                        className="transition-colors"
                                        style={{
                                            color: resendCooldown > 0 || resending ? "#334155" : "#06b6d4",
                                            cursor: resendCooldown > 0 || resending ? "not-allowed" : "pointer",
                                        }}
                                    >
                                        {resending
                                            ? "Sending…"
                                            : resendCooldown > 0
                                            ? `Resend in 0:${String(resendCooldown).padStart(2, "0")}`
                                            : "Resend Code"}
                                    </button>
                                    <button
                                        type="button"
                                        id="step2-back"
                                        onClick={() => { setStep(1); setApiError(null); }}
                                        className="transition-colors"
                                        style={{ color: "#475569" }}
                                        onMouseEnter={(e) => ((e.target as HTMLElement).style.color = "#94a3b8")}
                                        onMouseLeave={(e) => ((e.target as HTMLElement).style.color = "#475569")}
                                    >
                                        ← Back
                                    </button>
                                </div>
                            </div>
                        )}

                        {/* ── STEP 3 — Set Password ──────────────────────────── */}
                        {!success && step === 3 && (
                            <form id="register-step3-form" onSubmit={hs3(onStep3)} className="space-y-5">
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

                                {/* Password */}
                                <div>
                                    <label className="block text-sm font-medium text-slate-300 mb-2">
                                        Password
                                    </label>
                                    <div className="relative">
                                        <input
                                            {...r3("password", {
                                                required: "Password is required",
                                                minLength: { value: 6, message: "Minimum 6 characters" },
                                                onChange: (e) => setPwValue(e.target.value),
                                            })}
                                            id="input-reg-password"
                                            type={showPassword ? "text" : "password"}
                                            placeholder="Create a password"
                                            autoComplete="new-password"
                                            className={inputCls + " pr-12"}
                                            style={inputStyle(!!e3.password)}
                                            onFocus={(e) => Object.assign(e.target.style, focusStyle)}
                                            onBlur={(e) => {
                                                e.target.style.border = e3.password
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
                                    {e3.password && (
                                        <p className="text-red-400 text-xs mt-1.5">{e3.password.message}</p>
                                    )}

                                    {/* Requirement indicator */}
                                    {pwValue.length > 0 && (
                                        <div className="mt-2.5 flex items-center gap-2 text-xs">
                                            <span style={{ color: pwValue.length >= 6 ? "#22c55e" : "#64748b" }}>
                                                {pwValue.length >= 6 ? "✓" : "○"}
                                            </span>
                                            <span style={{ color: pwValue.length >= 6 ? "#22c55e" : "#64748b" }}>
                                                At least 6 characters
                                            </span>
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
                                            {...r3("confirmPassword", {
                                                required: "Please confirm your password",
                                                validate: (v) => v === passwordWatch || "Passwords do not match",
                                            })}
                                            id="input-confirm-password"
                                            type={showConfirm ? "text" : "password"}
                                            placeholder="Repeat your password"
                                            autoComplete="new-password"
                                            className={inputCls + " pr-12"}
                                            style={inputStyle(!!e3.confirmPassword)}
                                            onFocus={(e) => Object.assign(e.target.style, focusStyle)}
                                            onBlur={(e) => {
                                                e.target.style.border = e3.confirmPassword
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
                                    {e3.confirmPassword && (
                                        <p className="text-red-400 text-xs mt-1.5">{e3.confirmPassword.message}</p>
                                    )}
                                </div>

                                {/* Create Account */}
                                <button
                                    type="submit"
                                    id="submit-register"
                                    disabled={s3Submitting}
                                    className="w-full py-3.5 rounded-xl font-semibold text-sm text-white transition-all duration-200"
                                    style={{
                                        background: s3Submitting
                                            ? "rgba(6,182,212,0.4)"
                                            : "linear-gradient(135deg, #0ea5e9, #06b6d4)",
                                        boxShadow: s3Submitting ? "none" : "0 8px 25px rgba(6,182,212,0.35)",
                                        cursor: s3Submitting ? "not-allowed" : "pointer",
                                    }}
                                >
                                    {s3Submitting ? (
                                        <span className="flex items-center justify-center gap-2">
                                            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                                            </svg>
                                            Creating Account…
                                        </span>
                                    ) : (
                                        "🚀 Create Account"
                                    )}
                                </button>

                                <div className="flex items-center justify-between text-sm">
                                    <p className="text-slate-500">
                                        Already have an account?{" "}
                                        <Link to="/" id="link-login-step3" className="font-semibold" style={{ color: "#06b6d4" }}>
                                            Sign in
                                        </Link>
                                    </p>
                                    <button
                                        type="button"
                                        id="step3-back"
                                        onClick={() => { setStep(2); setApiError(null); }}
                                        className="transition-colors"
                                        style={{ color: "#475569" }}
                                        onMouseEnter={(e) => ((e.target as HTMLElement).style.color = "#94a3b8")}
                                        onMouseLeave={(e) => ((e.target as HTMLElement).style.color = "#475569")}
                                    >
                                        ← Back
                                    </button>
                                </div>
                            </form>
                        )}
                    </div>

                    {/* Footer */}
                    <p className="text-center text-slate-600 text-xs mt-6">
                        By creating an account, you agree to ZapChat{" "}
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