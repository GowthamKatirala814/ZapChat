using System.ComponentModel.DataAnnotations;
using Admin.Domain.Documents;
using ZapChat.Shared.Results;

namespace Admin.Application;

// ── DTOs ────────────────────────────────────────────────────────────────────────

public sealed record ReportDto(
    Guid Id,
    ReportTargetKind Kind,
    Guid MessageId,
    string ContentSnapshot,
    Guid AuthorUserId,
    string AuthorAnonymousName,
    string? RoomName,
    Guid ReportedByUserId,
    string ReportedByAnonymousName,
    string Reason,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    /// <summary>Distinct reporters against this author, and the configured threshold.</summary>
    int AuthorReportCount,
    int Threshold);

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    string EntityId,
    Guid ActorUserId,
    string ActorName,
    bool IsSystem,
    string? Details,
    DateTime Timestamp);

public sealed record BlockedUserDto(
    Guid UserId, string AnonymousName, string Reason,
    DateTime BlockedAt, string Source);

public sealed record ModerationSettingsDto(
    int ReportThreshold,
    bool AutoActionEnabled,
    bool AutoRemoveMessages,
    bool AutoDisableAccount,
    DateTime UpdatedAt);

/// <summary>
/// Dashboard figures.
///
/// Every count is wrapped so "could not be determined" is a distinct state from zero.
/// The old dashboard returned 0 from a bare catch, making an unreachable service
/// indistinguishable from a genuine zero.
/// </summary>
public sealed record DashboardStatsDto(
    Availability<long> TotalUsers,
    Availability<long> ActiveUsers,
    Availability<long> DeletedUsers,
    long BlockedUsers,
    Availability<long> TotalRooms,
    Availability<long> TotalMessages,
    Availability<long> TotalConversations,
    Availability<long> TotalDirectMessages,
    Availability<long> TotalPolls,
    Availability<long> TotalNotifications,
    long TotalReports,
    long PendingReports);

public sealed record DailyCountDto(string Date, int Count);

public sealed record NamedCountDto(string Name, long Count);

/// <summary>Room health, computed by joining chat activity with the report counts here.</summary>
public sealed record RoomHealthDto(
    Guid RoomId, string RoomName, int MessageCount, int ReportCount,
    double ReportRate, string Health);

// ── Requests ────────────────────────────────────────────────────────────────────

/// <summary>
/// Submitting a report. There is no reporter field — identity comes from the token.
/// The old endpoint was [AllowAnonymous] and took reportedByUserId from the body, which
/// combined with a weak soft-delete endpoint to let any signed-in user delete any
/// account with five forged reports.
/// </summary>
public sealed class SubmitReportRequest
{
    [Required]
    public ReportTargetKind Kind { get; set; }

