import { adminApiClient } from "./client";

// ── Types ─────────────────────────────────────────────────────────────────────

export interface DashboardStats {
    totalUsers: number;
    activeUsers: number;
    deletedUsers: number;
    totalChatRooms: number;
    totalPrivateConversations: number;
    totalMessages: number;
    totalPolls: number;
    totalNotifications: number;
    totalReports: number;
    pendingReports: number;
}

export interface RecentActivity {
    id: string;
    activityType: string;
    description: string;
    targetId: string;
    targetType: string;
    timestamp: string;
}

export interface AdminUser {
    id: string;
    anonymousName: string;
    department: string;
    branch: string;
    createdAt: string | null;
    isDeleted: boolean;
    deletedAt: string | null;
    deletedBy: string | null;
}

export interface ReportDto {
    id: string;
    messageId: string;
    messageContent: string;
    messageAuthorId: string;
    messageAuthorName: string;
    messageType: number;
    messageTypeName: string;
    reportedByUserId: string;
    reportedByUserName: string;
    reason: string;
    reportedAt: string;
    status: number;
    statusName: string;
    isAutoRemoved: boolean;
}

export interface RoomDto {
    id: string;
    name: string;
    description: string;
    createdAt: string;
    updatedAt: string | null;
    isDeleted: boolean;
    deletedAt: string | null;
    createdByAdmin: string;
    createdByAdminName: string;
    memberCount: number;
}

export interface AuditLog {
    id: string;
    action: string;
    targetType: string;
    targetId: string;
    performedBy: string;
    timestamp: string;
}

export interface DailyCount {
    date: string;
    count: number;
}

export interface ActiveItem {
    name: string;
    count: number;
}

export interface MostVotedPoll {
    pollId: string;
    question: string;
    totalVotes: number;
}

export interface ReportReason {
    reason: string;
    count: number;
}

export interface ModerationSettings {
    reportThreshold: number;
    autoDeleteEnabled: boolean;
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

export const getDashboardStats = async (): Promise<DashboardStats> => {
    const r = await adminApiClient.get("/api/admin/dashboard/stats");
    return r.data;
};

export const getRecentActivity = async (count = 20): Promise<RecentActivity[]> => {
    const r = await adminApiClient.get(`/api/admin/dashboard/recent-activity?count=${count}`);
    return r.data;
};

// ── Users ─────────────────────────────────────────────────────────────────────

export const getAdminUsers = async (): Promise<AdminUser[]> => {
    const r = await adminApiClient.get("/api/admin/users");
    return r.data;
};

export const searchAdminUsers = async (q: string): Promise<AdminUser[]> => {
    const r = await adminApiClient.get(`/api/admin/users/search?q=${encodeURIComponent(q)}`);
    return r.data;
};

export const deleteUser = async (id: string, reason: string): Promise<void> => {
    await adminApiClient.delete(`/api/admin/users/${id}`, { data: { reason } });
};

// ── Moderation ────────────────────────────────────────────────────────────────

export const getReports = async (status?: number, isAutoRemoved?: boolean, page = 1, pageSize = 50): Promise<ReportDto[]> => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (status !== undefined) params.set("status", String(status));
    if (isAutoRemoved !== undefined) params.set("isAutoRemoved", String(isAutoRemoved));
    const r = await adminApiClient.get(`/api/admin/reports?${params}`);
    return r.data;
};

export const markReportAsReviewed = async (reportId: string): Promise<void> => {
    await adminApiClient.post(`/api/admin/reports/${reportId}/review`);
};

export const ignoreReport = async (reportId: string): Promise<void> => {
    await adminApiClient.post(`/api/admin/reports/${reportId}/ignore`);
};

export const deleteReportedMessage = async (messageId: string): Promise<void> => {
    await adminApiClient.delete(`/api/admin/messages/${messageId}`);
};

export const deleteReportedUser = async (userId: string): Promise<void> => {
    await adminApiClient.delete(`/api/admin/users/${userId}`, { data: { reason: "Moderation action: User removed." } });
};

export const getModerationSettings = async (): Promise<ModerationSettings> => {
    const r = await adminApiClient.get("/api/admin/moderation/settings");
    return r.data;
};

