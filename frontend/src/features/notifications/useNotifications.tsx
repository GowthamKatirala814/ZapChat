import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import toast from "react-hot-toast";
import { keys } from "../../app/queryKeys";
import { useAuth } from "../../app/providers";
import { paths } from "../../config";
import { notificationsApi } from "../../services/api";
import { HubEvent } from "../../services/realtime/events";
import { useHubConnection, useHubEvent, useHubReconnect } from "../../services/realtime/hooks";
import type { AppNotification } from "../../types/api";

/**
 * Notifications.
 *
 * The unread count is read from the server, never counted from the loaded page — the
 * list is capped at 50 items, so a client-side count would silently under-report once a
 * user had more than that.
 */

export function useNotificationsList(unreadOnly: boolean) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.notifications.list(unreadOnly),
    queryFn: () => notificationsApi.list(50, unreadOnly),
    enabled: isAuthenticated,
  });
}

function useUnreadCountQuery() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.notifications.unreadCount(),
    queryFn: () => notificationsApi.unreadCount(),
    enabled: isAuthenticated,
  });
}

/** The badge number. Zero while loading, so the nav never flashes a wrong count. */
export function useUnreadNotifications(): number {
  return useUnreadCountQuery().data ?? 0;
}

export function useNotificationMutations() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: keys.notifications.all });
  };

  return {
    markRead: useMutation({
      mutationFn: (id: string) => notificationsApi.markRead(id),
      onSuccess: invalidate,
    }),
    markAllRead: useMutation({
      mutationFn: () => notificationsApi.markAllRead(),
      onSuccess: invalidate,
    }),
    remove: useMutation({
      mutationFn: (id: string) => notificationsApi.remove(id),
      onSuccess: invalidate,
    }),
  };
}

/**
 * App-wide notification stream. Mounted once, in the shell.
 *
 * `NotificationDeleted` matters as much as the arrival event: when a message is removed
 * by moderation the server withdraws the notifications it produced, and without handling
 * that the UI would keep offering to navigate to content that no longer exists.
 */
export function useNotificationStream() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  useHubConnection("notifications", isAuthenticated);

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: keys.notifications.all });
  };

  useHubEvent(
    "notifications",
    HubEvent.NotificationReceived,
    (notification: AppNotification) => {
      refresh();

      // A moderation notice concerns the user's own content and is easy to miss in a
      // list, so it is surfaced immediately. Ordinary message notifications are not
      // toasted — that would duplicate the chat window the user is already looking at.
      if (notification.type === "Moderation" || notification.type === "System") {
        toast(notification.message, {
          icon: notification.type === "Moderation" ? "⚠️" : "ℹ️",
          duration: 8_000,
        });
      } else if (notification.type === "Mention" || notification.type === "Reply") {
        toast(
          (t) => (
            <button
              type="button"
              className="text-left"
              onClick={() => {
                toast.dismiss(t.id);
                navigate(paths.notifications);
              }}
            >
              <span className="block font-medium">{notification.title}</span>
              <span className="block text-[12.5px] opacity-80">{notification.message}</span>
            </button>
          ),
          { duration: 6_000 },
        );
      }
    },
    isAuthenticated,
  );

  useHubEvent("notifications", HubEvent.NotificationDeleted, refresh, isAuthenticated);

  // Anything that arrived while the socket was down is only visible after a refetch.
  useHubReconnect("notifications", refresh, isAuthenticated);
}
