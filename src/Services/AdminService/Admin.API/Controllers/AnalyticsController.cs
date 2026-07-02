using Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IMemoryCache _cache;

    public AnalyticsController(IAnalyticsService analyticsService, IMemoryCache cache)
    {
        _analyticsService = analyticsService;
        _cache = cache;
    }

    private async Task<IActionResult> GetCachedAsync<T>(string key, Func<Task<T>> func)
    {
        if (!_cache.TryGetValue(key, out T? result))
        {
            result = await func();
            _cache.Set(key, result, TimeSpan.FromSeconds(60));
        }
        return Ok(result);
    }

    // ─── Existing endpoints (unchanged) ──────────────────────────────────────

    [HttpGet("user-growth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetUserGrowth([FromQuery] int days = 30) =>
        GetCachedAsync($"UserGrowth_{days}", () => _analyticsService.GetUserGrowthAsync(days));

    [HttpGet("active-rooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetActiveRooms([FromQuery] int top = 10) =>
        GetCachedAsync($"ActiveRooms_{top}", () => _analyticsService.GetActiveRoomsAsync(top));

    [HttpGet("active-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetActiveUsers([FromQuery] int top = 10) =>
        GetCachedAsync($"ActiveUsers_{top}", () => _analyticsService.GetActiveUsersAsync(top));

    [HttpGet("daily-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetDailyMessages([FromQuery] int days = 30) =>
        GetCachedAsync($"DailyMessages_{days}", () => _analyticsService.GetDailyMessagesAsync(days));

    [HttpGet("private-chat-volume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetPrivateChatVolume([FromQuery] int days = 30) =>
        GetCachedAsync($"PrivateChatVolume_{days}", () => _analyticsService.GetPrivateChatVolumeAsync(days));

    [HttpGet("daily-polls")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetDailyPolls([FromQuery] int days = 30) =>
        GetCachedAsync($"DailyPolls_{days}", () => _analyticsService.GetDailyPollsAsync(days));

    [HttpGet("most-voted-polls")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetMostVotedPolls([FromQuery] int top = 10) =>
        GetCachedAsync($"MostVotedPolls_{top}", () => _analyticsService.GetMostVotedPollsAsync(top));

    [HttpGet("daily-notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetDailyNotifications([FromQuery] int days = 30) =>
        GetCachedAsync($"DailyNotifications_{days}", () => _analyticsService.GetDailyNotificationsAsync(days));

    [HttpGet("report-trends")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetReportTrends([FromQuery] int days = 30) =>
        GetCachedAsync($"ReportTrends_{days}", () => _analyticsService.GetReportTrendsAsync(days));

    [HttpGet("report-reasons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetReportReasons() =>
        GetCachedAsync("ReportReasons", () => _analyticsService.GetReportReasonsAsync());

    [HttpGet("most-active-rooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetMostActiveRooms([FromQuery] int top = 10) =>
        GetCachedAsync($"MostActiveRooms_{top}", () => _analyticsService.GetMostActiveRoomsAsync(top));

    [HttpGet("most-active-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetMostActiveUsers([FromQuery] int top = 10) =>
        GetCachedAsync($"MostActiveUsers_{top}", () => _analyticsService.GetMostActiveUsersAsync(top));

    [HttpGet("daily-reports")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetDailyReports([FromQuery] int days = 30) =>
        GetCachedAsync($"DailyReports_{days}", () => _analyticsService.GetDailyReportsAsync(days));

    // ─── New analytics endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Chart 2 — Room Health Index.
    /// Returns rooms sorted by report rate descending with Healthy/Monitor/Critical classification.
    /// </summary>
    [HttpGet("room-health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetRoomHealth([FromQuery] int top = 10) =>
        GetCachedAsync($"RoomHealth_{top}", () => _analyticsService.GetRoomHealthAsync(top));

    /// <summary>
    /// Chart 3 — Poll Participation by Topic.
    /// Returns top polls with vote count and participation rate percentage.
    /// </summary>
    [HttpGet("poll-participation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetPollParticipation([FromQuery] int top = 6) =>
        GetCachedAsync($"PollParticipation_{top}", () => _analyticsService.GetPollParticipationAsync(top));

    /// <summary>
    /// Chart 4 — Message Volume by Hour of Day.
    /// Returns 24 data points (hours 0–23) with total message counts.
    /// </summary>
    [HttpGet("hourly-activity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetHourlyActivity() =>
        GetCachedAsync("HourlyActivity", () => _analyticsService.GetHourlyActivityAsync());

    /// <summary>
    /// Chart 5 — Sentiment Distribution by Room.
    /// Returns keyword-based positive/neutral/negative percentages per room.
    /// </summary>
    [HttpGet("room-sentiment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetRoomSentiment([FromQuery] int top = 8) =>
        GetCachedAsync($"RoomSentiment_{top}", () => _analyticsService.GetRoomSentimentAsync(top));
}
