using Admin.Domain.Documents;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;
using ZapChat.Shared.Results;

namespace Admin.Application.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _logs;
    private readonly ICurrentUser _currentUser;

    public AuditLogService(IAuditLogRepository logs, ICurrentUser currentUser)
    {
        _logs = logs;
        _currentUser = currentUser;
    }

    public Task LogAsync(
        string action, string entityType, string entityId,
        string? details = null, CancellationToken ct = default) =>
        _logs.InsertAsync(new AuditLogDocument
        {
            Action = action,
            Entity = new AuditEntity { Type = entityType, Id = entityId },
            Actor = new AuditActor
            {
                // The acting admin comes from the token, so audit attribution cannot be
                // forged by putting an adminId in the request body.
                UserId = _currentUser.UserId ?? Guid.Empty,
                Name = _currentUser.AnonymousName
            },
            Details = details
        }, ct);

    public Task LogSystemAsync(
        string action, string entityType, string entityId,
        string? details = null, CancellationToken ct = default) =>
        _logs.InsertAsync(new AuditLogDocument
        {
            Action = action,
            Entity = new AuditEntity { Type = entityType, Id = entityId },
            Actor = new AuditActor { UserId = Guid.Empty, Name = "system:auto-moderation" },
            Details = details
        }, ct);

    public async Task<PagedResult<AuditLogDto>> SearchAsync(
        int page, int pageSize, string? entityType, string? entityId,
        CancellationToken ct = default)
    {
        var result = await _logs.SearchAsync(page, pageSize, entityType, entityId, ct);

        return new PagedResult<AuditLogDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    internal static AuditLogDto ToDto(AuditLogDocument a) => new(
        a.Id, a.Action, a.Entity.Type, a.Entity.Id,
        a.Actor.UserId, a.Actor.Name, a.Actor.IsSystem, a.Details, a.Timestamp);
}

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reports;
    private readonly IModerationSettingsRepository _settings;
    private readonly IPlatformGateway _platform;
    private readonly IAuditLogService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reports,
        IModerationSettingsRepository settings,
        IPlatformGateway platform,
        IAuditLogService audit,
        ICurrentUser currentUser,
        ILogger<ReportService> logger)
    {
        _reports = reports;
        _settings = settings;
        _platform = platform;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// The one report creation path. Previously there were three, with different
    /// behaviour: an anonymous POST /api/reports that ran its own hardcoded threshold
    /// rule, POST /api/admin/reports that ran none, and a Chat endpoint that forwarded
    /// to the first.
    /// </summary>
    public async Task<ReportDto> SubmitAsync(
        SubmitReportRequest request, CancellationToken ct = default)
    {
        var reporterId = _currentUser.RequireUserId();

        var snapshot = await _platform.GetMessageAsync(request.Kind, request.MessageId, ct);

        if (snapshot is null)
        {
            throw new NotFoundException(
                "That message no longer exists, so it cannot be reported.");
        }

        if (snapshot.AuthorUserId == reporterId)
            throw new ValidationException("You cannot report your own message.");

        var report = new ReportDocument
        {
            Id = Guid.NewGuid(),
            Target = new ReportTarget
            {
                Kind = request.Kind,
                MessageId = request.MessageId,
                ContentSnapshot = snapshot.Content,
                AuthorUserId = snapshot.AuthorUserId,
                AuthorAnonymousName = snapshot.AuthorAnonymousName,
                RoomId = snapshot.RoomId,
                RoomName = snapshot.RoomName
            },
            ReportedBy = new Reporter
            {
                UserId = reporterId,
                AnonymousName = _currentUser.AnonymousName
            },
            Reason = request.Reason.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        if (!await _reports.TryInsertAsync(report, ct))
            throw new ConflictException("You have already reported this message.");

        _logger.LogInformation(
            "Report {ReportId} filed against message {MessageId}.", report.Id, request.MessageId);

        var settings = await _settings.GetAsync(ct);

        var reporters = await _reports.CountDistinctReportersForAuthorAsync(
            snapshot.AuthorUserId, ct);

        return ToDto(report, reporters, settings.ReportThreshold);
    }

    public async Task<PagedResult<ReportDto>> SearchAsync(
        ReportQuery query, CancellationToken ct = default)
    {
        var result = await _reports.SearchAsync(query, ct);
        var settings = await _settings.GetAsync(ct);

        // Distinct reporter counts for the authors on this page, so the queue shows how
        // close each is to the threshold.
        var counts = new Dictionary<Guid, int>();

        foreach (var authorId in result.Items.Select(r => r.Target.AuthorUserId).Distinct())
        {
            counts[authorId] = await _reports.CountDistinctReportersForAuthorAsync(authorId, ct);
        }

        return new PagedResult<ReportDto>
        {
            Items = result.Items
                .Select(r => ToDto(
                    r, counts.GetValueOrDefault(r.Target.AuthorUserId), settings.ReportThreshold))
                .ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <summary>
    /// Removes the reported message, then closes the report.
    ///
    /// The removal is attempted FIRST and the report is only closed if it succeeded, so
    /// the queue never claims an action that did not happen. The old implementation
    /// closed the report and never touched the message at all.
    /// </summary>
    public async Task ActionAsync(
        Guid reportId, ResolveReportRequest request, CancellationToken ct = default)
    {
        var report = await _reports.GetByIdAsync(reportId, ct)
                     ?? throw new NotFoundException("That report does not exist.");

        if (report.Status != ReportStatus.Pending)
            throw new ConflictException("That report has already been resolved.");

        var reason = string.IsNullOrWhiteSpace(request.Note)
            ? $"Removed after a report: {report.Reason}"
            : request.Note;

        var removed = await _platform.RemoveMessageAsync(
            report.Target.Kind, report.Target.MessageId, reason, ct);

        if (!removed)
        {
            throw new DependencyUnavailableException(
                "The message could not be removed because the owning service did not respond. " +
                "The report is still open — try again.");
        }

        await _reports.ResolveAsync(
            reportId, ReportStatus.Actioned, _currentUser.RequireUserId(), request.Note, ct);

        await _audit.LogAsync(
            "ReportActioned", "Message", report.Target.MessageId.ToString(), reason, ct);

        _logger.LogWarning(
            "Admin {AdminId} removed message {MessageId} after report {ReportId}.",
            _currentUser.UserId, report.Target.MessageId, reportId);
    }

    public async Task DismissAsync(
        Guid reportId, ResolveReportRequest request, CancellationToken ct = default)
    {
        var report = await _reports.GetByIdAsync(reportId, ct)
                     ?? throw new NotFoundException("That report does not exist.");

        if (!await _reports.ResolveAsync(
                reportId, ReportStatus.Dismissed, _currentUser.RequireUserId(), request.Note, ct))
        {
            throw new ConflictException("That report has already been resolved.");
        }

        await _audit.LogAsync(
            "ReportDismissed", "Report", reportId.ToString(), request.Note, ct);
    }

    internal static ReportDto ToDto(ReportDocument r, int authorReportCount, int threshold) => new(
        r.Id, r.Target.Kind, r.Target.MessageId, r.Target.ContentSnapshot,
        r.Target.AuthorUserId, r.Target.AuthorAnonymousName, r.Target.RoomName,
        r.ReportedBy.UserId, r.ReportedBy.AnonymousName,
        r.Reason, r.Status, r.CreatedAt, r.ResolvedAt,
        authorReportCount, threshold);
}

/// <summary>
/// The single automated moderation rule.
///
/// The old system ran two: a hardcoded ">= 5 unique reporters" inside the report
/// controller that ignored the configured settings and marked reports IsAutoRemoved, and
/// a background service that read the settings but scanned for Status == Pending. They
/// raced and disagreed. Worse, every action the background service took (disable the
/// account, fetch the email hash) called Auth unauthenticated and got a 401, so
/// auto-moderation never actually removed anyone.
/// </summary>
public sealed class AutoModerationService : IAutoModerationService
{
    private readonly IReportRepository _reports;
    private readonly IModerationSettingsRepository _settings;
    private readonly IBlockedUserRepository _blocked;
    private readonly IPlatformGateway _platform;
    private readonly IAuditLogService _audit;
    private readonly ILogger<AutoModerationService> _logger;

    public AutoModerationService(
        IReportRepository reports,
        IModerationSettingsRepository settings,
        IBlockedUserRepository blocked,
        IPlatformGateway platform,
        IAuditLogService audit,
        ILogger<AutoModerationService> logger)
    {
        _reports = reports;
        _settings = settings;
        _blocked = blocked;
        _platform = platform;
        _audit = audit;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);

        if (!settings.AutoActionEnabled)
        {
            _logger.LogDebug("Automatic moderation is disabled by configuration.");
            return 0;
        }

        var offenders = await _reports.FindAuthorsOverThresholdAsync(settings.ReportThreshold, ct);

        if (offenders.Count == 0) return 0;

        var actioned = 0;

        foreach (var (authorUserId, authorName, reporters) in offenders)
        {
            if (await _blocked.IsBlockedAsync(authorUserId, ct))
            {
                // Already handled; just clear the queue.
                await _reports.ResolvePendingForAuthorAsync(
                    authorUserId, ReportStatus.AutoActioned,
                    "Author was already blocked.", ct);
                continue;
            }

            _logger.LogWarning(
                "Automatic moderation triggered for {AuthorName} ({UserId}): {Reporters}/{Threshold} distinct reporters.",
                authorName, authorUserId, reporters, settings.ReportThreshold);

            var reason =
                $"Automatic moderation: {reporters} distinct users reported content by this account.";

            var succeeded = true;

            if (settings.AutoRemoveMessages)
            {
                var removed = await _platform.RemoveAllMessagesByAuthorAsync(
                    authorUserId, reason, ct);

                _logger.LogInformation(
                    "Removed {Count} message(s) authored by {UserId}.", removed, authorUserId);
            }

            if (settings.AutoDisableAccount)
            {
                if (await _platform.DisableAccountAsync(authorUserId, reason, ct))
                {
                    // Record the block with a real email hash so the account cannot
                    // simply re-register. The hash comes from Auth; the address never does.
                    var emailHash = await _platform.GetEmailHashAsync(authorUserId, ct);

                    if (emailHash is null)
                    {
                        _logger.LogWarning(
                            "Could not obtain the email hash for {UserId}; the block will not " +
                            "prevent re-registration until this is resolved.", authorUserId);
                    }

                    await _blocked.BlockAsync(new BlockedUserDocument
                    {
                        UserId = authorUserId,
                        AnonymousName = authorName,
                        EmailHash = emailHash ?? string.Empty,
                        Reason = reason,
                        BlockedBy = Guid.Empty,
                        Source = "AutoModeration"
                    }, ct);
                }
                else
                {
                    // Surfaced, not swallowed: leaving reports pending means the next run
                    // retries rather than the offender being silently forgotten.
                    succeeded = false;
                    _logger.LogError(
                        "Automatic moderation could not disable account {UserId}. " +
                        "Its reports remain open for the next run.", authorUserId);
                }
            }

            if (succeeded)
            {
                await _reports.ResolvePendingForAuthorAsync(
                    authorUserId, ReportStatus.AutoActioned, reason, ct);

                await _audit.LogSystemAsync(
                    "AutoModerationApplied", "User", authorUserId.ToString(), reason, ct);

                actioned++;
            }
        }

        return actioned;
    }
}

public sealed class AdminUserService : IAdminUserService
{
    private readonly IBlockedUserRepository _blocked;
    private readonly IPlatformGateway _platform;
    private readonly IAuditLogService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IBlockedUserRepository blocked,
        IPlatformGateway platform,
        IAuditLogService audit,
        ICurrentUser currentUser,
        ILogger<AdminUserService> logger)
    {
        _blocked = blocked;
        _platform = platform;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task BlockAsync(
        Guid userId, BlockUserRequest request, CancellationToken ct = default)
    {
        var adminId = _currentUser.RequireUserId();

        if (adminId == userId)
            throw new ValidationException("You cannot block your own account.");

        var name = await _platform.GetAnonymousNameAsync(userId, ct)
                   ?? throw new NotFoundException("That user does not exist.");

        if (!await _platform.DisableAccountAsync(userId, request.Reason, ct))
        {
            throw new DependencyUnavailableException(
                "The account could not be disabled because the auth service did not respond.");
        }

        var emailHash = await _platform.GetEmailHashAsync(userId, ct);

        await _blocked.BlockAsync(new BlockedUserDocument
        {
            UserId = userId,
            AnonymousName = name,
            EmailHash = emailHash ?? string.Empty,
            Reason = request.Reason,
            BlockedBy = adminId,
            Source = "Manual"
        }, ct);

        await _platform.RemoveAllMessagesByAuthorAsync(
            userId, $"Account blocked: {request.Reason}", ct);

        await _audit.LogAsync("UserBlocked", "User", userId.ToString(), request.Reason, ct);

        _logger.LogWarning(
            "Admin {AdminId} blocked user {UserId}. Reason: {Reason}",
            adminId, userId, request.Reason);
    }

    public async Task UnblockAsync(Guid userId, CancellationToken ct = default)
    {
        if (!await _blocked.UnblockAsync(userId, ct))
            throw new NotFoundException("That user is not blocked.");

        await _audit.LogAsync("UserUnblocked", "User", userId.ToString(), null, ct);
    }

    public async Task<IReadOnlyList<BlockedUserDto>> ListBlockedAsync(
        CancellationToken ct = default)
    {
        var blocked = await _blocked.ListAsync(ct);

        return blocked
            .Select(b => new BlockedUserDto(
                b.UserId, b.AnonymousName, b.Reason, b.BlockedAt, b.Source))
            .ToList();
    }
}

public sealed class DashboardService : IDashboardService
{
    private readonly IPlatformGateway _platform;
    private readonly IReportRepository _reports;
    private readonly IBlockedUserRepository _blocked;
    private readonly IAuditLogRepository _audit;

    public DashboardService(
        IPlatformGateway platform,
        IReportRepository reports,
        IBlockedUserRepository blocked,
        IAuditLogRepository audit)
    {
        _platform = platform;
        _reports = reports;
        _blocked = blocked;
        _audit = audit;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        // Fired concurrently. The old dashboard made eight sequential HTTP calls with no
        // timeout, so one slow service stalled the whole page.
        var users = _platform.GetUserCountsAsync(ct);
        var chat = _platform.GetChatCountsAsync(ct);
        var privateChat = _platform.GetPrivateChatCountsAsync(ct);
        var polls = _platform.GetPollCountAsync(ct);
        var notifications = _platform.GetNotificationCountAsync(ct);

        await Task.WhenAll(users, chat, privateChat, polls, notifications);

        var userCounts = users.Result;
        var chatCounts = chat.Result;
        var privateCounts = privateChat.Result;

        return new DashboardStatsDto(
            TotalUsers: Project(userCounts, u => u.Total),
            ActiveUsers: Project(userCounts, u => u.Active),
            DeletedUsers: Project(userCounts, u => u.Deleted),
            BlockedUsers: await _blocked.CountAsync(ct),
            TotalRooms: Project(chatCounts, c => c.Rooms),
            TotalMessages: Project(chatCounts, c => c.Messages),
            TotalConversations: Project(privateCounts, p => p.Conversations),
            TotalDirectMessages: Project(privateCounts, p => p.Messages),
            TotalPolls: polls.Result,
            TotalNotifications: notifications.Result,
            TotalReports: await _reports.CountAsync(null, ct),
            PendingReports: await _reports.CountAsync(ReportStatus.Pending, ct));
    }

    private static Availability<long> Project<T>(Availability<T> source, Func<T, long> select) =>
        source.IsAvailable && source.Value is not null
            ? Availability<long>.Available(select(source.Value))
            : Availability<long>.Unavailable(source.Reason ?? "Unavailable.");

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentActivityAsync(
        int count, CancellationToken ct = default)
    {
        var logs = await _audit.RecentAsync(count, ct);
        return logs.Select(AuditLogService.ToDto).ToList();
    }
}
