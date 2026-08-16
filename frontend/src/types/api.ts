/**
 * API contract types, mirroring the backend DTOs one-for-one.
 *
 * Two properties of these shapes are load-bearing for the product:
 *
 *  1. No type here carries another user's real identity. A message identifies its
 *     author by `anonymousName` plus an `isMine` flag the server computes for the
 *     requesting caller — so ownership works without ever disclosing who wrote what.
 *     `MyProfile` is the single exception and is only ever returned for the caller.
 *
 *  2. Enums arrive as names, not ordinals. `roomType === 1` breaks the moment a value
 *     is inserted into the enum; `"Branch"` does not.
 */

// ── Identity ──────────────────────────────────────────────────────────────────

export type Role = "user" | "admin";

/** Returned by login and refresh. Carries no token — those are HttpOnly cookies. */
export interface AuthResult {
  userId: string;
  anonymousName: string;
  email: string;
  role: Role;
}

/** The caller's own account. Real identity appears here and nowhere else. */
export interface MyProfile {
  userId: string;
  email: string;
  fullName: string;
  department: string;
  /** Read-only to the user: branch gates room access, so an admin manages it. */
  branch: string;
  anonymousName: string;
  createdAt: string;
  roles: string[];
}

/** What one user may learn about another. No email, no real name — by construction. */
export interface PublicUser {
  id: string;
  anonymousName: string;
  department: string;
  branch: string;
  createdAt: string;
  isDeleted: boolean;
}

/** Uniform envelope for the multi-step registration and password-reset flows. */
export interface StepResult {
  success: boolean;
  message: string;
  /** One-time token required by the next step. */
  token?: string;
}

// ── Rooms and messages ────────────────────────────────────────────────────────

export type RoomType = "General" | "Branch" | "Hr" | "Custom";

export interface Room {
  id: string;
  name: string;
  type: RoomType;
  /** Set only for Branch rooms. */
  branch?: string;
  description: string;
  memberCount: number;
  messageCount: number;
  isArchived: boolean;
  createdAt: string;
  lastMessage?: LastMessage;
  /** This caller's unread count, from the server. Never derived client-side. */
  unreadCount: number;
  isMember: boolean;
}

export interface LastMessage {
  messageId: string;
  preview: string;
  authorName: string;
  sentAt: string;
}

/** Who removed a message. The UI renders a different placeholder for each. */
export type DeletionKind = "None" | "User" | "Moderation";

export interface Message {
  id: string;
  roomId: string;
  anonymousName: string;
  /**
   * Server-computed for the requesting caller.
   *
   * Always `false` on a SignalR group broadcast: one payload goes to a whole room, so
   * it cannot carry a correct per-recipient value. The sender's own copy comes from
   * the send call's return value.
   */
  isMine: boolean;
  /** Empty string once the message has been removed. */
  content: string;
  sentAt: string;
  replyTo?: ReplyReference;
  reactions: Reaction[];
  attachments: Attachment[];
  isEdited: boolean;
  editedAt?: string;
  deletedBy: DeletionKind;
  deletedAt?: string;
}

export interface ReplyReference {
  messageId: string;
  /** Snapshot taken when the reply was written; editing the parent does not rewrite it. */
  snippet: string;
  authorName: string;
}

export interface Reaction {
  emoji: string;
  count: number;
  /** Whether the requesting caller is in this group. */
  mine: boolean;
  names: string[];
}

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  /** Relative path; resolve against the gateway origin. */
  url: string;
}

export interface RoomMember {
  userId: string;
  anonymousName: string;
  isOnline: boolean;
}

export interface ReadReceipt {
  anonymousName: string;
  lastReadAt: string;
}

/**
 * Cursor pagination. Pass `nextCursor` back as `before` for the next older page.
 * Offset paging would duplicate or skip messages as new ones arrive mid-scroll.
 */
