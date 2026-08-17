import { CheckCircle2, ChevronLeft, MailCheck } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ErrorState } from "../../components/feedback";
import { Button, Field, Input } from "../../components/ui";
import { paths } from "../../config";
import { authApi } from "../../services/api";
import { AuthLayout } from "./AuthLayout";
import { OtpInput } from "./OtpInput";
import { useResendCountdown } from "./useResendCountdown";
import { MIN_PASSWORD_LENGTH, OTP_EXPIRY_MINUTES } from "./constants";

/**
 * Password reset — the same three-step shape as registration:
 * `forgot-password` → `verify-otp` → `reset-password`.
 *
 * Note the deliberate asymmetry with registration: step 1 always reports success, because
 * the server does not disclose whether an address has an account. The old UI showed
 * "No account found with that email", which turned the reset form into a way to test
 * whether a colleague had signed up.
 */

type Step = "email" | "code" | "password" | "done";

export function ForgotPasswordPage() {
  const navigate = useNavigate();

  const [step, setStep] = useState<Step>("email");
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [resetToken, setResetToken] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const { secondsLeft, canResend, start } = useResendCountdown();

  async function run(action: () => Promise<void>) {
    setError(null);
    setBusy(true);
    try {
      await action();
    } catch (caught) {
      setError(caught);
    } finally {
      setBusy(false);
    }
  }

  const submitEmail = (event: FormEvent) => {
    event.preventDefault();
    void run(async () => {
      await authApi.forgotPassword(email.trim());
      setStep("code");
      start();
    });
  };

  const submitCode = (value = code) =>
    void run(async () => {
      const result = await authApi.verifyResetOtp(email.trim(), value);

      if (!result.token) throw new Error("The server did not return a reset token.");

      setResetToken(result.token);
      setStep("password");
    });

  const submitPassword = (event: FormEvent) => {
    event.preventDefault();
    void run(async () => {
      await authApi.resetPassword(resetToken, password, confirmPassword);
      setStep("done");
    });
  };

  const passwordProblem =
    password.length > 0 && password.length < MIN_PASSWORD_LENGTH
      ? `Use at least ${MIN_PASSWORD_LENGTH} characters.`
      : confirmPassword.length > 0 && password !== confirmPassword
        ? "The two passwords do not match."
        : undefined;

  if (step === "done") {
    return (
      <AuthLayout title="Password changed" subtitle="You can sign in with your new password.">
        <div className="flex flex-col gap-5">
          <div className="flex items-start gap-3 p-4 rounded-[--radius-DEFAULT] bg-success-soft border border-success/25">
            <CheckCircle2 size={18} className="text-success shrink-0 mt-0.5" />
            <p className="text-[13.5px] text-body">
              Any other devices signed in to your account have been signed out.
            </p>
          </div>

          <Button size="lg" className="w-full justify-center" onClick={() => navigate(paths.login)}>
            Continue to sign in
          </Button>
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout
      title="Reset your password"
      subtitle={
        step === "email"
          ? "Enter your work email and we will send you a verification code."
          : step === "code"
            ? `If an account exists for ${email}, a 6-digit code is on its way.`
            : "Choose a new password."
      }
      footer={
        <Link
          to={paths.login}
          className="inline-flex items-center gap-1 text-muted hover:text-body"
        >
          <ChevronLeft size={14} />
          Back to sign in
        </Link>
      }
    >
      <div className="flex flex-col gap-4">
        {error != null && <ErrorState error={error} compact />}

        {step === "email" && (
          <form onSubmit={submitEmail} className="flex flex-col gap-4" noValidate>
            <Field label="Work email" htmlFor="resetEmail" required>
              <Input
                id="resetEmail"
                type="email"
                required
                autoFocus
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@zapcg.com"
              />
            </Field>

            <Button type="submit" size="lg" loading={busy} className="w-full justify-center">
              Send code
            </Button>
          </form>
        )}

        {step === "code" && (
          <div className="flex flex-col gap-4">
            <div className="flex items-start gap-3 p-3.5 rounded-[--radius-DEFAULT] bg-surface-2 border border-line">
              <MailCheck size={17} className="text-muted shrink-0 mt-0.5" />
              <div className="text-[13px] text-body">
                <p className="font-medium">Check your email</p>
                <p className="text-muted mt-0.5">
                  If an account exists for <span className="text-body">{email}</span>, a
                  6-digit code is on its way. It expires in {OTP_EXPIRY_MINUTES} minutes.
                </p>
              </div>
            </div>

            <OtpInput value={code} onChange={setCode} disabled={busy} onComplete={submitCode} />

            <Button
              size="lg"
              loading={busy}
              disabled={code.length !== 6}
              className="w-full justify-center"
              onClick={() => submitCode()}
            >
              Verify code
            </Button>

            <div className="flex items-center justify-between gap-3">
              <button
                type="button"
                onClick={() => setStep("email")}
                className="inline-flex items-center gap-1 text-[13px] text-muted hover:text-body"
              >
                <ChevronLeft size={14} />
                Use a different email
              </button>

              {/* The countdown is purely local here. The server answers a throttled
                  reset request with the same sentence as an accepted one — a 429 would
                  confirm the address has an account — so there is no Retry-After to
                  read, and the client keeps its own timer. */}
              <button
                type="button"
                onClick={() => {
                  void run(async () => {
                    await authApi.forgotPassword(email.trim());
                    setCode("");
                    start();
                  });
                }}
                disabled={busy || !canResend}
                className="text-[13px] text-accent hover:underline disabled:text-faint disabled:no-underline disabled:cursor-not-allowed"
              >
                {canResend ? "Resend code" : `Resend in ${secondsLeft}s`}
              </button>
            </div>

            <p className="text-[12px] text-faint">
              Not in your inbox? Check the spam folder. If you never receive a code, the
              address may not have an account.
            </p>
          </div>
        )}

        {step === "password" && (
          <form onSubmit={submitPassword} className="flex flex-col gap-4" noValidate>
            <Field
              label="New password"
              htmlFor="resetPassword"
              required
              hint={`At least ${MIN_PASSWORD_LENGTH} characters.`}
            >
              <Input
                id="resetPassword"
                type="password"
                autoComplete="new-password"
                required
                autoFocus
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </Field>

            <Field
              label="Confirm new password"
              htmlFor="resetConfirm"
              required
              error={passwordProblem}
            >
              <Input
                id="resetConfirm"
                type="password"
                autoComplete="new-password"
                required
                invalid={Boolean(passwordProblem)}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
              />
            </Field>

            <Button
              type="submit"
              size="lg"
              loading={busy}
              disabled={Boolean(passwordProblem) || password.length < MIN_PASSWORD_LENGTH}
              className="w-full justify-center"
            >
              Change password
            </Button>
          </form>
        )}
      </div>
    </AuthLayout>
  );
}
