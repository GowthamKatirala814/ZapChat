using Admin.Domain.Entities;
using Admin.Domain.Enums;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    // DTOs for external service responses
    private record MessageDetail(Guid Id, string Content, Guid SenderId, string SenderName);
    private record UserDetail(Guid Id, string AnonymousName, string Email);

    private readonly AdminDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        AdminDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ReportsController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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

    [HttpPost]
    [AllowAnonymous]
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
        
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "Report Reviewed",
            EntityType = "Report",
            EntityId = report.Id.ToString(),
            PerformedBy = Guid.Empty, // Would be current user in real auth
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

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "Report Ignored",
            EntityType = "Report",
            EntityId = report.Id.ToString(),
            PerformedBy = Guid.Empty, // Would be current user in real auth
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