    [Required]
    public Guid MessageId { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class ResolveReportRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public sealed class BlockUserRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateModerationSettingsRequest
{
    [Range(2, 100)]
    public int ReportThreshold { get; set; } = 5;

    public bool AutoActionEnabled { get; set; } = true;
    public bool AutoRemoveMessages { get; set; } = true;
    public bool AutoDisableAccount { get; set; } = true;
}

public sealed class ReportQuery
{
    public ReportStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ── Repositories ────────────────────────────────────────────────────────────────

public interface IReportRepository
{
    Task<ReportDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Inserts, relying on the unique index over (target.messageId, reportedBy.userId).
    /// Returns false when this caller has already reported that message.
    /// </summary>
    Task<bool> TryInsertAsync(ReportDocument report, CancellationToken ct = default);

    Task<PagedResult<ReportDocument>> SearchAsync(ReportQuery query, CancellationToken ct = default);

    Task<bool> ResolveAsync(
        Guid id, ReportStatus status, Guid resolvedBy, string? note, CancellationToken ct = default);

    /// <summary>Distinct reporters against one author across all their reported messages.</summary>
    Task<int> CountDistinctReportersForAuthorAsync(
        Guid authorUserId, CancellationToken ct = default);

    /// <summary>Authors at or above the threshold, for the automated rule.</summary>
    Task<IReadOnlyList<(Guid AuthorUserId, string AuthorName, int Reporters)>>
        FindAuthorsOverThresholdAsync(int threshold, CancellationToken ct = default);

    Task<long> ResolvePendingForAuthorAsync(
        Guid authorUserId, ReportStatus status, string note, CancellationToken ct = default);

    Task<long> CountAsync(ReportStatus? status = null, CancellationToken ct = default);

    Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default);

    Task<IReadOnlyList<(string Reason, int Count)>> CountByReasonAsync(
        int top, CancellationToken ct = default);

    Task<IReadOnlyList<(Guid RoomId, int Count)>> CountByRoomAsync(CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task InsertAsync(AuditLogDocument document, CancellationToken ct = default);

    Task<PagedResult<AuditLogDocument>> SearchAsync(
        int page, int pageSize, string? entityType, string? entityId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogDocument>> RecentAsync(int count, CancellationToken ct = default);
}

public interface IBlockedUserRepository
{
    Task<bool> BlockAsync(BlockedUserDocument document, CancellationToken ct = default);
    Task<bool> UnblockAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsBlockedAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<BlockedUserDocument>> ListAsync(CancellationToken ct = default);
    Task<long> CountAsync(CancellationToken ct = default);
}

public interface IModerationSettingsRepository
{
    Task<ModerationSettingsDocument> GetAsync(CancellationToken ct = default);

    Task<ModerationSettingsDocument> UpdateAsync(
        UpdateModerationSettingsRequest request, Guid updatedBy, CancellationToken ct = default);
}

// ── Cross-service gateways ──────────────────────────────────────────────────────

/// <summary>
/// Everything Admin needs from the other services. One interface, so the call sites are
/// visible in one place instead of five ad-hoc HttpClient blocks with duplicated
/// response records.
/// </summary>
public interface IPlatformGateway
{
    Task<MessageSnapshot?> GetMessageAsync(
        ReportTargetKind kind, Guid messageId, CancellationToken ct = default);

    /// <summary>Actually removes the message. This is the call the old admin path never made.</summary>
    Task<bool> RemoveMessageAsync(
        ReportTargetKind kind, Guid messageId, string reason, CancellationToken ct = default);

    Task<long> RemoveAllMessagesByAuthorAsync(
        Guid authorUserId, string reason, CancellationToken ct = default);

    Task<bool> DisableAccountAsync(Guid userId, string reason, CancellationToken ct = default);

    Task<string?> GetEmailHashAsync(Guid userId, CancellationToken ct = default);

    Task<string?> GetAnonymousNameAsync(Guid userId, CancellationToken ct = default);

    // Dashboard figures, each independently available or not.
    Task<Availability<UserCounts>> GetUserCountsAsync(CancellationToken ct = default);
    Task<Availability<ChatCounts>> GetChatCountsAsync(CancellationToken ct = default);
    Task<Availability<PrivateChatCounts>> GetPrivateChatCountsAsync(CancellationToken ct = default);
    Task<Availability<long>> GetPollCountAsync(CancellationToken ct = default);
    Task<Availability<long>> GetNotificationCountAsync(CancellationToken ct = default);

    Task<Availability<IReadOnlyList<DailyCountDto>>> GetSeriesAsync(
        string service, string path, int days, CancellationToken ct = default);

    Task<Availability<IReadOnlyList<NamedCountDto>>> GetNamedCountsAsync(
        string service, string path, int top, CancellationToken ct = default);

    Task<Availability<IReadOnlyList<RoomActivity>>> GetRoomActivityAsync(
        int top, CancellationToken ct = default);
}

public sealed record MessageSnapshot(
    Guid Id, string Content, Guid AuthorUserId, string AuthorAnonymousName,
    Guid? RoomId, string? RoomName);

public sealed record UserCounts(long Total, long Active, long Deleted);
public sealed record ChatCounts(long Rooms, long Messages);
public sealed record PrivateChatCounts(long Conversations, long Messages);
public sealed record RoomActivity(Guid RoomId, string RoomName, int MessageCount);

// ── Services ────────────────────────────────────────────────────────────────────

public interface IReportService
{
    /// <summary>Submits a report as the authenticated caller.</summary>
    Task<ReportDto> SubmitAsync(SubmitReportRequest request, CancellationToken ct = default);

    Task<PagedResult<ReportDto>> SearchAsync(ReportQuery query, CancellationToken ct = default);

    /// <summary>Removes the reported message and closes the report.</summary>
    Task ActionAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct = default);

    Task DismissAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct = default);
}

/// <summary>
/// The one automated moderation implementation. The old system had two that disagreed:
/// a hardcoded ">= 5" inside the report controller that ignored the configured settings,
/// and a background service that read them — and the two used conflicting report states.
/// </summary>
public interface IAutoModerationService
{
    Task<int> RunAsync(CancellationToken ct = default);
}

public interface IAdminUserService
{
    Task BlockAsync(Guid userId, BlockUserRequest request, CancellationToken ct = default);
    Task UnblockAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<BlockedUserDto>> ListBlockedAsync(CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> GetRecentActivityAsync(int count, CancellationToken ct = default);
}

public interface IAuditLogService
{
    Task LogAsync(
        string action, string entityType, string entityId,
        string? details = null, CancellationToken ct = default);

    Task LogSystemAsync(
        string action, string entityType, string entityId,
        string? details = null, CancellationToken ct = default);

    Task<PagedResult<AuditLogDto>> SearchAsync(
        int page, int pageSize, string? entityType, string? entityId,
        CancellationToken ct = default);
}
