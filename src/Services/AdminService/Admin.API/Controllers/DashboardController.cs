using Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns aggregate statistics: total users, blocked, active,
    /// total rooms, reports, and placeholder counts for services not yet integrated.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Returns the N most recent admin activity entries from the audit log.
    /// Activity types include: UserBlocked, UserDeleted, RoomCreated, RoomDeleted,
    /// ReportApproved, ReportIgnored, ThresholdChanged, AutoMessageRemoved.
    /// </summary>
    [HttpGet("recent-activity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 20)
    {
        if (count < 1) count = 1;
        if (count > 100) count = 100;

        var activities = await _dashboardService.GetRecentActivityAsync(count);
        return Ok(activities);
    }
}
