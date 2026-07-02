using Admin.Domain.Entities;
using Admin.Domain.Enums;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Security.Claims;
using Admin.Application.Interfaces;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    // DTOs for external service responses
    private record MessageDetail(Guid Id, string Content, Guid SenderId, string SenderName);
    private record UserDetail(Guid Id, string AnonymousName, string Email);

    private readonly AdminDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReportsController> _logger;
    private readonly IUserManagementService _userManagementService;

    public ReportsController(
        AdminDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ReportsController> logger,
        IUserManagementService userManagementService)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var reports = await _context.Reports
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return Ok(reports);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var reports = await _context.Reports
            .Where(x => x.Status == ReportStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return Ok(reports);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();
        return Ok(report);
    }

    public record CreateReportRequest(Guid MessageId, MessageType MessageType, Guid ReportedByUserId, string Reason);

    // Allow authenticated users to submit reports (not just admins)
    [AllowAnonymous] // Reports are submitted by regular users via Chat/PrivateChat service forwarding
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequest request)
    {
        _logger.LogInformation(
            "REPORTS CONTROLLER HIT: MessageId={MessageId}, MessageType={MessageType}, ReportedByUserId={ReportedByUserId}",
            request.MessageId, request.MessageType, request.ReportedByUserId);

        // Fetch message details from the appropriate service
        var (messageContent, messageAuthorId, messageAuthorName) = await GetMessageDetailsAsync(request.MessageId, request.MessageType);

        // Fallback: If ChatService didn't provide an ID but did provide a name, resolve the ID
        if (messageAuthorId == Guid.Empty && !string.IsNullOrWhiteSpace(messageAuthorName) && messageAuthorName != "Unknown")
        {
            messageAuthorId = await ResolveUserIdByAnonymousNameAsync(messageAuthorName);
        }

        // Reject report if the message does not exist or has been deleted.
        // A Guid.Empty authorId means the upstream service returned no matching message.
        // Accepting such reports would create orphan records with no author, which corrupt
        // the unique-reporter-per-author threshold calculation in auto-moderation.
        if (messageAuthorId == Guid.Empty)
        {
            _logger.LogWarning(
                "Report rejected — message {MessageId} not found or already deleted.",
                request.MessageId);
            return BadRequest(new { message = "Cannot report a message that does not exist or has already been deleted." });
        }

        // Fetch reporter details from Auth Service
        var (reporterName, reporterEmail) = await GetUserDetailsAsync(request.ReportedByUserId);

        _logger.LogInformation(
            "Fetched data: MessageContent={MessageContent}, AuthorId={AuthorId}, AuthorName={AuthorName}, ReporterName={ReporterName}",
            messageContent, messageAuthorId, messageAuthorName, reporterName);

        // Enforce rule: one user can report a particular message only once,
        // regardless of reason. The unique DB index (IX_Reports_MessageId_ReportedByUserId)
        // also enforces this at the database level as a second line of defence.
        if (await _context.Reports.AnyAsync(r => r.MessageId == request.MessageId && r.ReportedByUserId == request.ReportedByUserId))
        {
            _logger.LogWarning(
                "Duplicate report rejected — UserId={UserId} already reported MessageId={MessageId}.",
                request.ReportedByUserId, request.MessageId);
            return Conflict(new { message = "You have already reported this message." });
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            MessageId = request.MessageId,
            MessageType = request.MessageType,
            MessageAuthorId = messageAuthorId,
            MessageContent = messageContent,
            MessageAuthorName = messageAuthorName,
            ReportedByUserId = request.ReportedByUserId,
            ReportedByUserName = reporterName,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow,
            Status = ReportStatus.Pending,
            IsAutoRemoved = false
        };

        _logger.LogInformation(
            "Saving report: Id={Id}, MessageContent={MessageContent}, MessageAuthorName={MessageAuthorName}, ReportedByUserName={ReportedByUserName}",
            report.Id, report.MessageContent, report.MessageAuthorName, report.ReportedByUserName);

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        // Auto-moderation: Remove user after 5 unique reports
        var uniqueReportersCount = await _context.Reports
            .Where(r => r.MessageAuthorId == messageAuthorId && !r.IsAutoRemoved)
            .Select(r => r.ReportedByUserId)
            .Distinct()
            .CountAsync();

        if (uniqueReportersCount >= 5)
        {
            _logger.LogWarning("Auto-moderation triggered for user {UserId}. {Count} unique reports reached.", messageAuthorId, uniqueReportersCount);
            
            try
            {
                // Delete user (adminId = Guid.Empty to signify system action)
                await _userManagementService.DeleteUserAsync(messageAuthorId, "Auto-moderation: Received 5 unique reports", Guid.Empty);
                
                // Mark reports as AutoRemoved
                var userReports = await _context.Reports
                    .Where(r => r.MessageAuthorId == messageAuthorId && !r.IsAutoRemoved)
                    .ToListAsync();
                
                foreach (var userReport in userReports)
                {
                    userReport.IsAutoRemoved = true;
                    userReport.Status = ReportStatus.AutoRemoved;
                }
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Auto-moderation completed successfully for user {UserId}.", messageAuthorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-moderation failed to delete user {UserId}.", messageAuthorId);
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
    }

    private async Task<(string content, Guid authorId, string authorName)> GetMessageDetailsAsync(Guid messageId, MessageType messageType)
    {
        try
        {
            if (messageType == MessageType.Room)
            {
                var client = _httpClientFactory.CreateClient("ChatService");
                var url = $"api/messages/{messageId}";
                _logger.LogInformation("Fetching room message from ChatService: {Url}", url);
                var response = await client.GetAsync(url);
                _logger.LogInformation("ChatService response for message {MessageId}: {StatusCode}", messageId, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("ChatService response body: {Json}", json);
                    var message = await response.Content.ReadFromJsonAsync<MessageDetail>();
                    _logger.LogInformation("Parsed message: Content={Content}, SenderId={SenderId}, SenderName={SenderName}",
                        message?.Content, message?.SenderId, message?.SenderName);
                    return (message?.Content ?? "Message deleted", message?.SenderId ?? Guid.Empty, message?.SenderName ?? "Unknown");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("ChatService returned error for message {MessageId}: {StatusCode} - {Error}", messageId, response.StatusCode, error);
                }
            }
            else if (messageType == MessageType.Private)
            {
                var client = _httpClientFactory.CreateClient("PrivateChatService");
                var url = $"api/messages/{messageId}";
                _logger.LogInformation("Fetching private message from PrivateChatService: {Url}", url);
                var response = await client.GetAsync(url);
                _logger.LogInformation("PrivateChatService response for message {MessageId}: {StatusCode}", messageId, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("PrivateChatService response body: {Json}", json);
                    var message = await response.Content.ReadFromJsonAsync<MessageDetail>();
                    _logger.LogInformation("Parsed message: Content={Content}, SenderId={SenderId}, SenderName={SenderName}",
                        message?.Content, message?.SenderId, message?.SenderName);
                    return (message?.Content ?? "Message deleted", message?.SenderId ?? Guid.Empty, message?.SenderName ?? "Unknown");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("PrivateChatService returned error for message {MessageId}: {StatusCode} - {Error}", messageId, response.StatusCode, error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch message details for {MessageId}", messageId);
        }

        return ("Message content unavailable", Guid.Empty, "Unknown");
    }

    private async Task<Guid> ResolveUserIdByAnonymousNameAsync(string anonymousName)
    {
        if (string.IsNullOrWhiteSpace(anonymousName) || anonymousName == "Unknown")
            return Guid.Empty;

        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var url = $"api/auth/users/by-name/{Uri.EscapeDataString(anonymousName)}";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserDetail>();
                return user?.Id ?? Guid.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve UserId for AnonymousName {AnonymousName}", anonymousName);
        }
        return Guid.Empty;
    }

    private async Task<(string name, string email)> GetUserDetailsAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var url = $"api/auth/users/{userId}";
            _logger.LogInformation("Fetching user from AuthService: {Url}", url);
            var response = await client.GetAsync(url);
            _logger.LogInformation("AuthService response for user {UserId}: {StatusCode}", userId, response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("AuthService response body: {Json}", json);
                var user = await response.Content.ReadFromJsonAsync<UserDetail>();
                _logger.LogInformation("Parsed user: AnonymousName={Name}, Email={Email}", user?.AnonymousName, user?.Email);
                return (user?.AnonymousName ?? "Unknown", user?.Email ?? "");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("AuthService returned error for user {UserId}: {StatusCode} - {Error}", userId, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user details for {UserId}", userId);
        }

        return ("Unknown", "");
    }

    [HttpPut("{id}/review")]
    public async Task<IActionResult> Review(Guid id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ReportStatus.Reviewed;

        var performedBy = GetCurrentUserId();
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "Report Reviewed",
            EntityType = "Report",
            EntityId = report.Id.ToString(),
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/ignore")]
    public async Task<IActionResult> Ignore(Guid id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ReportStatus.Ignored;

        var performedBy = GetCurrentUserId();
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "Report Ignored",
            EntityType = "Report",
            EntityId = report.Id.ToString(),
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
