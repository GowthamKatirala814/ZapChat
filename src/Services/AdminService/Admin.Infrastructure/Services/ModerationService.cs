using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Admin.Infrastructure.Services;

public class ModerationService : IModerationService
{
    // DTOs for external service responses
    private record MessageDetail(Guid Id, string Content, Guid SenderId, string SenderName);
    private record UserDetail(Guid Id, string AnonymousName, string Email);

    private readonly IReportRepository _reportRepository;
    private readonly IModerationSettingsRepository _settingsRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModerationService> _logger;

    public ModerationService(
        IReportRepository reportRepository,
        IModerationSettingsRepository settingsRepository,
        IAuditLogService auditLogService,
        IHttpClientFactory httpClientFactory,
        ILogger<ModerationService> logger)
    {
        _reportRepository = reportRepository;
        _settingsRepository = settingsRepository;
        _auditLogService = auditLogService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<ReportDto>> GetReportsAsync(
        ReportStatus? statusFilter = null,
        bool? isAutoRemoved = null,
        int page = 1,
        int pageSize = 50)
    {
        var reports = await _reportRepository.GetAllAsync(statusFilter, isAutoRemoved, page, pageSize);
        return reports.Select(MapToDto);
    }

    public async Task<ReportDto?> GetReportByIdAsync(Guid reportId)
    {
        var report = await _reportRepository.GetByIdAsync(reportId);
        return report is null ? null : MapToDto(report);
    }

    public async Task<ReportDto> SubmitReportAsync(ReportMessageRequest request)
    {
        _logger.LogInformation("SubmitReportAsync started for MessageId={MessageId}, MessageType={MessageType}, ReportedByUserId={ReportedByUserId}", 
            request.MessageId, request.MessageType, request.ReportedByUserId);
        
        // Fetch message details from the appropriate service
        var (messageContent, messageAuthorId, messageAuthorName) = await GetMessageDetailsAsync(request.MessageId, request.MessageType);
        
        // Fetch reporter details from Auth Service
        var (reporterName, reporterEmail) = await GetUserDetailsAsync(request.ReportedByUserId);

        _logger.LogInformation("Fetched data: MessageContent={MessageContent}, AuthorId={AuthorId}, AuthorName={AuthorName}, ReporterName={ReporterName}", 
            messageContent, messageAuthorId, messageAuthorName, reporterName);

        // Enforce rule: A user can report a particular message only once
        var existingReports = await _reportRepository.GetByMessageIdAsync(request.MessageId);
        if (existingReports.Any(r => r.ReportedByUserId == request.ReportedByUserId))
        {
            throw new InvalidOperationException("You have already reported this message.");
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

        _logger.LogInformation("Saving report: Id={Id}, MessageContent={MessageContent}, MessageAuthorName={MessageAuthorName}, ReportedByUserName={ReportedByUserName}", 
            report.Id, report.MessageContent, report.MessageAuthorName, report.ReportedByUserName);

        await _reportRepository.AddAsync(report);

        return MapToDto(report);
    }

    public async Task MarkReportAsReviewedAsync(Guid reportId, Guid adminId)
    {
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report is null)
            throw new KeyNotFoundException($"Report {reportId} not found.");

        report.Status = ReportStatus.Reviewed;
        await _reportRepository.UpdateAsync(report);
        await _auditLogService.LogAsync("ReportReviewed", "Report", reportId.ToString(), adminId);
    }

    public async Task IgnoreReportAsync(Guid reportId, Guid adminId)
    {
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report is null)
            throw new KeyNotFoundException($"Report {reportId} not found.");

        report.Status = ReportStatus.Ignored;
        await _reportRepository.UpdateAsync(report);
        await _auditLogService.LogAsync("ReportIgnored", "Report", reportId.ToString(), adminId);
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid adminId)
    {
        var reports = await _reportRepository.GetByMessageIdAsync(messageId);
        var pendingReports = reports.Where(r => r.Status == ReportStatus.Pending).ToList();

        if (reports.Any() && !pendingReports.Any())
        {
            throw new InvalidOperationException("This message has already been removed or its reports have been resolved.");
        }

        // Mark all reports for this message as reviewed
        foreach (var report in pendingReports)
        {
            report.Status = ReportStatus.Reviewed;
            await _reportRepository.UpdateAsync(report);
        }

        // Integration contract: Admin Service records deletion but does NOT directly delete
        // from ChatService or PrivateChatService. Those services should poll or be notified.
        await _auditLogService.LogAsync("MessageDeleted", "Message", messageId.ToString(), adminId);
    }

    public async Task DeleteUserAsync(Guid userId, Guid adminId)
    {
        // Mark all pending reports whose message was authored by this user as Reviewed.
        // This removes them from the Pending queue immediately so admins don't see ghost entries.
        var affectedReports = await _reportRepository.GetPendingByAuthorIdAsync(userId);
        foreach (var report in affectedReports)
        {
            report.Status = ReportStatus.Reviewed;
            await _reportRepository.UpdateAsync(report);
        }

        // Integration contract: Admin Service records user deletion
        await _auditLogService.LogAsync("UserDeleted", "User", userId.ToString(), adminId);
    }

    public async Task<ModerationSettingsDto> GetSettingsAsync()
    {
        var settings = await _settingsRepository.GetOrCreateDefaultAsync();
        return MapSettingsToDto(settings);
    }

    public async Task<ModerationSettingsDto> UpdateSettingsAsync(
        UpdateModerationSettingsRequest request,
        Guid adminId)
    {
        var settings = await _settingsRepository.GetOrCreateDefaultAsync();
        settings.ReportThreshold = request.ReportThreshold;
        settings.AutoDeleteEnabled = request.AutoDeleteEnabled;
        settings.UpdatedAt = DateTime.UtcNow;

        await _settingsRepository.UpdateAsync(settings);
        await _auditLogService.LogAsync(
            "ThresholdChanged",
            "ModerationSettings",
            settings.Id.ToString(),
            adminId);

        return MapSettingsToDto(settings);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Fetches message details from ChatService or PrivateChatService based on message type
    /// </summary>
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

    /// <summary>
    /// Fetches user details from Auth Service
    /// </summary>
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



    private static ReportDto MapToDto(Report r) => new()
    {
        Id = r.Id,
        MessageId = r.MessageId,
        MessageContent = r.MessageContent ?? "Message content unavailable",
        MessageAuthorId = r.MessageAuthorId,
        MessageAuthorName = r.MessageAuthorName ?? "Unknown",
        MessageType = r.MessageType,
        MessageTypeName = r.MessageType.ToString(),
        ReportedByUserId = r.ReportedByUserId,
        ReportedByUserName = r.ReportedByUserName ?? "Unknown",
        Reason = r.Reason,
        ReportedAt = r.CreatedAt,
        Status = r.Status,
        StatusName = r.Status.ToString(),
        IsAutoRemoved = r.IsAutoRemoved
    };


    private static ModerationSettingsDto MapSettingsToDto(ModerationSettings s) => new()
    {
        Id = s.Id,
        ReportThreshold = s.ReportThreshold,
        AutoDeleteEnabled = s.AutoDeleteEnabled,
        UpdatedAt = s.UpdatedAt
    };
}
