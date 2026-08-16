import { clsx } from "clsx";
import { Check, Play, Settings2, ShieldAlert, ShieldCheck, X } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import {
  Avatar, Badge, Button, Card, CardHeader, Field, Input, Modal, Textarea,
} from "../../components/ui";
import { formatCount, formatDateTime, formatRelative } from "../../lib/format";
import { errorMessage } from "../../services/api";
import type { Report, ReportStatus } from "../../types/api";
import {
  useBlockedUsers, useModerationMutations, useModerationSettings, useModerationStats, useReports,
} from "./useAdmin";

/**
 * The moderation queue.
 *
 * What a moderator sees is deliberately bounded: the reported text, the two anonymous
 * names, the stated reason, and how many distinct people have reported this author. Real
 * names and email addresses are not in the DTO at all, so the queue cannot leak them —
 * an admin who needs to act on an account does so by its anonymous identity.
 */
export function ModerationPage() {
  const [status, setStatus] = useState<ReportStatus | undefined>("Pending");
  const [page, setPage] = useState(1);
  const [showSettings, setShowSettings] = useState(false);
  const [resolving, setResolving] = useState<{ report: Report; kind: "action" | "dismiss" } | null>(
    null,
  );

  const reports = useReports(status, page);
  const settings = useModerationSettings();
  const stats = useModerationStats();
  const blocked = useBlockedUsers();
  const { action, dismiss, runAutoModeration, unblockUser } = useModerationMutations();

  const items = reports.data?.items ?? [];

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-1 p-1 bg-surface-2 rounded-[--radius-DEFAULT]">
          {(
            [
              ["Pending", "Pending"],
              ["Actioned", "Actioned"],
              ["AutoActioned", "Automated"],
              ["Dismissed", "Dismissed"],
              [undefined, "All"],
            ] as const
          ).map(([value, label]) => (
            <button
              key={label}
              type="button"
              aria-pressed={status === value}
              onClick={() => {
                setStatus(value);
                setPage(1);
              }}
              className={
                status === value
                  ? "px-3 h-7 rounded-[--radius-sm] bg-surface text-body text-[12.5px] font-medium shadow-sm"
                  : "px-3 h-7 rounded-[--radius-sm] text-muted text-[12.5px] hover:text-body transition-colors"
              }
            >
              {label}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="secondary"
            icon={<Play size={14} />}
            loading={runAutoModeration.isPending}
            onClick={() => {
              if (
                !window.confirm(
                  "Run the automated moderation sweep now? It applies the configured rules to every author currently over the report threshold.",
                )
              )
                return;

              runAutoModeration.mutate(undefined, {
                onSuccess: (result) =>
                  toast.success(
                    result.authorsActioned === 0
                      ? "No authors were over the threshold."
                      : `${result.authorsActioned} author${result.authorsActioned === 1 ? "" : "s"} actioned.`,
                  ),
                onError: (error) => toast.error(errorMessage(error)),
              });
            }}
          >
            Run auto-moderation
          </Button>

          <Button
            size="sm"
            variant="secondary"
            icon={<Settings2 size={14} />}
            onClick={() => setShowSettings(true)}
          >
            Settings
          </Button>
        </div>
      </div>

      {settings.data && (
        <div className="flex items-start gap-2.5 p-3 rounded-[--radius-DEFAULT] bg-surface-2 border border-line text-[12.5px]">
          <ShieldCheck size={15} className="text-muted shrink-0 mt-0.5" />
          <p className="text-muted">
            {settings.data.autoActionEnabled ? (
              <>
                Automated moderation is <span className="text-body font-medium">on</span>: an
                author reported by {settings.data.reportThreshold} or more distinct people is
                actioned automatically
                {settings.data.autoRemoveMessages && ", their reported messages removed"}
                {settings.data.autoDisableAccount && ", their account disabled"}.
              </>
            ) : (
              <>
                Automated moderation is{" "}
                <span className="text-body font-medium">off</span>. Reports are queued for
                manual review only.
              </>
            )}
          </p>
        </div>
      )}

      {/* ── Queue ──────────────────────────────────────────────────────────── */}
      {reports.isLoading ? (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-40 rounded-[--radius-lg]" count={3} />
        </div>
      ) : reports.error ? (
        <ErrorState error={reports.error} onRetry={() => void reports.refetch()} />
      ) : items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ShieldCheck size={20} />}
            title={status === "Pending" ? "Nothing awaiting review" : "No reports here"}
            description={
              status === "Pending"
                ? "The queue is clear. Reported messages appear here as soon as they are submitted."
                : "Try another filter."
            }
          />
        </Card>
      ) : (
        <>
          <div className="flex flex-col gap-3">
            {items.map((report) => (
              <ReportCard
                key={report.id}
                report={report}
                onAction={() => setResolving({ report, kind: "action" })}
                onDismiss={() => setResolving({ report, kind: "dismiss" })}
              />
            ))}
          </div>

          {(reports.data?.totalPages ?? 1) > 1 && (
            <div className="flex items-center justify-between">
              <span className="text-[12.5px] text-faint">
                Page {reports.data!.page} of {reports.data!.totalPages} ·{" "}
                {formatCount(reports.data!.totalCount)} reports
              </span>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="secondary"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Previous
                </Button>
                <Button
                  size="sm"
                  variant="secondary"
                  disabled={page >= (reports.data?.totalPages ?? 1)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {/* ── Automated content checks ───────────────────────────────────────── */}
      <Card>
        <CardHeader
          title="Automated content checks"
          description="Every message is screened before it is posted. Source: Chat service — moderationEvents collection."
        />

        {stats.isLoading ? (
          <div className="zc-skeleton h-20 w-full" aria-hidden />
        ) : stats.error ? (
          <ErrorState error={stats.error} onRetry={() => void stats.refetch()} compact />
        ) : !stats.data || stats.data.total === 0 ? (
          <p className="text-[13px] text-faint py-4 text-center">
            No messages have been screened yet.
          </p>
        ) : (
          <div className="flex flex-col gap-4">
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              <Stat label="Screened" value={stats.data.total} />
              <Stat label="Allowed" value={stats.data.allowed} tone="success" />
              <Stat label="Blocked" value={stats.data.blocked} tone="danger" />
              <Stat
                label="Checked by AI"
                value={stats.data.geminiRequests}
                footnote={`${formatCount(stats.data.ruleRequests)} by rules`}
              />
            </div>

            {Object.keys(stats.data.blockedByCategory).length > 0 && (
              <div>
                <p className="text-[11.5px] font-semibold uppercase tracking-[0.05em] text-faint mb-2">
                  Blocked by category
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {Object.entries(stats.data.blockedByCategory)
                    .sort(([, a], [, b]) => b - a)
                    .map(([category, count]) => (
                      <Badge key={category} tone="danger">
                        {category} · {formatCount(count)}
                      </Badge>
                    ))}
                </div>
              </div>
            )}
          </div>
        )}
      </Card>

      {/* ── Blocked accounts ───────────────────────────────────────────────── */}
      <Card>
        <CardHeader
          title="Blocked accounts"
          description="Accounts blocked manually or by the automated rules."
        />

        {blocked.isLoading ? (
          <div className="zc-skeleton h-16 w-full" aria-hidden />
        ) : blocked.error ? (
          <ErrorState error={blocked.error} onRetry={() => void blocked.refetch()} compact />
        ) : (blocked.data?.length ?? 0) === 0 ? (
          <p className="text-[13px] text-faint py-4 text-center">No accounts are blocked.</p>
        ) : (
          <ul className="flex flex-col divide-y divide-line-subtle -my-1">
            {blocked.data!.map((user) => (
              <li key={user.userId} className="flex items-center gap-3 py-2.5">
                <Avatar name={user.anonymousName} size={30} />

                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-[13.5px] font-medium text-body">
                      {user.anonymousName}
                    </span>
                    <Badge tone={user.source === "AutoModeration" ? "warning" : "neutral"}>
                      {user.source === "AutoModeration" ? "Automated" : "Manual"}
                    </Badge>
                  </div>
                  <p className="text-[12.5px] text-muted truncate">{user.reason}</p>
                </div>

                <span className="text-[11.5px] text-faint shrink-0">
                  {formatRelative(user.blockedAt)}
                </span>

                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    if (!window.confirm(`Unblock ${user.anonymousName}?`)) return;

                    unblockUser.mutate(user.userId, {
                      onSuccess: () => toast.success("Account unblocked."),
                      onError: (error) => toast.error(errorMessage(error)),
                    });
                  }}
                >
                  Unblock
                </Button>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {resolving && (
        <ResolveDialog
          report={resolving.report}
          kind={resolving.kind}
          isPending={action.isPending || dismiss.isPending}
          onClose={() => setResolving(null)}
          onConfirm={(note) => {
            const mutation = resolving.kind === "action" ? action : dismiss;

            mutation.mutate(
              { reportId: resolving.report.id, note },
              {
                onSuccess: () => {
                  toast.success(
                    resolving.kind === "action"
                      ? "Message removed and report closed."
                      : "Report dismissed.",
                  );
                  setResolving(null);
                },
                onError: (error) => toast.error(errorMessage(error)),
              },
            );
          }}
        />
      )}

      {showSettings && <SettingsDialog onClose={() => setShowSettings(false)} />}
    </div>
  );
}

// ── Report card ───────────────────────────────────────────────────────────────

function ReportCard({
  report,
  onAction,
  onDismiss,
}: {
  report: Report;
  onAction: () => void;
  onDismiss: () => void;
}) {
  const pending = report.status === "Pending";
  const overThreshold = report.authorReportCount >= report.threshold;

  return (
    <Card className="flex flex-col gap-3">
      <div className="flex items-start justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-2 flex-wrap">
          <Badge tone={report.kind === "DirectMessage" ? "info" : "neutral"}>
            {report.kind === "DirectMessage" ? "Direct message" : report.roomName ?? "Channel"}
          </Badge>

          <StatusBadge status={report.status} />

          {overThreshold && pending && (
            <Badge tone="danger">
              <ShieldAlert size={10} />
              {report.authorReportCount} reports — over threshold ({report.threshold})
            </Badge>
          )}
        </div>

        <time
          dateTime={report.createdAt}
          title={formatDateTime(report.createdAt)}
          className="text-[11.5px] text-faint shrink-0"
        >
          {formatRelative(report.createdAt)}
        </time>
      </div>

      {/* The snapshot is what the message said when it was reported, even if it has
          been edited since — otherwise an author could edit their way out of review. */}
      <blockquote className="p-3 rounded-[--radius-DEFAULT] bg-surface-2 border-l-2 border-line-strong">
        <p className="text-[13.5px] text-body zc-message-text">
          {report.contentSnapshot || <span className="italic text-faint">(no text content)</span>}
        </p>
      </blockquote>

      <div className="grid sm:grid-cols-2 gap-3">
        <Party label="Author" name={report.authorAnonymousName} count={report.authorReportCount} />
        <Party label="Reported by" name={report.reportedByAnonymousName} />
      </div>

      <div>
        <p className="text-[11.5px] font-semibold uppercase tracking-[0.05em] text-faint">
          Reason given
        </p>
        <p className="text-[13px] text-body mt-0.5 zc-message-text">{report.reason}</p>
      </div>

      {pending ? (
        <div className="flex items-center gap-2 pt-1">
          <Button size="sm" variant="danger" icon={<Check size={14} />} onClick={onAction}>
            Remove message
          </Button>
          <Button size="sm" variant="secondary" icon={<X size={14} />} onClick={onDismiss}>
            Dismiss report
          </Button>
        </div>
      ) : (
        report.resolvedAt && (
          <p className="text-[12px] text-faint pt-1">
            Resolved {formatRelative(report.resolvedAt)}
          </p>
        )
      )}
    </Card>
  );
}

function Party({ label, name, count }: { label: string; name: string; count?: number }) {
  return (
    <div className="flex items-center gap-2.5">
      <Avatar name={name} size={28} />
      <div className="min-w-0">
        <p className="text-[11px] font-semibold uppercase tracking-[0.05em] text-faint">{label}</p>
        <p className="text-[13px] text-body truncate">
          {name}
          {count !== undefined && count > 1 && (
            <span className="text-faint ml-1.5 text-[12px]">
              · {count} reports total
            </span>
          )}
        </p>
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: ReportStatus }) {
  const map = {
    Pending: { tone: "warning", label: "Pending" },
    Actioned: { tone: "danger", label: "Actioned" },
    AutoActioned: { tone: "danger", label: "Auto-actioned" },
    Dismissed: { tone: "neutral", label: "Dismissed" },
  } as const;

  const entry = map[status];

  return <Badge tone={entry.tone}>{entry.label}</Badge>;
}

function Stat({
  label,
  value,
  tone,
  footnote,
}: {
  label: string;
  value: number;
  tone?: "success" | "danger";
  footnote?: string;
}) {
  return (
    <div className="p-3 rounded-[--radius-DEFAULT] bg-surface-2 border border-line">
      <p className="text-[11px] font-semibold uppercase tracking-[0.05em] text-faint">{label}</p>
      <p
        className={clsx("text-[20px] font-semibold mt-0.5 zc-tabular")}
        style={{
          color: tone ? `var(--zc-${tone})` : "var(--zc-text)",
        }}
      >
        {formatCount(value)}
      </p>
      {footnote && <p className="text-[11px] text-faint mt-0.5">{footnote}</p>}
    </div>
  );
}

// ── Dialogs ───────────────────────────────────────────────────────────────────

function ResolveDialog({
  report,
  kind,
  onClose,
  onConfirm,
  isPending,
}: {
  report: Report;
  kind: "action" | "dismiss";
  onClose: () => void;
  onConfirm: (note?: string) => void;
  isPending: boolean;
}) {
  const [note, setNote] = useState("");

  return (
    <Modal
      open
      onClose={onClose}
      title={kind === "action" ? "Remove this message" : "Dismiss this report"}
      description={
        kind === "action"
          ? "The message is removed for everyone and the author is notified. The report is then closed."
          : "The message stays up and the report is closed. The reporter is not told who reviewed it."
      }
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            variant={kind === "action" ? "danger" : "primary"}
            loading={isPending}
            onClick={() => onConfirm(note.trim() || undefined)}
          >
            {kind === "action" ? "Remove message" : "Dismiss report"}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <blockquote className="p-3 rounded-[--radius-DEFAULT] bg-surface-2 border-l-2 border-line-strong">
          <p className="text-[13px] text-body zc-message-text line-clamp-5">
            {report.contentSnapshot}
          </p>
          <p className="text-[11.5px] text-faint mt-1.5">— {report.authorAnonymousName}</p>
        </blockquote>

        <Field
          label="Note"
          htmlFor="resolveNote"
          hint="Optional. Recorded in the audit log against your name."
        >
          <Textarea
            id="resolveNote"
            rows={3}
            maxLength={500}
            autoFocus
            value={note}
            onChange={(e) => setNote(e.target.value)}
            placeholder="Why are you taking this decision?"
          />
        </Field>
      </div>
    </Modal>
  );
}

function SettingsDialog({ onClose }: { onClose: () => void }) {
  const settings = useModerationSettings();
  const { saveSettings } = useModerationMutations();

  const [draft, setDraft] = useState(() => ({
    reportThreshold: settings.data?.reportThreshold ?? 5,
    autoActionEnabled: settings.data?.autoActionEnabled ?? false,
    autoRemoveMessages: settings.data?.autoRemoveMessages ?? false,
    autoDisableAccount: settings.data?.autoDisableAccount ?? false,
  }));

  return (
    <Modal
      open
      onClose={onClose}
      title="Moderation settings"
      description="These govern the automated sweep. Manual review is always available regardless."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            loading={saveSettings.isPending}
            onClick={() =>
              saveSettings.mutate(draft, {
                onSuccess: () => {
                  toast.success("Settings saved.");
                  onClose();
                },
                onError: (error) => toast.error(errorMessage(error)),
              })
            }
          >
            Save settings
          </Button>
        </>
      }
    >
      {settings.isLoading ? (
        <div className="zc-skeleton h-40 w-full" aria-hidden />
      ) : settings.error ? (
        <ErrorState error={settings.error} onRetry={() => void settings.refetch()} compact />
      ) : (
        <div className="flex flex-col gap-4">
          {saveSettings.error != null && <ErrorState error={saveSettings.error} compact />}

          <Field
            label="Report threshold"
            htmlFor="threshold"
            hint="How many distinct people must report an author before the automated rules apply."
          >
            <Input
              id="threshold"
              type="number"
              min={1}
              max={100}
              value={draft.reportThreshold}
              onChange={(e) =>
                setDraft((d) => ({ ...d, reportThreshold: Number(e.target.value) || 1 }))
              }
            />
          </Field>

          <Toggle
            label="Enable automated moderation"
            description="Without this, reports are queued for manual review only."
            checked={draft.autoActionEnabled}
            onChange={(checked) => setDraft((d) => ({ ...d, autoActionEnabled: checked }))}
          />

          <Toggle
            label="Remove reported messages"
            description="Delete the reported messages of an author over the threshold."
            checked={draft.autoRemoveMessages}
            disabled={!draft.autoActionEnabled}
            onChange={(checked) => setDraft((d) => ({ ...d, autoRemoveMessages: checked }))}
          />

          <Toggle
            label="Disable the account"
            description="Prevent the author from signing in. They will need an administrator to restore access."
            checked={draft.autoDisableAccount}
            disabled={!draft.autoActionEnabled}
            onChange={(checked) => setDraft((d) => ({ ...d, autoDisableAccount: checked }))}
          />
        </div>
      )}
    </Modal>
  );
}

function Toggle({
  label,
  description,
  checked,
  disabled,
  onChange,
}: {
  label: string;
  description: string;
  checked: boolean;
  disabled?: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <label
      className={clsx(
        "flex items-start gap-3 p-3 rounded-[--radius-DEFAULT] border cursor-pointer transition-colors",
        checked && !disabled ? "border-accent bg-accent-soft" : "border-line bg-surface",
        disabled && "opacity-50 cursor-not-allowed",
      )}
    >
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-0.5 w-4 h-4 accent-[var(--zc-accent)] shrink-0"
      />
      <span className="min-w-0">
        <span className="block text-[13.5px] font-medium text-body">{label}</span>
        <span className="block text-[12.5px] text-muted mt-0.5">{description}</span>
      </span>
    </label>
  );
}
