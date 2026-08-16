import { clsx } from "clsx";
import { forwardRef, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type TextareaHTMLAttributes } from "react";
import { Loader2 } from "lucide-react";

/**
 * The UI primitives. Every screen composes from these, so login, chat, polls and the
 * admin console share one visual language rather than three.
 */

// ── Button ────────────────────────────────────────────────────────────────────

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger" | "subtle";
type ButtonSize = "sm" | "md" | "lg" | "icon";

const buttonVariants: Record<ButtonVariant, string> = {
  primary:
    "bg-accent text-accent-contrast hover:bg-accent-hover shadow-sm disabled:bg-line-strong",
  secondary:
    "bg-surface text-body border border-line hover:bg-surface-2 hover:border-line-strong",
  ghost: "text-muted hover:bg-surface-2 hover:text-body",
  danger: "bg-danger text-white hover:opacity-90 shadow-sm",
  subtle: "bg-surface-2 text-body hover:bg-surface-3",
};

const buttonSizes: Record<ButtonSize, string> = {
  sm: "h-8 px-3 text-[13px] gap-1.5 rounded-[--radius-sm]",
  md: "h-9 px-4 text-sm gap-2 rounded-[--radius-DEFAULT]",
  lg: "h-11 px-5 text-[15px] gap-2 rounded-[--radius-DEFAULT]",
  icon: "h-8 w-8 rounded-[--radius-sm] justify-center",
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  icon?: ReactNode;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = "primary", size = "md", loading, icon, children, className, disabled, ...rest },
  ref,
) {
  return (
    <button
      ref={ref}
      // `loading` implies disabled so a double submit is impossible.
      disabled={disabled || loading}
      className={clsx(
        "inline-flex items-center font-medium transition-colors select-none",
        "disabled:opacity-60 disabled:cursor-not-allowed",
        buttonVariants[variant],
        buttonSizes[size],
        className,
      )}
      {...rest}
    >
      {loading ? <Loader2 size={15} className="zc-spin shrink-0" /> : icon}
      {children}
    </button>
  );
});

// ── Inputs ────────────────────────────────────────────────────────────────────

const fieldBase =
  "w-full bg-surface border border-line rounded-[--radius-DEFAULT] px-3 " +
  "text-body placeholder:text-faint transition-colors " +
  "focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20 " +
  "disabled:bg-surface-2 disabled:cursor-not-allowed";

interface FieldProps {
  label?: string;
  hint?: string;
  error?: string;
  required?: boolean;
}

/** Wraps a control with its label, hint and error so every form looks the same. */
export function Field({
  label,
  hint,
  error,
  required,
  htmlFor,
  children,
}: FieldProps & { htmlFor?: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={htmlFor} className="text-[13px] font-medium text-body">
          {label}
          {required && <span className="text-danger ml-0.5">*</span>}
        </label>
      )}
      {children}
      {error ? (
        <p className="text-[12.5px] text-danger" role="alert">
          {error}
        </p>
      ) : hint ? (
        <p className="text-[12.5px] text-faint">{hint}</p>
      ) : null}
    </div>
  );
}

export const Input = forwardRef<
  HTMLInputElement,
  InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }
>(function Input({ className, invalid, ...rest }, ref) {
  return (
    <input
      ref={ref}
      aria-invalid={invalid || undefined}
      className={clsx(fieldBase, "h-10", invalid && "border-danger", className)}
      {...rest}
    />
  );
});

export const Textarea = forwardRef<
  HTMLTextAreaElement,
  TextareaHTMLAttributes<HTMLTextAreaElement> & { invalid?: boolean }
>(function Textarea({ className, invalid, ...rest }, ref) {
  return (
    <textarea
      ref={ref}
      aria-invalid={invalid || undefined}
      className={clsx(fieldBase, "py-2 resize-none", invalid && "border-danger", className)}
      {...rest}
    />
  );
});

export const Select = forwardRef<
  HTMLSelectElement,
  React.SelectHTMLAttributes<HTMLSelectElement>
>(function Select({ className, children, ...rest }, ref) {
  return (
    <select ref={ref} className={clsx(fieldBase, "h-10 pr-8", className)} {...rest}>
      {children}
    </select>
  );
});

// ── Surfaces ──────────────────────────────────────────────────────────────────

