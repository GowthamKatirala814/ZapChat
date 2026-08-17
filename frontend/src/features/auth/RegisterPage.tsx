import { CheckCircle2, ChevronLeft, MailCheck } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ErrorState } from "../../components/feedback";
import { Button, Field, Input, Select } from "../../components/ui";
import { paths } from "../../config";
import { authApi } from "../../services/api";
import { ApiError } from "../../services/api";
import { AuthLayout } from "./AuthLayout";
import { OtpInput } from "./OtpInput";
import { useResendCountdown } from "./useResendCountdown";
import { BRANCHES, DEPARTMENTS, MIN_PASSWORD_LENGTH, OTP_EXPIRY_MINUTES } from "./constants";

/**
 * Three-step registration, matching the server exactly:
 *
 *   1. `register/initiate`  — details are validated and a code is emailed. No account yet.
 *   2. `register/verify-otp` — the code is exchanged for a one-time verification token.
 *   3. `register/complete`   — the token plus a password creates the account.
 *
 * The token from step 2 is held in component state only. It is single-use and short-lived,
 * so persisting it would create a credential on disk for no benefit; a refresh mid-flow
 * correctly restarts from step 1.
 */

type Step = "details" | "code" | "password" | "done";

export function RegisterPage() {
  const navigate = useNavigate();

  const [step, setStep] = useState<Step>("details");
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [department, setDepartment] = useState<string>(DEPARTMENTS[0]);
  const [branch, setBranch] = useState<string>(BRANCHES[0]);

  const [code, setCode] = useState("");
  const [verificationToken, setVerificationToken] = useState("");
  const { secondsLeft, canResend, start, startFromError } = useResendCountdown();

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

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

  const submitDetails = (event: FormEvent) => {
    event.preventDefault();
    void run(async () => {
      await authApi.registerInitiate({
        fullName: fullName.trim(),
        email: email.trim(),
        department: department.trim(),
        branch,
      });

      // Reached only when the server actually handed the message to the mail provider —
      // it throws otherwise, so advancing here does not promise something untrue.
      setStep("code");
      start();
    });
  };

  const submitCode = (value = code) => {
    void run(async () => {
      const result = await authApi.registerVerify(email.trim(), value);

      if (!result.token) throw new Error("The server did not return a verification token.");

      setVerificationToken(result.token);
      setStep("password");
    });
  };

  const submitPassword = (event: FormEvent) => {
    event.preventDefault();
    void run(async () => {
      await authApi.registerComplete(verificationToken, password, confirmPassword);
      setStep("done");
    });
  };

  const resend = () =>
    void run(async () => {
      try {
        await authApi.registerInitiate({
          fullName: fullName.trim(),
          email: email.trim(),
          department: department.trim(),
          branch,
        });

        setCode("");
        start();
      } catch (caught) {
        // A 429 means the server's own per-mailbox cooldown is still running. Sync the
        // local timer to its Retry-After rather than showing a red error for something
        // that is only a matter of waiting.
        if (ApiError.from(caught)?.isRateLimited) {
          startFromError(caught);
          return;
        }
        throw caught;
      }
    });

  const passwordProblem =
    password.length > 0 && password.length < MIN_PASSWORD_LENGTH
      ? `Use at least ${MIN_PASSWORD_LENGTH} characters.`
      : confirmPassword.length > 0 && password !== confirmPassword
        ? "The two passwords do not match."
        : undefined;

  if (step === "done") {
    return (
      <AuthLayout
        title="Your account is ready"
        subtitle="You can sign in now. From this point on, your colleagues will only see your anonymous name."
      >
        <div className="flex flex-col gap-5">
          <div className="flex items-start gap-3 p-4 rounded-[--radius-DEFAULT] bg-success-soft border border-success/25">
            <CheckCircle2 size={18} className="text-success shrink-0 mt-0.5" />
            <p className="text-[13.5px] text-body">
              A pseudonym has been generated for you. You will see it as soon as you sign in — it
              is the only name anyone else in ZapChat can see.
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
      title="Create your account"
      subtitle={
        step === "details"
          ? "Your details verify that you work here and decide which office channels you can open."
          : step === "code"
            ? `We sent a 6-digit code to ${email}.`
            : "Last step — choose a password."
      }
      footer={
        step === "details" ? (
          <>
            Already have an account?{" "}
            <Link to={paths.login} className="text-accent font-medium hover:underline">
              Sign in
            </Link>
          </>
        ) : null
      }
    >
      <StepIndicator step={step} />

      <div className="mt-6 flex flex-col gap-4">
        {error != null && <ErrorState error={error} compact />}

        {step === "details" && (
          <form onSubmit={submitDetails} className="flex flex-col gap-4" noValidate>
            <Field label="Full name" htmlFor="fullName" required hint="Used for verification only — never shown to other users.">
              <Input
                id="fullName"
                required
                autoFocus
                maxLength={200}
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
              />
            </Field>

            <Field label="Work email" htmlFor="regEmail" required>
              <Input
                id="regEmail"
                type="email"
                required
                maxLength={256}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@zapcg.com"
              />
            </Field>

            <div className="grid sm:grid-cols-2 gap-4">
              <Field label="Department" htmlFor="department" required>
                <Select
                  id="department"
                  value={department}
                  onChange={(e) => setDepartment(e.target.value)}
                >
                  {DEPARTMENTS.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field
                label="Office"
                htmlFor="branch"
                required
                hint="Decides your branch channel."
              >
                <Select id="branch" value={branch} onChange={(e) => setBranch(e.target.value)}>
                  {BRANCHES.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>

            <Button type="submit" size="lg" loading={busy} className="w-full justify-center mt-1">
              Send verification code
            </Button>
          </form>
        )}

        {step === "code" && (
          <div className="flex flex-col gap-4">
            <div className="flex items-start gap-3 p-3.5 rounded-[--radius-DEFAULT] bg-success-soft border border-success/25">
              <MailCheck size={17} className="text-success shrink-0 mt-0.5" />
              <div className="text-[13px] text-body">
                <p className="font-medium">Verification email sent</p>
                <p className="text-muted mt-0.5">
                  We sent a 6-digit code to <span className="text-body">{email}</span>. It
                  expires in {OTP_EXPIRY_MINUTES} minutes and can be used once.
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
                onClick={() => setStep("details")}
                className="inline-flex items-center gap-1 text-[13px] text-muted hover:text-body"
              >
                <ChevronLeft size={14} />
                Change details
              </button>

              {/* Disabled during the server's per-mailbox cooldown, so the usual
                  response to "it has not arrived yet" is a wait rather than a 429. */}
              <button
                type="button"
                onClick={resend}
                disabled={busy || !canResend}
                className="text-[13px] text-accent hover:underline disabled:text-faint disabled:no-underline disabled:cursor-not-allowed"
              >
                {canResend ? "Resend code" : `Resend in ${secondsLeft}s`}
              </button>
            </div>

            <p className="text-[12px] text-faint">
              Not in your inbox? Check the spam folder — the message comes from an automated
              address.
            </p>
          </div>
        )}

        {step === "password" && (
          <form onSubmit={submitPassword} className="flex flex-col gap-4" noValidate>
            <Field
              label="Password"
              htmlFor="newPassword"
              required
              hint={`At least ${MIN_PASSWORD_LENGTH} characters.`}
            >
              <Input
                id="newPassword"
                type="password"
                autoComplete="new-password"
                required
                autoFocus
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </Field>

            <Field label="Confirm password" htmlFor="confirmPassword" required error={passwordProblem}>
              <Input
                id="confirmPassword"
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
              className="w-full justify-center mt-1"
            >
              Create account
            </Button>
          </form>
        )}
      </div>
    </AuthLayout>
  );
}

function StepIndicator({ step }: { step: Step }) {
  const steps: Array<{ key: Step; label: string }> = [
    { key: "details", label: "Details" },
    { key: "code", label: "Verify" },
    { key: "password", label: "Password" },
  ];

  const currentIndex = steps.findIndex((s) => s.key === step);

  return (
    <ol className="flex items-center gap-2" aria-label="Registration progress">
      {steps.map((item, index) => {
        const state = index < currentIndex ? "done" : index === currentIndex ? "current" : "todo";

        return (
          <li key={item.key} className="flex-1 flex flex-col gap-1.5">
            <span
              className="h-1 rounded-[--radius-full] transition-colors"
              style={{
                background:
                  state === "todo" ? "var(--zc-border)" : "var(--zc-accent)",
              }}
              aria-hidden
            />
            <span
              className="text-[11.5px] font-medium"
              style={{
                color: state === "todo" ? "var(--zc-text-3)" : "var(--zc-accent-text)",
              }}
              aria-current={state === "current" ? "step" : undefined}
            >
              {item.label}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
