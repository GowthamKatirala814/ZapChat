import { useMutation } from "@tanstack/react-query";
import {
  BellRing, Building2, Check, EyeOff, Info, LogOut, Mail, Monitor, Moon, ShieldCheck, Sun,
  UserRound,
} from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { useAuth, useCurrentUser, useTheme, type ThemePreference } from "../../app/providers";
import { Page, PageBody, PageHeader } from "../../components/layout/ListDetail";
import { Avatar, Badge, Button, Card, CardHeader, Field, Select } from "../../components/ui";
import { ErrorState } from "../../components/feedback";
import { formatDate } from "../../lib/format";
import { authApi } from "../../services/api";
import { DEPARTMENTS } from "../auth/constants";
import { usePushNotifications } from "../notifications/usePush";

/**
 * Your profile.
 *
 * This is the one screen in the product where a real identity is visible, and it is
 * visible only to its owner — `GET /api/auth/me` is the only endpoint that returns an
 * email or a full name, and only ever for the caller. The page says so explicitly,
 * because "am I actually anonymous?" is the question the product has to answer
 * convincingly.
 */
export function ProfilePage() {
  const user = useCurrentUser();
  const { signOut, isAdmin, refreshProfile } = useAuth();

  const [department, setDepartment] = useState(user.department);

  const save = useMutation({
    mutationFn: () => authApi.updateProfile(department),
    onSuccess: async () => {
      await refreshProfile();
      toast.success("Profile updated.");
    },
  });

  const changed = department !== user.department;

  return (
    <Page>
      <PageHeader title="Your profile" description="Who you are here, and who others see" />

      <PageBody width="narrow">
        <div className="flex flex-col gap-4">
          {/* ── Identity ───────────────────────────────────────────────────── */}
          <Card>
            <div className="flex items-center gap-4">
              <Avatar name={user.anonymousName} size={56} />

              <div className="min-w-0">
                <h2 className="font-display text-[19px] font-semibold text-body truncate">
                  {user.anonymousName}
                </h2>
                <p className="text-[13px] text-muted mt-0.5">
                  This is the name everyone else in ZapChat sees.
                </p>
              </div>
            </div>

            <div className="mt-4 flex items-start gap-2.5 p-3 rounded-[--radius-DEFAULT] bg-accent-soft border border-accent/20">
              <EyeOff size={16} className="text-accent-text shrink-0 mt-0.5" />
              <p className="text-[12.5px] text-body leading-relaxed">
                Your pseudonym is assigned when you register and cannot be changed — that
                is what stops it being used to signal who you are. Nobody else can see the
                details below.
              </p>
            </div>
          </Card>

          {/* ── Private details ────────────────────────────────────────────── */}
          <Card>
            <CardHeader
              title="Private details"
              description="Visible only to you. Used to verify your account and decide which channels you can open."
            />

            <dl className="flex flex-col gap-3">
              <Detail icon={<UserRound size={15} />} label="Full name" value={user.fullName} />
              <Detail icon={<Mail size={15} />} label="Work email" value={user.email} />
              <Detail
                icon={<Building2 size={15} />}
                label="Office"
                value={user.branch}
                hint="Decides which branch channel you can read. Only an administrator can change it."
              />
              <Detail
                icon={<Info size={15} />}
                label="Member since"
                value={formatDate(user.createdAt)}
              />
            </dl>

            {isAdmin && (
              <div className="mt-4 pt-4 border-t border-line-subtle flex items-center gap-2">
                <Badge tone="accent">
                  <ShieldCheck size={11} />
                  Administrator
                </Badge>
                <span className="text-[12.5px] text-faint">
                  You can access the moderation queue and admin console.
                </span>
              </div>
            )}
          </Card>

          {/* ── Editable ───────────────────────────────────────────────────── */}
          <Card>
            <CardHeader
              title="Department"
              description="The only profile field you can change yourself."
            />

            {save.error != null && (
              <div className="mb-3">
                <ErrorState error={save.error} compact />
              </div>
            )}

            <div className="flex flex-col sm:flex-row sm:items-end gap-3">
              <div className="flex-1">
                <Field label="Your department" htmlFor="department">
                  <Select
                    id="department"
                    value={department}
                    onChange={(e) => setDepartment(e.target.value)}
                  >
                    {/* The stored value may predate this list, so it is always offered. */}
                    {!DEPARTMENTS.includes(user.department as (typeof DEPARTMENTS)[number]) && (
                      <option value={user.department}>{user.department}</option>
                    )}
                    {DEPARTMENTS.map((item) => (
                      <option key={item} value={item}>
                        {item}
                      </option>
                    ))}
                  </Select>
                </Field>
              </div>

              <Button
                size="md"
                icon={<Check size={15} />}
                disabled={!changed}
                loading={save.isPending}
                onClick={() => save.mutate()}
              >
                Save
              </Button>
            </div>
          </Card>

          {/* ── Appearance ─────────────────────────────────────────────────── */}
          <Card>
            <CardHeader title="Appearance" description="Applies to this browser only." />
            <ThemePicker />
          </Card>

          <PushCard />

          <Card>
            <CardHeader
              title="Session"
              description="Signing out revokes this device's session on the server."
            />
            <Button
              variant="secondary"
              size="sm"
              icon={<LogOut size={15} />}
              onClick={() => void signOut()}
            >
              Sign out
            </Button>
          </Card>
        </div>
      </PageBody>
    </Page>
  );
}

