using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Admin.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class ModerationController : ControllerBase
{
    private readonly IModerationService _moderationService;

    public ModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    /// <summary>
    /// Returns reported messages. 
    /// Filter by status: 0=Pending, 1=Reviewed, 2=Ignored.
    /// Filter by isAutoRemoved: true=Auto Removed, false=Manually handled.
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReports(
        [FromQuery] ReportStatus? status = null,
        [FromQuery] bool? isAutoRemoved = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var reports = await _moderationService.GetReportsAsync(status, isAutoRemoved, page, pageSize);
        return Ok(reports);
    }

    /// <summary>
    /// Returns a single report by ID.
    /// </summary>
    [HttpGet("reports/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var report = await _moderationService.GetReportByIdAsync(id);
        if (report is null) return NotFound();
        return Ok(report);
    }

    /// <summary>
    /// Submits a new message report. Automatically checks threshold and
    /// applies auto-remove if the report count reaches the configured limit.
    /// </summary>
    [HttpPost("reports")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitReport([FromBody] ReportMessageRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var report = await _moderationService.SubmitReportAsync(request);
        return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
    }

    /// <summary>
    /// Marks a report as reviewed (status = 1).
    /// </summary>
    [HttpPost("reports/{reportId:guid}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsReviewed(Guid reportId)
    {
        try
        {
            var adminId = GetAdminId();
            await _moderationService.MarkReportAsReviewedAsync(reportId, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Ignores a report (status = 2).
    /// </summary>
    [HttpPost("reports/{reportId:guid}/ignore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> IgnoreReport(Guid reportId)
    {
        try
        {
            var adminId = GetAdminId();
            await _moderationService.IgnoreReportAsync(reportId, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deletes a reported message permanently.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        try
        {
            var adminId = GetAdminId();
            await _moderationService.DeleteMessageAsync(messageId, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Returns current moderation settings (report threshold, auto-delete flag).
    /// If settings don't exist yet, returns defaults: threshold=5, autoDelete=true.
    /// </summary>
    [HttpGet("moderation/settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _moderationService.GetSettingsAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Updates moderation settings. Generates a "ThresholdChanged" audit log entry.
    /// </summary>
    [HttpPut("moderation/settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateModerationSettingsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = GetAdminId();
        var updated = await _moderationService.UpdateSettingsAsync(request, adminId);
        return Ok(updated);
    }

    private Guid GetAdminId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
