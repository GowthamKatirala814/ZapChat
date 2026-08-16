import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { keys } from "../../app/queryKeys";
import { useAuth } from "../../app/providers";
import { adminApi, chatAdminApi, reportsApi } from "../../services/api";
import type { ModerationSettings, ReportStatus } from "../../types/api";

/**
 * Admin data.
 *
 * Every query here is admin-gated on the server; the route guard only saves the user a
 * 403. `enabled: isAdmin` also stops a non-admin's browser firing a burst of requests it
 * will never be allowed to complete.
 */

function useAdminQuery() {
  const { isAdmin } = useAuth();
  return isAdmin;
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

export function useDashboardStats() {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.stats(),
    queryFn: () => adminApi.stats(),
    enabled,
    // Cross-service aggregate; a minute of staleness is fine and avoids hammering
    // five services every time the tab regains focus.
    staleTime: 60_000,
  });
}

export function useRecentActivity(count = 12) {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.recentActivity(),
    queryFn: () => adminApi.recentActivity(count),
    enabled,
  });
}

// ── Analytics ─────────────────────────────────────────────────────────────────

/**
 * One analytics series.
 *
 * The chart name is part of the cache key, and the fetcher is passed in, so adding a
 * chart never means adding a hook. Series that come from another service arrive wrapped
 * in `Availability`, which the chart renders as "unavailable" rather than as zero.
 */
export function useAnalytics<T>(chart: string, fetcher: () => Promise<T>, param?: number) {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.analytics(chart, param),
    queryFn: fetcher,
    enabled,
    staleTime: 60_000,
  });
}

// ── Moderation ────────────────────────────────────────────────────────────────

export function useReports(status: ReportStatus | undefined, page: number) {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.reports(status, page),
    queryFn: () => reportsApi.queue(status, page, 25),
    enabled,
  });
}

export function useModerationSettings() {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.moderationSettings(),
    queryFn: () => adminApi.moderationSettings(),
    enabled,
  });
}

export function useModerationStats() {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.moderationStats(),
    queryFn: () => chatAdminApi.moderationStats(),
    enabled,
    staleTime: 60_000,
  });
}

export function useBlockedUsers() {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.blockedUsers(),
    queryFn: () => adminApi.blockedUsers(),
    enabled,
  });
}

export function useModerationMutations() {
  const queryClient = useQueryClient();

  /** Resolving a report changes the queue, the dashboard counts and the audit log. */
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: keys.admin.all });
  };

  return {
    action: useMutation({
      mutationFn: ({ reportId, note }: { reportId: string; note?: string }) =>
        reportsApi.action(reportId, note),
      onSuccess: invalidate,
    }),

    dismiss: useMutation({
      mutationFn: ({ reportId, note }: { reportId: string; note?: string }) =>
        reportsApi.dismiss(reportId, note),
      onSuccess: invalidate,
    }),

    saveSettings: useMutation({
      mutationFn: (settings: Omit<ModerationSettings, "updatedAt">) =>
        adminApi.updateModerationSettings(settings),
      onSuccess: invalidate,
    }),

    runAutoModeration: useMutation({
      mutationFn: () => adminApi.runAutoModeration(),
      onSuccess: invalidate,
    }),

    blockUser: useMutation({
      mutationFn: ({ userId, reason }: { userId: string; reason: string }) =>
        adminApi.blockUser(userId, reason),
      onSuccess: invalidate,
    }),

    unblockUser: useMutation({
      mutationFn: (userId: string) => adminApi.unblockUser(userId),
      onSuccess: invalidate,
    }),
  };
}

// ── Users ─────────────────────────────────────────────────────────────────────

export interface UserQuery {
  page: number;
  pageSize: number;
  search?: string;
  status?: string;
  branch?: string;
  department?: string;
  sortBy?: string;
  sortDesc?: boolean;
}

export function useAdminUsers(params: UserQuery) {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.users(params),
    queryFn: () => adminApi.users(params),
    enabled,
  });
}

export function useUserMutations() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: keys.admin.all });
  };

  return {
    deleteUser: useMutation({
      mutationFn: ({ userId, reason }: { userId: string; reason: string }) =>
        adminApi.deleteUser(userId, reason),
      onSuccess: invalidate,
    }),

    /** Branch gates channel access, so it is admin-managed rather than self-asserted. */
    setBranch: useMutation({
      mutationFn: ({ userId, branch }: { userId: string; branch: string }) =>
        adminApi.setUserBranch(userId, branch),
      onSuccess: invalidate,
    }),
  };
}

// ── Rooms ─────────────────────────────────────────────────────────────────────

export function useAdminRooms(includeArchived: boolean) {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.rooms(includeArchived),
    queryFn: () => chatAdminApi.rooms(includeArchived),
    enabled,
  });
}

export function useRoomMutations() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: keys.admin.all });
    // The user-facing sidebar is a different cache and must not go stale.
    void queryClient.invalidateQueries({ queryKey: keys.rooms.all });
  };

  return {
    create: useMutation({
      mutationFn: (payload: {
        name: string;
        description: string;
        type: string;
        branch?: string;
      }) => chatAdminApi.createRoom(payload),
      onSuccess: invalidate,
    }),

    update: useMutation({
      mutationFn: ({
        roomId,
        name,
        description,
      }: {
        roomId: string;
        name: string;
        description: string;
      }) => chatAdminApi.updateRoom(roomId, name, description),
      onSuccess: invalidate,
    }),

    /** Archives rather than deletes; the messages are retained. */
    archive: useMutation({
      mutationFn: (roomId: string) => chatAdminApi.archiveRoom(roomId),
      onSuccess: invalidate,
    }),

    restore: useMutation({
      mutationFn: (roomId: string) => chatAdminApi.restoreRoom(roomId),
      onSuccess: invalidate,
    }),
  };
}

// ── Audit ─────────────────────────────────────────────────────────────────────

export function useAuditLogs(page: number, entityType?: string) {
  const enabled = useAdminQuery();

  return useQuery({
    queryKey: keys.admin.auditLogs(page, entityType),
    queryFn: () => adminApi.auditLogs(page, 30, entityType),
    enabled,
  });
}
