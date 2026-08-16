using Admin.Application;
using Admin.Domain.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Results;

namespace Admin.API;

/// <summary>
/// Reporting. Submission is open to any authenticated user; everything else is admin-only.
/// </summary>
[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    /// <summary>
    /// Files a report as the authenticated caller. The reporter's identity comes from the
    /// token, so report counts are trustworthy and the threshold rule means something.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReportDto>> Submit(
        [FromBody] SubmitReportRequest request, CancellationToken ct)
        => Ok(await _reports.SubmitAsync(request, ct));

    /// <summary>
    /// The moderation queue. Admin-only: these records contain the reported content, the
    /// author's anonymous name and the reporter's, so exposing them to ordinary users
    /// disclosed who had reported whom.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<ActionResult<PagedResult<ReportDto>>> Search(
        [FromQuery] ReportQuery query, CancellationToken ct)
        => Ok(await _reports.SearchAsync(query, ct));

    /// <summary>Removes the reported message and closes the report.</summary>
    [HttpPost("{reportId:guid}/action")]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<IActionResult> Action(
        Guid reportId, [FromBody] ResolveReportRequest request, CancellationToken ct)
    {
        await _reports.ActionAsync(reportId, request, ct);
        return NoContent();
    }

    [HttpPost("{reportId:guid}/dismiss")]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<IActionResult> Dismiss(
        Guid reportId, [FromBody] ResolveReportRequest request, CancellationToken ct)
    {
        await _reports.DismissAsync(reportId, request, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Aggregate figures. Each value reports whether it could be determined, so the UI
    /// can show "unavailable" rather than a zero that looks like real data.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats(CancellationToken ct)
        => Ok(await _dashboard.GetStatsAsync(ct));

    [HttpGet("recent-activity")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> RecentActivity(
        [FromQuery] int count = 20, CancellationToken ct = default)
        => Ok(await _dashboard.GetRecentActivityAsync(count, ct));
}

[ApiController]
[Route("api/admin/moderation")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class ModerationController : ControllerBase
{
    private readonly IModerationSettingsRepository _settings;
    private readonly IAutoModerationService _autoModeration;
    private readonly IAuditLogService _audit;
    private readonly ICurrentUser _currentUser;

    public ModerationController(
        IModerationSettingsRepository settings,
        IAutoModerationService autoModeration,
        IAuditLogService audit,
        ICurrentUser currentUser)
    {
        _settings = settings;
        _autoModeration = autoModeration;
        _audit = audit;
        _currentUser = currentUser;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ModerationSettingsDto>> GetSettings(CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        return Ok(ToDto(settings));
    }

    /// <summary>
    /// Updates the threshold and the automatic actions. These values are now actually
    /// honoured — the old report path hardcoded a threshold of 5 and ignored them.
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<ModerationSettingsDto>> UpdateSettings(
        [FromBody] UpdateModerationSettingsRequest request, CancellationToken ct)
    {
        var updated = await _settings.UpdateAsync(request, _currentUser.RequireUserId(), ct);

        await _audit.LogAsync(
            "ModerationSettingsChanged", "Settings", updated.Id,
            $"threshold={request.ReportThreshold}, autoAction={request.AutoActionEnabled}", ct);

        return Ok(ToDto(updated));
    }

    /// <summary>Runs the threshold rule immediately, for testing and manual sweeps.</summary>
    [HttpPost("run-auto-moderation")]
    public async Task<ActionResult<object>> RunAutoModeration(CancellationToken ct)
        => Ok(new { authorsActioned = await _autoModeration.RunAsync(ct) });

    private static ModerationSettingsDto ToDto(ModerationSettingsDocument s) => new(
        s.ReportThreshold, s.AutoActionEnabled, s.AutoRemoveMessages,
        s.AutoDisableAccount, s.UpdatedAt);
}

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _users;

    public AdminUsersController(IAdminUserService users) => _users = users;

    [HttpGet("blocked")]
    public async Task<ActionResult<IReadOnlyList<BlockedUserDto>>> Blocked(CancellationToken ct)
        => Ok(await _users.ListBlockedAsync(ct));

    /// <summary>Disables the account, records the block, and removes its content.</summary>
    [HttpPost("{userId:guid}/block")]
    public async Task<IActionResult> Block(
        Guid userId, [FromBody] BlockUserRequest request, CancellationToken ct)
    {
        await _users.BlockAsync(userId, request, ct);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/block")]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken ct)
    {
        await _users.UnblockAsync(userId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _audit;

    public AuditLogsController(IAuditLogService audit) => _audit = audit;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        CancellationToken ct = default)
        => Ok(await _audit.SearchAsync(page, pageSize, entityType, entityId, ct));
}

/// <summary>
/// Analytics. Every series is fetched from the owning service and returned with an
/// explicit availability flag, so a chart can render "unavailable" instead of a flat zero
/// line — which is what the old catch{} -> Enumerable.Empty produced.
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IPlatformGateway _platform;
    private readonly IReportRepository _reports;

    public AnalyticsController(IPlatformGateway platform, IReportRepository reports)
    {
        _platform = platform;
        _reports = reports;
    }

    [HttpGet("messages-per-day")]
    public async Task<ActionResult<Availability<IReadOnlyList<DailyCountDto>>>> MessagesPerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await _platform.GetSeriesAsync(
            ServiceClients.Chat, "api/chat-admin/analytics/messages-per-day", days, ct));

    [HttpGet("messages-per-hour")]
    public async Task<ActionResult<Availability<IReadOnlyList<NamedCountDto>>>> MessagesPerHour(
        CancellationToken ct)
        => Ok(await _platform.GetNamedCountsAsync(
            ServiceClients.Chat, "api/chat-admin/analytics/messages-per-hour", 24, ct));

    [HttpGet("direct-messages-per-day")]
    public async Task<ActionResult<Availability<IReadOnlyList<DailyCountDto>>>> DirectPerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await _platform.GetSeriesAsync(
            ServiceClients.PrivateChat, "api/privatechat-admin/analytics/messages-per-day", days, ct));

    [HttpGet("polls-per-day")]
    public async Task<ActionResult<Availability<IReadOnlyList<DailyCountDto>>>> PollsPerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await _platform.GetSeriesAsync(
            ServiceClients.Poll, "api/poll-admin/analytics/polls-per-day", days, ct));

    [HttpGet("notifications-per-day")]
    public async Task<ActionResult<Availability<IReadOnlyList<DailyCountDto>>>> NotificationsPerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await _platform.GetSeriesAsync(
            ServiceClients.Notification, "api/notification-admin/analytics/per-day", days, ct));

    [HttpGet("top-rooms")]
    public async Task<ActionResult<Availability<IReadOnlyList<RoomActivity>>>> TopRooms(
        [FromQuery] int top = 10, CancellationToken ct = default)
        => Ok(await _platform.GetRoomActivityAsync(top, ct));

    [HttpGet("top-authors")]
    public async Task<ActionResult<Availability<IReadOnlyList<NamedCountDto>>>> TopAuthors(
        [FromQuery] int top = 10, CancellationToken ct = default)
        => Ok(await _platform.GetNamedCountsAsync(
            ServiceClients.Chat, "api/chat-admin/analytics/top-authors", top, ct));

    [HttpGet("top-polls")]
    public async Task<ActionResult<Availability<IReadOnlyList<NamedCountDto>>>> TopPolls(
        [FromQuery] int top = 10, CancellationToken ct = default)
        => Ok(await _platform.GetNamedCountsAsync(
            ServiceClients.Poll, "api/poll-admin/analytics/top-polls", top, ct));

    /// <summary>Report volume over time. Computed locally — reports live in this database.</summary>
    [HttpGet("reports-per-day")]
    public async Task<ActionResult<IReadOnlyList<DailyCountDto>>> ReportsPerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var counts = (await _reports.CountByDayAsync(days, ct))
            .ToDictionary(x => x.Day.Date, x => x.Count);

        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        return Ok(Enumerable.Range(0, Math.Clamp(days, 1, 365))
            .Select(offset =>
            {
                var day = since.AddDays(offset);
                return new DailyCountDto(
                    day.ToString("yyyy-MM-dd"), counts.GetValueOrDefault(day));
            })
            .ToList());
    }

    [HttpGet("report-reasons")]
    public async Task<ActionResult<IReadOnlyList<NamedCountDto>>> ReportReasons(
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var counts = await _reports.CountByReasonAsync(top, ct);
        return Ok(counts.Select(c => new NamedCountDto(c.Reason, c.Count)).ToList());
    }

    /// <summary>
    /// Room health: message volume from Chat joined with report counts held here.
    /// Both real numbers, unlike the old version whose per-room message count and active
    /// user count were hardcoded zeros.
    /// </summary>
    [HttpGet("room-health")]
    public async Task<ActionResult<Availability<IReadOnlyList<RoomHealthDto>>>> RoomHealth(
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var activity = await _platform.GetRoomActivityAsync(50, ct);

        if (!activity.IsAvailable || activity.Value is null)
        {
            return Ok(Availability<IReadOnlyList<RoomHealthDto>>.Unavailable(
                activity.Reason ?? "Chat activity is unavailable."));
        }

        var reportCounts = (await _reports.CountByRoomAsync(ct))
            .ToDictionary(x => x.RoomId, x => x.Count);

        var health = activity.Value
            .Select(room =>
            {
                var reports = reportCounts.GetValueOrDefault(room.RoomId);

                var rate = room.MessageCount > 0
                    ? Math.Round(reports / (double)room.MessageCount * 100, 2)
                    : 0;

                var label = rate switch
                {
                    < 1.0 => "Healthy",
                    < 5.0 => "Monitor",
                    _ => "Critical"
                };

                return new RoomHealthDto(
                    room.RoomId, room.RoomName, room.MessageCount, reports, rate, label);
            })
            .OrderByDescending(r => r.ReportRate)
            .Take(Math.Clamp(top, 1, 50))
            .ToList();

        return Ok(Availability<IReadOnlyList<RoomHealthDto>>.Available(health));
    }
}
