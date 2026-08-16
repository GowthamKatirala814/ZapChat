import { Bot, ClipboardList, UserRound } from "lucide-react";
import { useState } from "react";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import { Badge, Button, Card, Select } from "../../components/ui";
import { formatCount, formatDateTime } from "../../lib/format";
import { humaniseAction } from "../../lib/messages";
import type { AuditLogEntry } from "../../types/api";
import { useAuditLogs } from "./useAdmin";

/**
 * The audit log.
 *
 * Every administrative action is written here by the service that performed it, including
 * the automated ones — which is what makes "the system did it" verifiable rather than a
 * claim. `isSystem` separates a rule firing from a person deciding.
 */
const ENTITY_TYPES = ["Report", "User", "Message", "Room", "Settings"] as const;

export function AuditPage() {
  const [page, setPage] = useState(1);
  const [entityType, setEntityType] = useState<string>("");

  const logs = useAuditLogs(page, entityType || undefined);
  const items = logs.data?.items ?? [];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <p className="text-[13px] text-muted">
          Written by each service as actions are taken. Source: Admin service — auditLogs
          collection.
        </p>

        <Select
          value={entityType}
          onChange={(e) => {
            setEntityType(e.target.value);
            setPage(1);
          }}
          aria-label="Filter by entity"
          className="w-auto h-9 text-[13px]"
        >
          <option value="">Everything</option>
          {ENTITY_TYPES.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </Select>
      </div>

      {logs.isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-14 rounded-[--radius-DEFAULT]" count={8} />
        </div>
      ) : logs.error ? (
        <ErrorState error={logs.error} onRetry={() => void logs.refetch()} />
      ) : items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ClipboardList size={20} />}
            title="Nothing recorded"
            description={
              entityType
                ? "No actions of this kind have been taken yet."
                : "Administrative actions will appear here as they happen."
            }
          />
        </Card>
      ) : (
        <>
          <Card padded={false}>
            <ul className="divide-y divide-line-subtle">
              {items.map((entry) => (
                <AuditRow key={entry.id} entry={entry} />
              ))}
            </ul>
          </Card>

          {(logs.data?.totalPages ?? 1) > 1 && (
            <div className="flex items-center justify-between">
              <span className="text-[12.5px] text-faint">
                Page {logs.data!.page} of {logs.data!.totalPages} ·{" "}
                {formatCount(logs.data!.totalCount)} entries
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
                  disabled={page >= (logs.data?.totalPages ?? 1)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function AuditRow({ entry }: { entry: AuditLogEntry }) {
  return (
    <li className="flex items-start gap-3 p-3.5">
      <span
        className="w-8 h-8 rounded-[--radius-DEFAULT] flex items-center justify-center shrink-0"
        style={{
          background: entry.isSystem ? "var(--zc-warning-soft)" : "var(--zc-surface-2)",
          color: entry.isSystem ? "var(--zc-warning)" : "var(--zc-text-3)",
        }}
        aria-hidden
      >
        {entry.isSystem ? <Bot size={15} /> : <UserRound size={15} />}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-[13.5px] font-medium text-body">{humaniseAction(entry.action)}</span>
          <Badge tone="neutral">{entry.entityType}</Badge>
          <Badge tone={entry.isSystem ? "warning" : "accent"}>
            {entry.isSystem ? "Automated" : entry.actorName}
          </Badge>
        </div>

        {entry.details && (
          <p className="text-[12.5px] text-muted mt-1 zc-message-text">{entry.details}</p>
        )}

        <p className="text-[11px] text-faint mt-1 font-mono truncate">{entry.entityId}</p>
      </div>

      <time
        dateTime={entry.timestamp}
        className="text-[11.5px] text-faint shrink-0 zc-tabular hidden sm:block"
      >
        {formatDateTime(entry.timestamp)}
      </time>
    </li>
  );
}
