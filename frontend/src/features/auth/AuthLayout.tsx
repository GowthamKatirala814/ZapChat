import { EyeOff, Lock, Users } from "lucide-react";
import type { ReactNode } from "react";

/**
 * The frame for login, registration and password reset.
 *
 * The right panel exists to answer the question the product creates: "if I sign in with
 * my work email, is my name attached to what I post?" It is answered here, before the
 * user commits, rather than in a tooltip they will discover afterwards.
 */
export function AuthLayout({
  title,
  subtitle,
  children,
  footer,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="min-h-dvh flex bg-bg">
      <div className="flex-1 flex items-center justify-center p-5 sm:p-8">
        <div className="w-full max-w-[400px]">
          <div className="flex items-center gap-2.5 mb-8">
            <span
              className="w-8 h-8 rounded-[--radius-sm] flex items-center justify-center text-accent-contrast font-bold"
              style={{
                background: "linear-gradient(135deg, var(--zc-accent), var(--zc-room-branch))",
              }}
              aria-hidden
            >
              Z
            </span>
            <span className="font-display font-semibold text-[17px] text-body">ZapChat</span>
          </div>

          <h1 className="font-display text-[26px] font-semibold text-body leading-tight">{title}</h1>
          {subtitle && <p className="text-[14px] text-muted mt-2 leading-relaxed">{subtitle}</p>}

          <div className="mt-7">{children}</div>

          {footer && <div className="mt-6 text-[13.5px] text-muted">{footer}</div>}
        </div>
      </div>

      <aside
        className="hidden lg:flex flex-col justify-center w-[46%] max-w-[560px] p-12 border-l border-line"
        style={{
          background:
            "linear-gradient(160deg, var(--zc-surface) 0%, var(--zc-surface-2) 55%, var(--zc-accent-soft) 100%)",
        }}
      >
        <h2 className="font-display text-[22px] font-semibold text-body leading-snug max-w-sm">
          Speak freely at work, without your name on it.
        </h2>

        <div className="mt-8 flex flex-col gap-6 max-w-md">
          <Point
            icon={<Lock size={17} />}
            title="You sign in as yourself"
            body="Your work email verifies that you belong here, and decides which office channels you can open. That part is never anonymous."
          />
          <Point
            icon={<EyeOff size={17} />}
            title="You post as a pseudonym"
            body="Everyone sees a generated name instead of yours. Your real name, email and department are never attached to a message."
          />
          <Point
            icon={<Users size={17} />}
            title="Moderation still applies"
            body="Anonymous is not unaccountable. Reported content is reviewed, and repeated abuse can be traced back by an administrator."
          />
        </div>
      </aside>
    </div>
  );
}

function Point({ icon, title, body }: { icon: ReactNode; title: string; body: string }) {
  return (
    <div className="flex gap-3.5">
      <span className="w-8 h-8 rounded-[--radius-DEFAULT] bg-surface border border-line flex items-center justify-center text-accent shrink-0">
        {icon}
      </span>
      <div>
        <p className="text-[14px] font-medium text-body">{title}</p>
        <p className="text-[13px] text-muted mt-1 leading-relaxed">{body}</p>
      </div>
    </div>
  );
}