export const updateModerationSettings = async (settings: ModerationSettings): Promise<ModerationSettings> => {
    const r = await adminApiClient.put("/api/admin/moderation/settings", settings);
    return r.data;
};

// ── Rooms ─────────────────────────────────────────────────────────────────────

export const getAdminRooms = async (includeDeleted = false): Promise<RoomDto[]> => {
    const r = await adminApiClient.get(`/api/admin/rooms?includeDeleted=${includeDeleted}`);
    return r.data;
};

export const createAdminRoom = async (name: string, description: string): Promise<RoomDto> => {
    const r = await adminApiClient.post("/api/admin/rooms", { name, description });
    return r.data;
};

export const deleteAdminRoom = async (id: string): Promise<void> => {
    await adminApiClient.delete(`/api/admin/rooms/${id}`);
};

// ── Audit Logs ────────────────────────────────────────────────────────────────

export const getAuditLogs = async (page = 1, pageSize = 50): Promise<{ page: number; pageSize: number; totalCount: number; data: AuditLog[] }> => {
    const r = await adminApiClient.get(`/api/admin/audit-logs?page=${page}&pageSize=${pageSize}`);
    return r.data;
};

// ── Analytics ─────────────────────────────────────────────────────────────────

export const getUserGrowth = async (days = 30): Promise<DailyCount[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/user-growth?days=${days}`);
    return r.data;
};

export const getDailyMessages = async (days = 30): Promise<DailyCount[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/daily-messages?days=${days}`);
    return r.data;
};

export const getPrivateChatVolume = async (days = 30): Promise<DailyCount[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/private-chat-volume?days=${days}`);
    return r.data;
};

export const getDailyPolls = async (days = 30): Promise<DailyCount[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/daily-polls?days=${days}`);
    return r.data;
};

export const getDailyNotifications = async (days = 30): Promise<DailyCount[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/daily-notifications?days=${days}`);
    return r.data;
};

export const getReportTrends = async (days = 30): Promise<DailyCount[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/report-trends?days=${days}`);
    return r.data;
};

export const getMostActiveRooms = async (top = 10): Promise<ActiveItem[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/most-active-rooms?top=${top}`);
    return r.data;
};

export const getMostActiveUsers = async (top = 10): Promise<ActiveItem[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/most-active-users?top=${top}`);
    return r.data;
};

export const getMostVotedPolls = async (top = 10): Promise<MostVotedPoll[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/most-voted-polls?top=${top}`);
    return r.data;
};

export const getReportReasons = async (): Promise<ReportReason[]> => {
    const r = await adminApiClient.get("/api/admin/analytics/report-reasons");
    return r.data;
};

// ── New Analytics (Charts Redesign) ─────────────────────────────────────────

export interface RoomHealth {
    roomName: string;
    messageCount: number;
    reportCount: number;
    reportRate: number;
    health: "Healthy" | "Monitor" | "Critical";
}

export interface PollParticipation {
    pollQuestion: string;
    totalVotes: number;
    participationRate: number;
}

export interface HourlyActivity {
    hour: number;
    messageCount: number;
}

export interface RoomSentiment {
    roomName: string;
    positive: number;
    neutral: number;
    negative: number;
}

export const getActiveRoomsTyped = async (top = 8): Promise<{ roomId: string; roomName: string; messageCount: number }[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/active-rooms?top=${top}`);
    return r.data;
};

export const getRoomHealth = async (top = 10): Promise<RoomHealth[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/room-health?top=${top}`);
    return r.data;
};

export const getPollParticipation = async (top = 6): Promise<PollParticipation[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/poll-participation?top=${top}`);
    return r.data;
};

export const getHourlyActivity = async (): Promise<HourlyActivity[]> => {
    const r = await adminApiClient.get("/api/admin/analytics/hourly-activity");
    return r.data;
};

export const getRoomSentiment = async (top = 8): Promise<RoomSentiment[]> => {
    const r = await adminApiClient.get(`/api/admin/analytics/room-sentiment?top=${top}`);
    return r.data;
};