export function Card({
  children,
  className,
  padded = true,
}: {
  children: ReactNode;
  className?: string;
  padded?: boolean;
}) {
  return (
    <div
      className={clsx(
        "bg-surface border border-line rounded-[--radius-lg] shadow-sm",
        padded && "p-5",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function CardHeader({
  title,
  description,
  action,
}: {
  title: ReactNode;
  description?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="flex items-start justify-between gap-4 mb-4">
      <div className="min-w-0">
        <h3 className="text-[15px] font-semibold text-body">{title}</h3>
        {description && <p className="text-[13px] text-faint mt-0.5">{description}</p>}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  );
}

// ── Badge ─────────────────────────────────────────────────────────────────────

type BadgeTone = "neutral" | "accent" | "success" | "warning" | "danger" | "info";

const badgeTones: Record<BadgeTone, string> = {
  neutral: "bg-surface-2 text-muted border-line",
  accent: "bg-accent-soft text-accent-text border-accent/25",
  success: "bg-success-soft text-success border-success/25",
  warning: "bg-warning-soft text-warning border-warning/25",
  danger: "bg-danger-soft text-danger border-danger/25",
  info: "bg-info-soft text-info border-info/25",
};

export function Badge({
  children,
  tone = "neutral",
  className,
}: {
  children: ReactNode;
  tone?: BadgeTone;
  className?: string;
}) {
  return (
    <span
      className={clsx(
        "inline-flex items-center gap-1 px-2 py-0.5 rounded-[--radius-sm] border",
        "text-[11px] font-medium leading-5 whitespace-nowrap",
        badgeTones[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

/** Unread pill. Renders nothing at zero so callers need no conditional. */
export function CountBadge({ count, max = 99 }: { count: number; max?: number }) {
  if (count <= 0) return null;

  return (
    <span
      className={clsx(
        "min-w-[18px] h-[18px] px-1.5 rounded-[--radius-full]",
        "bg-accent text-accent-contrast",
        "text-[11px] font-semibold leading-[18px] text-center zc-tabular",
      )}
      aria-label={`${count} unread`}
    >
      {count > max ? `${max}+` : count}
    </span>
  );
}

// ── Avatar ────────────────────────────────────────────────────────────────────

/**
 * Deterministic colour from the anonymous name, so the same pseudonym always looks the
 * same. Hue only — saturation and lightness are fixed so every avatar sits at the same
 * visual weight.
 */
function hueFor(name: string): number {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = (hash * 31 + name.charCodeAt(i)) | 0;
  return Math.abs(hash) % 360;
}

export function Avatar({
  name,
  size = 32,
  online,
  className,
}: {
  name: string;
  size?: number;
  online?: boolean;
  className?: string;
}) {
  const hue = hueFor(name || "?");
  const initials = (name || "?").replace(/[^A-Za-z]/g, "").slice(0, 2).toUpperCase() || "?";

  return (
    <span className={clsx("relative inline-flex shrink-0", className)}>
      <span
        aria-hidden
        className="inline-flex items-center justify-center rounded-[--radius-full] font-semibold text-white"
        style={{
          width: size,
          height: size,
          fontSize: Math.round(size * 0.38),
          background: `linear-gradient(135deg, hsl(${hue} 62% 52%), hsl(${(hue + 28) % 360} 58% 42%))`,
        }}
      >
        {initials}
      </span>
      {online !== undefined && (
        <span
          className={clsx(
            "absolute bottom-0 right-0 rounded-[--radius-full] border-2 border-surface",
            online ? "bg-success" : "bg-line-strong",
          )}
          style={{ width: Math.max(8, size * 0.3), height: Math.max(8, size * 0.3) }}
          aria-label={online ? "Online" : "Offline"}
        />
      )}
    </span>
  );
}

// ── Modal ─────────────────────────────────────────────────────────────────────

export function Modal({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  width = 460,
}: {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  description?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
  width?: number;
}) {
  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4"
      style={{ background: "var(--zc-overlay)" }}
      onClick={onClose}
      role="presentation"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={typeof title === "string" ? title : undefined}
        className={clsx(
          "bg-surface border border-line w-full",
          "rounded-t-[--radius-lg] sm:rounded-[--radius-lg]",
          "shadow-lg zc-enter max-h-[90vh] overflow-y-auto",
        )}
        style={{ maxWidth: width }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="px-5 pt-5 pb-4">
          <h2 className="text-base font-semibold text-body">{title}</h2>
          {description && <p className="text-[13px] text-faint mt-1">{description}</p>}
        </div>

        <div className="px-5 pb-5">{children}</div>

        {footer && (
          <div className="px-5 py-4 border-t border-line-subtle bg-surface-2 flex justify-end gap-2 rounded-b-[--radius-lg]">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}

// ── Tooltip-ish label ─────────────────────────────────────────────────────────

export function SectionLabel({ children }: { children: ReactNode }) {
  return (
    <div className="text-[11px] font-semibold uppercase tracking-[0.08em] text-faint px-2 mb-1.5">
      {children}
    </div>
  );
}

export function Divider({ className }: { className?: string }) {
  return <hr className={clsx("border-0 border-t border-line-subtle", className)} />;
}
