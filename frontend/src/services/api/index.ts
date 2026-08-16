import { api, config } from "../../config";
import { http, unwrap } from "./client";
import type {
  AdminUser, AppNotification, AuditLogEntry, AuthResult, Availability, BlockedUser,
  Conversation, CursorPage, DailyCount, DashboardStats, DirectMessage, Message,
  ModerationSettings, ModerationStats, MyProfile, NamedCount, PagedResult, Poll,
  PublicUser, ReadReceipt, Report, ReportStatus, ReportTargetKind, Room, RoomActivity,
  RoomHealth, RoomMember, StepResult,
} from "../../types/api";

/**
 * Every backend call the browser makes, typed and in one place.
 *
 * Note what is systematically absent: no `userId` parameters. The server derives the
 * caller from the session cookie, so a vote, report, block, read-marker or profile edit
 * cannot be performed on another user's behalf by editing a request. The old client
 * passed `localStorage.userId` into most of these.
 */

// ── Auth ──────────────────────────────────────────────────────────────────────

export const authApi = {
  login: (email: string, password: string) =>
    unwrap<AuthResult>(http.post(api.auth.login, { email, password })),

  logout: () => unwrap<void>(http.post(api.auth.logout)),

  /** Current session. 401 here means "not signed in", which is a normal outcome. */
  me: () => unwrap<MyProfile>(http.get(api.auth.me)),

  /** Department only — branch gates room access, so an admin manages it. */
  updateProfile: (department: string) =>
    unwrap<{ department: string; branch: string }>(http.patch(api.auth.me, { department })),

  /** The directory other users see: anonymous names only. */
  directory: () => unwrap<PublicUser[]>(http.get(api.auth.users)),

  user: (id: string) => unwrap<PublicUser>(http.get(api.auth.user(id))),

  registerInitiate: (payload: {
    fullName: string;
    email: string;
    department: string;
    branch: string;
  }) => unwrap<StepResult>(http.post(api.auth.registerInitiate, payload)),

  registerVerify: (email: string, otpCode: string) =>
    unwrap<StepResult>(http.post(api.auth.registerVerify, { email, otpCode })),

  registerComplete: (verificationToken: string, password: string, confirmPassword: string) =>
    unwrap<StepResult>(
      http.post(api.auth.registerComplete, { verificationToken, password, confirmPassword }),
    ),

  forgotPassword: (email: string) =>
    unwrap<StepResult>(http.post(api.auth.forgotPassword, { email })),

  verifyResetOtp: (email: string, otpCode: string) =>
    unwrap<StepResult>(http.post(api.auth.verifyResetOtp, { email, otpCode })),

  resetPassword: (resetToken: string, newPassword: string, confirmPassword: string) =>
    unwrap<StepResult>(
      http.post(api.auth.resetPassword, { resetToken, newPassword, confirmPassword }),
    ),

  /**
   * Raw JWT for the SignalR handshake — a WebSocket upgrade cannot carry a header.
   * Requires an authenticated caller; it is not a public token echo.
   */
  hubToken: () =>
    http
      .get<string>(api.auth.hubToken, {
        responseType: "text",
        transformResponse: [(data: string) => data],
      })
      .then((response) => response.data.trim()),
};

// ── Rooms and messages ────────────────────────────────────────────────────────

export const roomsApi = {
  /** Already filtered by the caller's branch and carrying their own unread counts. */
  list: () => unwrap<Room[]>(http.get(api.rooms.list)),

  byId: (roomId: string) => unwrap<Room>(http.get(api.rooms.byId(roomId))),

  join: (roomId: string) => unwrap<Room>(http.post(api.rooms.join(roomId))),

  leave: (roomId: string) => unwrap<void>(http.post(api.rooms.leave(roomId))),

  markRead: (roomId: string) => unwrap<void>(http.post(api.rooms.markRead(roomId))),

  members: (roomId: string) => unwrap<RoomMember[]>(http.get(api.rooms.members(roomId))),

  /** Newest-first page; pass a previous `nextCursor` as `before` to page older. */
  history: (roomId: string, before?: string, limit = 40) =>
    unwrap<CursorPage<Message>>(
      http.get(api.rooms.messages(roomId), { params: { before, limit } }),
    ),

  send: (
    roomId: string,
    payload: { content: string; replyToMessageId?: string; attachmentIds?: string[] },
  ) =>
    unwrap<Message>(
      http.post(api.rooms.messages(roomId), {
        content: payload.content,
        replyToMessageId: payload.replyToMessageId,
        attachmentIds: payload.attachmentIds ?? [],
      }),
    ),
};

