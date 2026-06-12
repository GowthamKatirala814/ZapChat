using Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // ─── Existing endpoints (unchanged) ──────────────────────────────────────

    [HttpGet("user-growth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserGrowth([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetUserGrowthAsync(days);
        return Ok(data);
    }

    [HttpGet("active-rooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveRooms([FromQuery] int top = 10)
    {
        var data = await _analyticsService.GetActiveRoomsAsync(top);
        return Ok(data);
    }

    [HttpGet("active-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveUsers([FromQuery] int top = 10)
    {
        var data = await _analyticsService.GetActiveUsersAsync(top);
        return Ok(data);
    }

    [HttpGet("daily-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDailyMessages([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetDailyMessagesAsync(days);
        return Ok(data);
    }

    [HttpGet("private-chat-volume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPrivateChatVolume([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetPrivateChatVolumeAsync(days);
        return Ok(data);
    }

    [HttpGet("daily-polls")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDailyPolls([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetDailyPollsAsync(days);
        return Ok(data);
    }

    [HttpGet("most-voted-polls")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMostVotedPolls([FromQuery] int top = 10)
    {
        var data = await _analyticsService.GetMostVotedPollsAsync(top);
        return Ok(data);
    }

    [HttpGet("daily-notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDailyNotifications([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetDailyNotificationsAsync(days);
        return Ok(data);
    }

    [HttpGet("report-trends")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReportTrends([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetReportTrendsAsync(days);
        return Ok(data);
    }

    [HttpGet("report-reasons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReportReasons()
    {
        var data = await _analyticsService.GetReportReasonsAsync();
        return Ok(data);
    }

    [HttpGet("most-active-rooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMostActiveRooms([FromQuery] int top = 10)
    {
        var data = await _analyticsService.GetMostActiveRoomsAsync(top);
        return Ok(data);
    }

    [HttpGet("most-active-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMostActiveUsers([FromQuery] int top = 10)
    {
        var data = await _analyticsService.GetMostActiveUsersAsync(top);
        return Ok(data);
    }

    [HttpGet("daily-reports")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDailyReports([FromQuery] int days = 30)
    {
        var data = await _analyticsService.GetDailyReportsAsync(days);
        return Ok(data);
    }

    // ─── New analytics endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Chart 2 — Room Health Index.
    /// Returns rooms sorted by report rate descending with Healthy/Monitor/Critical classification.
    /// </summary>
    [HttpGet("room-health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoomHealth([FromQuery] int top = 10)
    {
        var data = await _analyticsService.GetRoomHealthAsync(top);
        return Ok(data);
    }

    /// <summary>
    /// Chart 3 — Poll Participation by Topic.
    /// Returns top polls with vote count and participation rate percentage.
    /// </summary>
    [HttpGet("poll-participation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPollParticipation([FromQuery] int top = 6)
    {
        var data = await _analyticsService.GetPollParticipationAsync(top);
        return Ok(data);
    }

    /// <summary>
    /// Chart 4 — Message Volume by Hour of Day.
    /// Returns 24 data points (hours 0–23) with total message counts.
    /// </summary>
    [HttpGet("hourly-activity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHourlyActivity()
    {
        var data = await _analyticsService.GetHourlyActivityAsync();
        return Ok(data);
    }

    /// <summary>
    /// Chart 5 — Sentiment Distribution by Room.
    /// Returns keyword-based positive/neutral/negative percentages per room.
    /// </summary>
    [HttpGet("room-sentiment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoomSentiment([FromQuery] int top = 8)
    {
        var data = await _analyticsService.GetRoomSentimentAsync(top);
        return Ok(data);
    }
}
