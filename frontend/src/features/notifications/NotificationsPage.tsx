import { clsx } from "clsx";
import {
  AtSign, Bell, CheckCheck, Info, MessageSquare, Reply, ShieldAlert, Trash2,
} from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import { Page, PageBody, PageHeader } from "../../components/layout/ListDetail";
import { Badge, Button } from "../../components/ui";
import { formatRelative } from "../../lib/format";
import { errorMessage } from "../../services/api";
import type { AppNotification, NotificationType } from "../../types/api";
import { useNotificationMutations, useNotificationsList, useUnreadNotifications } from "./useNotifications";

/**
 * Activity.
 *
 * Notifications are produced by the services that cause them — a mention, a reply, a
 * moderation action — and are withdrawn by the server when the thing they point at is
 * removed. So there is nothing to reconcile here; the list is displayed as given.
 */
export function NotificationsPage() {
  const [unreadOnly, setUnreadOnly] = useState(false);

  const notifications = useNotificationsList(unreadOnly);
  const unread = useUnreadNotifications();
  const { markRead, markAllRead, remove } = useNotificationMutations();

  const items = notifications.data ?? [];

  return (
    <Page>
      <PageHeader
        title="Activity"
        description={
          unread > 0 ? `${unread} unread` : "Mentions, replies and moderation notices"
        }
        action={
          unread > 0 && (
            <Button
              size="sm"
              variant="secondary"
              icon={<CheckCheck size={14} />}
              loading={markAllRead.isPending}
              onClick={() =>
                markAllRead.mutate(undefined, {
                  onError: (error) => toast.error(errorMessage(error)),
                })
              }
            >
              Mark all read
            </Button>
          )
        }
      />

      <PageBody width="narrow">
        <div className="flex items-center gap-1 mb-4 p-1 bg-surface-2 rounded-[--radius-DEFAULT] w-fit">
          <Tab active={!unreadOnly} onClick={() => setUnreadOnly(false)}>
            All
          </Tab>
          <Tab active={unreadOnly} onClick={() => setUnreadOnly(true)}>
            Unread
          </Tab>
        </div>

        {notifications.isLoading ? (
          <div className="flex flex-col gap-2">
            <Skeleton className="h-[68px] rounded-[--radius-DEFAULT]" count={5} />
          </div>
        ) : notifications.error ? (
          <ErrorState error={notifications.error} onRetry={() => void notifications.refetch()} />
        ) : items.length === 0 ? (
          <EmptyState
            icon={<Bell size={20} />}
            title={unreadOnly ? "Nothing unread" : "No activity yet"}
            description={
              unreadOnly
                ? "You are all caught up."
                : "When somebody mentions you, replies to you, or a moderator acts on your content, it will show up here."
            }
          />
        ) : (
          <ul className="flex flex-col gap-1.5">
            {items.map((notification) => (
              <li key={notification.id}>
                <NotificationRow
                  notification={notification}
                  onMarkRead={() =>
                    markRead.mutate(notification.id, {
                      onError: (error) => toast.error(errorMessage(error)),
                    })
                  }
                  onRemove={() =>
                    remove.mutate(notification.id, {
                      onError: (error) => toast.error(errorMessage(error)),
                    })
                  }
                />
              </li>
            ))}
          </ul>
        )}
      </PageBody>
    </Page>
  );
}

function NotificationRow({
  notification,
  onMarkRead,
  onRemove,
}: {
  notification: AppNotification;
  onMarkRead: () => void;
  onRemove: () => void;
}) {
  const { icon, tone } = describe(notification.type);

  return (
    <div
      className={clsx(
        "group flex items-start gap-3 p-3 rounded-[--radius-DEFAULT] border transition-colors",
        notification.isRead
          ? "bg-surface border-line-subtle"
          : "bg-surface border-line shadow-sm",
      )}
    >
      <span
        className="w-8 h-8 rounded-[--radius-DEFAULT] flex items-center justify-center shrink-0"
        style={{
          background: `var(--zc-${tone}-soft)`,
          color: `var(--zc-${tone})`,
        }}
        aria-hidden
      >
        {icon}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 flex-wrap">
          <span
            className={clsx(
              "text-[13.5px] truncate",
              notification.isRead ? "text-body" : "font-semibold text-body",
            )}
          >
            {notification.title}
          </span>
          {!notification.isRead && <Badge tone="accent">New</Badge>}
        </div>

        <p className="text-[13px] text-muted mt-0.5 zc-message-text">{notification.message}</p>

        <p className="text-[11.5px] text-faint mt-1">{formatRelative(notification.createdAt)}</p>
      </div>

      <div className="flex items-center gap-0.5 shrink-0 opacity-0 group-hover:opacity-100 focus-within:opacity-100 transition-opacity">
        {!notification.isRead && (
          <button
            type="button"
            onClick={onMarkRead}
            className="p-1.5 rounded-[--radius-sm] text-muted hover:bg-surface-2 hover:text-body"
            aria-label="Mark as read"
            title="Mark as read"
          >
            <CheckCheck size={15} />
          </button>
        )}
        <button
          type="button"
          onClick={onRemove}
          className="p-1.5 rounded-[--radius-sm] text-muted hover:bg-danger-soft hover:text-danger"
          aria-label="Remove notification"
          title="Remove"
        >
          <Trash2 size={15} />
        </button>
      </div>
    </div>
  );
}

/** Icon and semantic colour per type. Moderation reads as a warning, not as branding. */
function describe(type: NotificationType) {
  switch (type) {
    case "Mention":
      return { icon: <AtSign size={16} />, tone: "accent" };
    case "Reply":
      return { icon: <Reply size={16} />, tone: "info" };
    case "Moderation":
      return { icon: <ShieldAlert size={16} />, tone: "warning" };
    case "System":
      return { icon: <Info size={16} />, tone: "info" };
    default:
      return { icon: <MessageSquare size={16} />, tone: "accent" };
  }
}

function Tab({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={
        active
          ? "px-3 h-7 rounded-[--radius-sm] bg-surface text-body text-[12.5px] font-medium shadow-sm"
          : "px-3 h-7 rounded-[--radius-sm] text-muted text-[12.5px] hover:text-body transition-colors"
      }
    >
      {children}
    </button>
  );
}
