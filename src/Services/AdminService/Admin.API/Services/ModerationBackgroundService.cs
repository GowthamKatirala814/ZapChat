using Admin.Domain.Entities;
using Admin.Domain.Enums;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Admin.API.Services;

public class ModerationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModerationBackgroundService> _logger;

    public ModerationBackgroundService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ModerationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessModerationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ModerationBackgroundService.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessModerationAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        var settings = await dbContext.ModerationSettings.FirstOrDefaultAsync(stoppingToken);
        if (settings == null)
        {
            settings = new ModerationSettings { AutoDeleteEnabled = true, ReportThreshold = 5 };
            dbContext.ModerationSettings.Add(settings);
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        if (!settings.AutoDeleteEnabled) return;

        var pendingReports = await dbContext.Reports
            .Where(r => r.Status == ReportStatus.Pending)
            .ToListAsync(stoppingToken);

        var groupedReports = pendingReports
            .GroupBy(r => new { r.MessageId, r.MessageType })
            .Where(g => g.Count() >= settings.ReportThreshold)
            .ToList();

        foreach (var group in groupedReports)
        {
            var messageId = group.Key.MessageId;
            var messageType = group.Key.MessageType;

            bool success = await CallAutoRemoveApi(messageId, messageType, stoppingToken);

            if (success)
            {
                foreach (var report in group)
                {
                    report.Status = ReportStatus.AutoRemoved;
                }
                
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Message Auto Removed",
                    EntityType = messageType.ToString(),
                    EntityId = messageId.ToString(),
                    PerformedBy = Guid.Empty,
                    Timestamp = DateTime.UtcNow
                };
                dbContext.AuditLogs.Add(auditLog);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task<bool> CallAutoRemoveApi(Guid messageId, MessageType messageType, CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient();
        
        string? targetUrl;
        if (messageType == MessageType.Room)
        {
            targetUrl = _configuration["ServiceUrls:ChatService"];
        }
        else
        {
            targetUrl = _configuration["ServiceUrls:PrivateChatService"];
        }

        if (string.IsNullOrEmpty(targetUrl))
        {
            _logger.LogError("Service URL not configured for MessageType: {MessageType}", messageType);
            return false;
        }

        var payload = new { MessageId = messageId };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync($"{targetUrl}/api/moderation/auto-remove", content, stoppingToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call auto-remove API for MessageId: {MessageId}", messageId);
            return false;
        }
    }
}
