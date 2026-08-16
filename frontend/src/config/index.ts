/**
 * Runtime configuration and the canonical API route table.
 *
 * Every URL in the application resolves through here. The previous frontend hardcoded
 * "https://localhost:5000" in six separate files, so it could not be pointed at another
 * environment without editing source.
 */

function origin(name: string, value: string | undefined, fallback: string): string {
  const trimmed = value?.trim();
  if (trimmed) return trimmed.replace(/\/+$/, "");

  if (import.meta.env.PROD) {
    // A production bundle silently falling back to localhost is worse than a loud failure.
    throw new Error(`${name} is not set. Define it before building for production.`);
  }

  return fallback;
}

export const config = {
  /** Gateway origin. All REST and SignalR traffic goes through it. */
  apiUrl: origin("VITE_API_URL", import.meta.env.VITE_API_URL, "https://localhost:5000"),

  /** Hub origin; normally the same host, separated so WebSockets can be split off. */
  hubUrl: origin(
    "VITE_HUB_URL",
    import.meta.env.VITE_HUB_URL ?? import.meta.env.VITE_API_URL,
    "https://localhost:5000",
  ),

  /**
   * Web push public key. Empty disables the feature in the UI, matching the server,
   * which registers a no-op dispatcher when no VAPID keys are configured.
   */
  vapidPublicKey: (import.meta.env.VITE_VAPID_PUBLIC_KEY ?? "").trim(),
} as const;

export const pushEnabled = config.vapidPublicKey.length > 0;

/**
 * Every backend route the browser is allowed to call, by name.
 *
 * Routes not listed here are deliberately absent: the internal service-to-service
 * endpoints (`/api/auth/internal/*`, `/api/notifications/internal`,
 * `/api/moderation-lookup/*`) are not routed through the gateway and require a service
 * token, so no browser call can reach them.
 */
