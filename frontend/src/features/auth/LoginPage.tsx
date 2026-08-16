import { useState, type FormEvent } from "react";
import { Eye, EyeOff } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../app/providers";
import { ErrorState } from "../../components/feedback";
import { Button, Field, Input } from "../../components/ui";
import { paths } from "../../config";
import { AuthLayout } from "./AuthLayout";

export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [submitting, setSubmitting] = useState(false);

  /** Where the user was headed before the guard intercepted them. */
  const destination =
    (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? paths.chat;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      await signIn(email.trim(), password);
      navigate(destination, { replace: true });
    } catch (caught) {
      // The server's own message is shown: it distinguishes bad credentials from a
      // disabled account, and a user locked out by moderation needs to know which.
      setError(caught);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout
      title="Sign in"
      subtitle="Use your work email. Your colleagues will only ever see your anonymous name."
      footer={
        <>
          New here?{" "}
          <Link to={paths.register} className="text-accent font-medium hover:underline">
            Create an account
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
        {error != null && <ErrorState error={error} compact />}

        <Field label="Work email" htmlFor="email" required>
          <Input
            id="email"
            type="email"
            autoComplete="username"
            autoFocus
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@zapcg.com"
          />
        </Field>

        <Field label="Password" htmlFor="password" required>
          <div className="relative">
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="pr-10"
            />
            <button
              type="button"
              onClick={() => setShowPassword((v) => !v)}
              className="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-faint hover:text-body rounded-[--radius-sm]"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
        </Field>

        <div className="flex justify-end -mt-1">
          <Link
            to={paths.forgotPassword}
            className="text-[13px] text-muted hover:text-accent hover:underline"
          >
            Forgot your password?
          </Link>
        </div>

        <Button type="submit" size="lg" loading={submitting} className="w-full justify-center mt-1">
          Sign in
        </Button>
      </form>
    </AuthLayout>
  );
}