function Detail({
  icon,
  label,
  value,
  hint,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <span className="w-7 h-7 rounded-[--radius-sm] bg-surface-2 flex items-center justify-center text-faint shrink-0">
        {icon}
      </span>
      <div className="min-w-0">
        <dt className="text-[11.5px] font-medium uppercase tracking-[0.06em] text-faint">
          {label}
        </dt>
        <dd className="text-[14px] text-body mt-0.5 break-words">{value}</dd>
        {hint && <p className="text-[12px] text-faint mt-0.5">{hint}</p>}
      </div>
    </div>
  );
}

/**
 * Push notifications.
 *
 * The card renders nothing at all when the server has no VAPID keys configured. That is
 * the rule the whole rebuild follows: a control whose backend does not exist is not a
 * disabled control, it is an absent one.
 */
function PushCard() {
  const { state, busy, error, subscribe, unsubscribe } = usePushNotifications();

  if (state === "checking" || state === "unconfigured") return null;

  return (
    <Card>
      <CardHeader
        title="Push notifications"
        description="Get mentions and replies as system notifications, even when ZapChat is closed."
      />

      {error && (
        <p className="text-[13px] text-danger mb-3" role="alert">
          {error}
        </p>
      )}

      {state === "unsupported" ? (
        <p className="text-[13px] text-faint">
          This browser does not support push notifications, or the page is not served over
          HTTPS.
        </p>
      ) : state === "denied" ? (
        <p className="text-[13px] text-faint">
          Notifications are blocked for this site. Allow them in your browser settings to
          turn this on.
        </p>
      ) : state === "subscribed" ? (
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <span className="inline-flex items-center gap-2 text-[13px] text-success">
            <BellRing size={15} />
            Enabled on this device
          </span>
          <Button variant="secondary" size="sm" loading={busy} onClick={() => void unsubscribe()}>
            Turn off
          </Button>
        </div>
      ) : (
        <Button
          variant="secondary"
          size="sm"
          icon={<BellRing size={15} />}
          loading={busy}
          onClick={() => void subscribe()}
        >
          Enable on this device
        </Button>
      )}
    </Card>
  );
}

function ThemePicker() {
  const { preference, setPreference } = useTheme();

  const options: Array<{ value: ThemePreference; label: string; icon: React.ReactNode }> = [
    { value: "light", label: "Light", icon: <Sun size={15} /> },
    { value: "dark", label: "Dark", icon: <Moon size={15} /> },
    { value: "system", label: "System", icon: <Monitor size={15} /> },
  ];

  return (
    <div className="grid grid-cols-3 gap-2" role="radiogroup" aria-label="Theme">
      {options.map((option) => {
        const active = preference === option.value;

        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={active}
            onClick={() => setPreference(option.value)}
            className={
              active
                ? "flex flex-col items-center gap-1.5 py-3 rounded-[--radius-DEFAULT] border border-accent bg-accent-soft text-accent-text text-[12.5px] font-medium"
                : "flex flex-col items-center gap-1.5 py-3 rounded-[--radius-DEFAULT] border border-line bg-surface text-muted text-[12.5px] hover:border-line-strong transition-colors"
            }
          >
            {option.icon}
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