export interface CursorPage<T> {
  items: T[];
  nextCursor?: string;
  hasMore: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ── Private chat ──────────────────────────────────────────────────────────────

export interface Conversation {
  id: string;
  otherUserId: string;
  otherAnonymousName: string;
  unreadCount: number;
  lastMessage?: ConversationLastMessage;
  isBlockedByMe: boolean;
  hasBlockedMe: boolean;
}

export interface ConversationLastMessage {
  messageId: string;
  preview: string;
  senderName: string;
  sentByMe: boolean;
  sentAt: string;
}

export interface DirectMessage {
  id: string;
  conversationId: string;
  senderName: string;
  isMine: boolean;
  content: string;
  sentAt: string;
  /** Set once the recipient has read it. Drives the read tick. */
  readAt?: string;
  replyTo?: ReplyReference;
  reactions: Reaction[];
  attachments: Attachment[];
  isEdited: boolean;
  editedAt?: string;
  deletedBy: DeletionKind;
  deletedAt?: string;
}

// ── Polls ─────────────────────────────────────────────────────────────────────

export type PollStatus = "Open" | "Closed" | "Removed";

export interface Poll {
  id: string;
  question: string;
  creatorName: string;
  isMine: boolean;
  options: PollOption[];
  totalVotes: number;
  upvotes: number;
  downvotes: number;
  status: PollStatus;
  createdAt: string;
  /** The option this caller chose, if any. */
  myVoteOptionId?: string;
  /** true up, false down, null/undefined none. */
  myReaction?: boolean | null;
}

export interface PollOption {
  id: string;
  text: string;
  voteCount: number;
  percentage: number;
}

// ── Notifications ─────────────────────────────────────────────────────────────

export type NotificationType =
  | "Message"
  | "Mention"
  | "Reply"
  | "Moderation"
  | "System";

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  /** The message that produced this; used to withdraw it if that message is deleted. */
  sourceId?: string;
  createdAt: string;
}

// ── Moderation and admin ──────────────────────────────────────────────────────

export type ReportTargetKind = "RoomMessage" | "DirectMessage";
export type ReportStatus = "Pending" | "Actioned" | "Dismissed" | "AutoActioned";

export interface Report {
  id: string;
  kind: ReportTargetKind;
  messageId: string;
  /** What the message said when it was reported, even if edited since. */
  contentSnapshot: string;
  authorUserId: string;
  authorAnonymousName: string;
  roomName?: string;
  reportedByUserId: string;
  reportedByAnonymousName: string;
  reason: string;
  status: ReportStatus;
  createdAt: string;
  resolvedAt?: string;
  /** Distinct reporters against this author, and the configured threshold. */
  authorReportCount: number;
  threshold: number;
}

export interface ModerationSettings {
  reportThreshold: number;
  autoActionEnabled: boolean;
  autoRemoveMessages: boolean;
  autoDisableAccount: boolean;
  updatedAt: string;
}

export interface BlockedUser {
  userId: string;
  anonymousName: string;
  reason: string;
  blockedAt: string;
  source: "Manual" | "AutoModeration";
}

export interface AuditLogEntry {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  actorUserId: string;
  actorName: string;
  /** True when the automated moderation rule performed it, not a person. */
  isSystem: boolean;
  details?: string;
  timestamp: string;
}

export interface AdminUser {
  id: string;
  anonymousName: string;
  department: string;
  branch: string;
  createdAt: string;
  isActive: boolean;
  isDeleted: boolean;
  deletedAt?: string;
  deletedBy?: string;
  deletionReason?: string;
  roles: string[];
  isLockedOut: boolean;
}

/**
 * A value that may not have been computable.
 *
 * This is what stops a dashboard tile rendering `0` when a service was unreachable.
 * Every cross-service figure is wrapped, so "no data" and "the query failed" are
 * distinguishable in the UI.
 */
export interface Availability<T> {
  isAvailable: boolean;
  value?: T;
  reason?: string;
}

export interface DashboardStats {
  totalUsers: Availability<number>;
  activeUsers: Availability<number>;
  deletedUsers: Availability<number>;
  /** Local to the admin database, so always available. */
  blockedUsers: number;
  totalRooms: Availability<number>;
  totalMessages: Availability<number>;
  totalConversations: Availability<number>;
  totalDirectMessages: Availability<number>;
  totalPolls: Availability<number>;
  totalNotifications: Availability<number>;
  totalReports: number;
  pendingReports: number;
}

export interface DailyCount {
  date: string;
  count: number;
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface RoomActivity {
  roomId: string;
  roomName: string;
  messageCount: number;
}

export interface RoomHealth {
  roomId: string;
  roomName: string;
  messageCount: number;
  reportCount: number;
  reportRate: number;
  health: "Healthy" | "Monitor" | "Critical";
}

export interface ModerationStats {
  total: number;
  allowed: number;
  blocked: number;
  geminiRequests: number;
  ruleRequests: number;
  blockedByCategory: Record<string, number>;
  topMatchedRules: Record<string, number>;
}

// ── Errors ────────────────────────────────────────────────────────────────────

/** The single error shape every service returns. */
export interface ApiErrorBody {
  code: string;
  message: string;
  traceId: string;
  errors?: Record<string, string[]>;
  /** Present on a 422 moderation rejection: the category that blocked it. */
  category?: string;
}