export const messagesApi = {
  byId: (messageId: string) => unwrap<Message>(http.get(api.messages.byId(messageId))),

  edit: (messageId: string, content: string) =>
    unwrap<Message>(http.put(api.messages.byId(messageId), { content })),

  remove: (messageId: string) => unwrap<void>(http.delete(api.messages.byId(messageId))),

  /** The server decides add-or-remove and returns the resulting state. */
  toggleReaction: (messageId: string, emoji: string) =>
    unwrap<Message>(http.post(api.messages.reactions(messageId), { emoji })),

  readBy: (messageId: string) =>
    unwrap<ReadReceipt[]>(http.get(api.messages.readBy(messageId))),
};

export const filesApi = {
  /** Upload first, then pass the returned id as an attachment when sending. */
  upload: (file: File, onProgress?: (percent: number) => void) => {
    const form = new FormData();
    form.append("file", file);

    return unwrap<{
      id: string;
      fileName: string;
      contentType: string;
      sizeBytes: number;
      url: string;
    }>(
      http.post(api.files.upload, form, {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 120_000,
        onUploadProgress: (event) => {
          if (onProgress && event.total) {
            onProgress(Math.round((event.loaded / event.total) * 100));
          }
        },
      }),
    );
  },

  /** Absolute URL. Downloads are authorized by room membership on the server. */
  downloadUrl: (fileId: string) => `${config.apiUrl}${api.files.download(fileId)}`,
};

// ── Private chat ──────────────────────────────────────────────────────────────

export const conversationsApi = {
  list: () => unwrap<Conversation[]>(http.get(api.conversations.list)),

  /** Idempotent: the same pair always resolves to the same conversation. */
  start: (otherUserId: string) =>
    unwrap<Conversation>(http.post(api.conversations.start, { otherUserId })),

  byId: (conversationId: string) =>
    unwrap<Conversation>(http.get(api.conversations.byId(conversationId))),

  history: (conversationId: string, before?: string, limit = 40) =>
    unwrap<CursorPage<DirectMessage>>(
      http.get(api.conversations.messages(conversationId), { params: { before, limit } }),
    ),

  /** The recipient is derived from the conversation server-side, never sent. */
  send: (conversationId: string, payload: { content: string; replyToMessageId?: string }) =>
    unwrap<DirectMessage>(http.post(api.conversations.messages(conversationId), payload)),

  markRead: (conversationId: string) =>
    unwrap<void>(http.post(api.conversations.markRead(conversationId))),

  edit: (messageId: string, content: string) =>
    unwrap<DirectMessage>(http.put(api.directMessages.byId(messageId), { content })),

  remove: (messageId: string) => unwrap<void>(http.delete(api.directMessages.byId(messageId))),

  toggleReaction: (messageId: string, emoji: string) =>
    unwrap<DirectMessage>(http.post(api.directMessages.reactions(messageId), { emoji })),
};

export const blocksApi = {
  list: () => unwrap<string[]>(http.get(api.blocks.list)),
  /** The blocker is always the caller. */
  block: (userId: string) => unwrap<void>(http.post(api.blocks.byUser(userId))),
  unblock: (userId: string) => unwrap<void>(http.delete(api.blocks.byUser(userId))),
};

// ── Polls ─────────────────────────────────────────────────────────────────────