export const api = {
  auth: {
    login: "/api/auth/login",
    logout: "/api/auth/logout",
    refresh: "/api/auth/refresh",
    /** Raw JWT for the SignalR handshake. Authenticated; not a public echo. */
    hubToken: "/api/auth/token",
    me: "/api/auth/me",
    users: "/api/auth/users",
    user: (id: string) => `/api/auth/users/${id}`,
    registerInitiate: "/api/auth/register/initiate",
    registerVerify: "/api/auth/register/verify-otp",
    registerComplete: "/api/auth/register/complete",
    forgotPassword: "/api/auth/forgot-password",
    verifyResetOtp: "/api/auth/verify-otp",
    resetPassword: "/api/auth/reset-password",
  },

  rooms: {
    list: "/api/rooms",
    byId: (id: string) => `/api/rooms/${id}`,
    join: (id: string) => `/api/rooms/${id}/join`,
    leave: (id: string) => `/api/rooms/${id}/leave`,
    markRead: (id: string) => `/api/rooms/${id}/read`,
    members: (id: string) => `/api/rooms/${id}/members`,
    messages: (id: string) => `/api/rooms/${id}/messages`,
  },

  messages: {
    byId: (id: string) => `/api/messages/${id}`,
    reactions: (id: string) => `/api/messages/${id}/reactions`,
    readBy: (id: string) => `/api/messages/${id}/read-by`,
  },

  files: {
    upload: "/api/files",
    download: (id: string) => `/api/files/${id}`,
  },

  conversations: {
    list: "/api/conversations",
    start: "/api/conversations",
    byId: (id: string) => `/api/conversations/${id}`,
    messages: (id: string) => `/api/conversations/${id}/messages`,
    markRead: (id: string) => `/api/conversations/${id}/read`,
  },

  directMessages: {
    byId: (id: string) => `/api/direct-messages/${id}`,
    reactions: (id: string) => `/api/direct-messages/${id}/reactions`,
  },

  blocks: {
    list: "/api/blocks",
    byUser: (userId: string) => `/api/blocks/${userId}`,
  },

  polls: {
    list: "/api/polls",
    byId: (id: string) => `/api/polls/${id}`,
    create: "/api/polls",
    vote: (id: string) => `/api/polls/${id}/vote`,
    reaction: (id: string) => `/api/polls/${id}/reaction`,
    close: (id: string) => `/api/polls/${id}/close`,
  },

  notifications: {
    list: "/api/notifications",
    unreadCount: "/api/notifications/unread-count",
    markRead: (id: string) => `/api/notifications/${id}/read`,
    markAllRead: "/api/notifications/read-all",
    remove: (id: string) => `/api/notifications/${id}`,
    subscribePush: "/api/notifications/push/subscribe",
    unsubscribePush: "/api/notifications/push/unsubscribe",
  },

  reports: {
    submit: "/api/reports",
    queue: "/api/reports",
    action: (id: string) => `/api/reports/${id}/action`,
    dismiss: (id: string) => `/api/reports/${id}/dismiss`,
  },

  admin: {
    stats: "/api/admin/dashboard/stats",
    recentActivity: "/api/admin/dashboard/recent-activity",
    moderationSettings: "/api/admin/moderation/settings",
    runAutoModeration: "/api/admin/moderation/run-auto-moderation",
    blockedUsers: "/api/admin/users/blocked",
    blockUser: (id: string) => `/api/admin/users/${id}/block`,
    auditLogs: "/api/admin/audit-logs",
    users: "/api/auth/admin/users",
    userById: (id: string) => `/api/auth/admin/users/${id}`,
    setUserBranch: (id: string) => `/api/auth/admin/users/${id}/branch`,
    analytics: {
      messagesPerDay: "/api/admin/analytics/messages-per-day",
      messagesPerHour: "/api/admin/analytics/messages-per-hour",
      directMessagesPerDay: "/api/admin/analytics/direct-messages-per-day",
      pollsPerDay: "/api/admin/analytics/polls-per-day",
      notificationsPerDay: "/api/admin/analytics/notifications-per-day",
      topRooms: "/api/admin/analytics/top-rooms",
      topAuthors: "/api/admin/analytics/top-authors",
      topPolls: "/api/admin/analytics/top-polls",
      reportsPerDay: "/api/admin/analytics/reports-per-day",
      reportReasons: "/api/admin/analytics/report-reasons",
      roomHealth: "/api/admin/analytics/room-health",
    },
  },

  chatAdmin: {
    rooms: "/api/chat-admin/rooms",
    roomById: (id: string) => `/api/chat-admin/rooms/${id}`,
    restoreRoom: (id: string) => `/api/chat-admin/rooms/${id}/restore`,
    roomMembers: (id: string) => `/api/chat-admin/rooms/${id}/members`,
    moderationStats: "/api/chat-admin/moderation/stats",
  },
} as const;

/** SignalR hub paths. Moved from /chatHub to /hubs/chat during the backend rebuild. */
export const hubPaths = {
  chat: "/hubs/chat",
  privateChat: "/hubs/private-chat",
  polls: "/hubs/polls",
  notifications: "/hubs/notifications",
} as const;

export type HubName = keyof typeof hubPaths;

/** In-app route table, so navigation targets are never string literals in JSX. */
export const paths = {
  login: "/login",
  register: "/register",
  /** Reset is a single wizard route; the OTP and new-password steps are steps, not URLs. */
  forgotPassword: "/forgot-password",

  chat: "/chat",
  room: (id: string) => `/chat/${id}`,

  messages: "/messages",
  conversation: (id: string) => `/messages/${id}`,

  polls: "/polls",
  notifications: "/notifications",
  profile: "/profile",

  admin: {
    root: "/admin",
    moderation: "/admin/moderation",
    rooms: "/admin/rooms",
    users: "/admin/users",
    analytics: "/admin/analytics",
    audit: "/admin/audit",
  },
} as const;
