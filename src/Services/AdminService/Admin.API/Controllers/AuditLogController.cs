using Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns paginated audit log entries, ordered by most recent first.
    /// Every admin action (block, delete, create room, approve report, etc.) is logged here.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var logs = await _auditLogService.GetLogsAsync(page, pageSize);
        var total = await _auditLogService.GetTotalCountAsync();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Data = logs
        });
    }

    /// <summary>
    /// Returns audit logs filtered by target type and target ID.
    /// Example: targetType=User, targetId={userId}
    /// </summary>
    [HttpGet("by-target")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogsByTarget(
        [FromQuery] string targetType,
        [FromQuery] string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(targetId))
            return BadRequest(new { message = "targetType and targetId are required." });

        var logs = await _auditLogService.GetLogsByTargetAsync(targetType, targetId);
        return Ok(logs);
    }
}