export const pollsApi = {
  list: (limit = 50) => unwrap<Poll[]>(http.get(api.polls.list, { params: { limit } })),

  byId: (pollId: string) => unwrap<Poll>(http.get(api.polls.byId(pollId))),

  create: (question: string, options: string[]) =>
    unwrap<Poll>(http.post(api.polls.create, { question, options })),

  /** `null` withdraws the caller's vote. The voter is the authenticated caller. */
  vote: (pollId: string, optionId: string | null) =>
    unwrap<Poll>(http.post(api.polls.vote(pollId), { optionId })),

  react: (pollId: string, isUpvote: boolean | null) =>
    unwrap<Poll>(http.post(api.polls.reaction(pollId), { isUpvote })),

  /** Creator or admin. */
  close: (pollId: string) => unwrap<void>(http.post(api.polls.close(pollId))),

  /** Admin only. */
  remove: (pollId: string) => unwrap<void>(http.delete(api.polls.byId(pollId))),
};

// ── Notifications ─────────────────────────────────────────────────────────────

export const notificationsApi = {
  /** Always the caller's own; there is no user-id parameter. */
  list: (limit = 50, unreadOnly = false) =>
    unwrap<AppNotification[]>(http.get(api.notifications.list, { params: { limit, unreadOnly } })),

  unreadCount: () =>
    unwrap<{ unread: number }>(http.get(api.notifications.unreadCount)).then((r) => r.unread),

  markRead: (id: string) => unwrap<void>(http.post(api.notifications.markRead(id))),

  markAllRead: () => unwrap<void>(http.post(api.notifications.markAllRead)),

  remove: (id: string) => unwrap<void>(http.delete(api.notifications.remove(id))),

  subscribePush: (subscription: { endpoint: string; p256dh: string; auth: string }) =>
    unwrap<void>(http.post(api.notifications.subscribePush, subscription)),

  /**
   * Takes the whole subscription, not just the endpoint: every field on
   * `PushSubscriptionRequest` is `[Required]`, so posting empty keys fails validation
   * before the server ever reads the endpoint it is meant to remove.
   */
  unsubscribePush: (subscription: { endpoint: string; p256dh: string; auth: string }) =>
    unwrap<void>(http.post(api.notifications.unsubscribePush, subscription)),
};

// ── Reporting ─────────────────────────────────────────────────────────────────

export const reportsApi = {
  /** The reporter is taken from the session, not the payload. */
  submit: (kind: ReportTargetKind, messageId: string, reason: string) =>
    unwrap<Report>(http.post(api.reports.submit, { kind, messageId, reason })),

  /** Admin only — the queue exposes reporter and author names. */
  queue: (status?: ReportStatus, page = 1, pageSize = 50) =>
    unwrap<PagedResult<Report>>(http.get(api.reports.queue, { params: { status, page, pageSize } })),

  /** Removes the reported message, then closes the report. */
  action: (reportId: string, note?: string) =>
    unwrap<void>(http.post(api.reports.action(reportId), { note })),

  dismiss: (reportId: string, note?: string) =>
    unwrap<void>(http.post(api.reports.dismiss(reportId), { note })),
};

// ── Admin ─────────────────────────────────────────────────────────────────────

