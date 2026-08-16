/**
 * Every cache key in the application.
 *
 * Keys live here rather than inline at each `useQuery` because invalidation is the part
 * that goes wrong: a realtime event handler in one feature must be able to invalidate a
 * list owned by another, and a key typed out twice with a different shape is a cache miss
 * that looks like a bug in the server.
 *
 * Convention: a broader key is a prefix of the narrower ones under it, so invalidating
 * `keys.rooms.all` also invalidates every room detail and message page.
 */
export const keys = {
  session: ["session"] as const,

  rooms: {
    all: ["rooms"] as const,
    list: () => ["rooms", "list"] as const,
    detail: (roomId: string) => ["rooms", "detail", roomId] as const,
    members: (roomId: string) => ["rooms", "members", roomId] as const,
    messages: (roomId: string) => ["rooms", "messages", roomId] as const,
  },

  conversations: {
    all: ["conversations"] as const,
    list: () => ["conversations", "list"] as const,
    detail: (id: string) => ["conversations", "detail", id] as const,
    messages: (id: string) => ["conversations", "messages", id] as const,
  },

  blocks: ["blocks"] as const,
  directory: ["directory"] as const,

  polls: {
    all: ["polls"] as const,
    list: () => ["polls", "list"] as const,
  },

  notifications: {
    all: ["notifications"] as const,
    list: (unreadOnly: boolean) => ["notifications", "list", unreadOnly] as const,
    unreadCount: () => ["notifications", "unread-count"] as const,
  },

  admin: {
    all: ["admin"] as const,
    stats: () => ["admin", "stats"] as const,
    recentActivity: () => ["admin", "recent-activity"] as const,
    reports: (status: string | undefined, page: number) =>
      ["admin", "reports", status ?? "all", page] as const,
    moderationSettings: () => ["admin", "moderation-settings"] as const,
    moderationStats: () => ["admin", "moderation-stats"] as const,
    blockedUsers: () => ["admin", "blocked-users"] as const,
    auditLogs: (page: number, entityType?: string) =>
      ["admin", "audit-logs", page, entityType ?? "all"] as const,
    users: (params: unknown) => ["admin", "users", params] as const,
    rooms: (includeArchived: boolean) => ["admin", "rooms", includeArchived] as const,
    roomMembers: (roomId: string) => ["admin", "room-members", roomId] as const,
    analytics: (chart: string, param?: number) => ["admin", "analytics", chart, param] as const,
  },
} as const;