export const adminApi = {
  stats: () => unwrap<DashboardStats>(http.get(api.admin.stats)),

  recentActivity: (count = 20) =>
    unwrap<AuditLogEntry[]>(http.get(api.admin.recentActivity, { params: { count } })),

  moderationSettings: () => unwrap<ModerationSettings>(http.get(api.admin.moderationSettings)),

  updateModerationSettings: (settings: Omit<ModerationSettings, "updatedAt">) =>
    unwrap<ModerationSettings>(http.put(api.admin.moderationSettings, settings)),

  runAutoModeration: () =>
    unwrap<{ authorsActioned: number }>(http.post(api.admin.runAutoModeration)),

  blockedUsers: () => unwrap<BlockedUser[]>(http.get(api.admin.blockedUsers)),

  blockUser: (userId: string, reason: string) =>
    unwrap<void>(http.post(api.admin.blockUser(userId), { reason })),

  unblockUser: (userId: string) => unwrap<void>(http.delete(api.admin.blockUser(userId))),

  auditLogs: (page = 1, pageSize = 50, entityType?: string) =>
    unwrap<PagedResult<AuditLogEntry>>(
      http.get(api.admin.auditLogs, { params: { page, pageSize, entityType } }),
    ),

  users: (params: {
    page?: number;
    pageSize?: number;
    search?: string;
    status?: string;
    branch?: string;
    department?: string;
    sortBy?: string;
    sortDesc?: boolean;
  }) => unwrap<PagedResult<AdminUser>>(http.get(api.admin.users, { params })),

  deleteUser: (userId: string, reason: string) =>
    unwrap<void>(http.delete(api.admin.userById(userId), { data: { reason } })),

  setUserBranch: (userId: string, branch: string) =>
    unwrap<void>(http.put(api.admin.setUserBranch(userId), { branch })),

  /**
   * Analytics. Each series is wrapped in Availability so a chart can render
   * "unavailable" instead of a flat zero line when its owning service is down.
   */
  analytics: {
    messagesPerDay: (days = 30) =>
      unwrap<Availability<DailyCount[]>>(
        http.get(api.admin.analytics.messagesPerDay, { params: { days } }),
      ),

    messagesPerHour: () =>
      unwrap<Availability<NamedCount[]>>(http.get(api.admin.analytics.messagesPerHour)),

    directMessagesPerDay: (days = 30) =>
      unwrap<Availability<DailyCount[]>>(
        http.get(api.admin.analytics.directMessagesPerDay, { params: { days } }),
      ),

    pollsPerDay: (days = 30) =>
      unwrap<Availability<DailyCount[]>>(
        http.get(api.admin.analytics.pollsPerDay, { params: { days } }),
      ),

    notificationsPerDay: (days = 30) =>
      unwrap<Availability<DailyCount[]>>(
        http.get(api.admin.analytics.notificationsPerDay, { params: { days } }),
      ),

    topRooms: (top = 10) =>
      unwrap<Availability<RoomActivity[]>>(
        http.get(api.admin.analytics.topRooms, { params: { top } }),
      ),

    topAuthors: (top = 10) =>
      unwrap<Availability<NamedCount[]>>(
        http.get(api.admin.analytics.topAuthors, { params: { top } }),
      ),

    topPolls: (top = 10) =>
      unwrap<Availability<NamedCount[]>>(
        http.get(api.admin.analytics.topPolls, { params: { top } }),
      ),

    /** Computed inside the admin database, so always available. */
    reportsPerDay: (days = 30) =>
      unwrap<DailyCount[]>(http.get(api.admin.analytics.reportsPerDay, { params: { days } })),

    reportReasons: (top = 10) =>
      unwrap<NamedCount[]>(http.get(api.admin.analytics.reportReasons, { params: { top } })),

    roomHealth: (top = 10) =>
      unwrap<Availability<RoomHealth[]>>(
        http.get(api.admin.analytics.roomHealth, { params: { top } }),
      ),
  },
};

export const chatAdminApi = {
  rooms: (includeArchived = false) =>
    unwrap<Room[]>(http.get(api.chatAdmin.rooms, { params: { includeArchived } })),

  createRoom: (payload: {
    name: string;
    description: string;
    type: string;
    branch?: string;
  }) => unwrap<Room>(http.post(api.chatAdmin.rooms, payload)),

  updateRoom: (roomId: string, name: string, description: string) =>
    unwrap<Room>(http.put(api.chatAdmin.roomById(roomId), { name, description })),

  /** Archives rather than deletes; messages are retained. */
  archiveRoom: (roomId: string) => unwrap<void>(http.delete(api.chatAdmin.roomById(roomId))),

  restoreRoom: (roomId: string) => unwrap<void>(http.post(api.chatAdmin.restoreRoom(roomId))),

  roomMembers: (roomId: string) =>
    unwrap<RoomMember[]>(http.get(api.chatAdmin.roomMembers(roomId))),

  moderationStats: () => unwrap<ModerationStats>(http.get(api.chatAdmin.moderationStats)),
};

export { ApiError, errorMessage } from "./client";
