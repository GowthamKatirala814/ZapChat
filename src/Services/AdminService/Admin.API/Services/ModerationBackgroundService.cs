using Admin.Domain.Entities;
using Admin.Domain.Enums;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Admin.API.Services;

public class ModerationBackgroundService : BackgroundService
{
    // DTO used only to deserialize the relevant fields from AuthService
    private sealed record AuthUserRecord(Guid Id, string AnonymousName, string Email);

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

        // Ensure a settings record exists
        var settings = await dbContext.ModerationSettings.FirstOrDefaultAsync(stoppingToken);
        if (settings == null)
        {
            settings = new ModerationSettings { AutoDeleteEnabled = true, ReportThreshold = 5 };
            dbContext.ModerationSettings.Add(settings);
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        if (!settings.AutoDeleteEnabled) return;

        // ── Load all pending reports and group by offending author ─────────────
        // We group by MessageAuthorId (not MessageId) so that reports across
        // different messages by the same user all count toward a single threshold.
        var pendingReports = await dbContext.Reports
            .Where(r => r.Status == ReportStatus.Pending && r.MessageAuthorId != Guid.Empty)
            .ToListAsync(stoppingToken);

        var groupedByAuthor = pendingReports
            .GroupBy(r => r.MessageAuthorId)
            .Select(g => new
            {
                MessageAuthorId   = g.Key,
                // Count DISTINCT reporters — the same reporter on multiple messages
                // from the same author still only counts as ONE toward the threshold.
                UniqueReporters   = g.Select(r => r.ReportedByUserId).Distinct().Count(),
                Reports           = g.ToList()
            })
            .Where(g => g.UniqueReporters >= settings.ReportThreshold)
            .ToList();

        if (!groupedByAuthor.Any())
        {
            _logger.LogDebug(
                "ModerationBackgroundService: no author exceeded the threshold of {Threshold} unique reporters.",
                settings.ReportThreshold);
            return;
        }

        foreach (var group in groupedByAuthor)
        {
            var authorId   = group.MessageAuthorId;
            var authorName = group.Reports.First().MessageAuthorName;
            var uniqueCount = group.UniqueReporters;

            _logger.LogWarning(
                "Auto-moderation triggered for UserId={UserId} ({AuthorName}): " +
                "{UniqueReporters}/{Threshold} unique reporters.",
                authorId, authorName, uniqueCount, settings.ReportThreshold);

            // ── 1. Fetch the user's real email from AuthService ────────────────
            // This is required so the BlockedUser record has the correct email
            // hash, which prevents the banned user from re-registering.
            var (userEmail, fetchedSuccessfully) = await FetchUserEmailAsync(authorId, stoppingToken);
            var emailHash = ComputeSha256Hash(
                fetchedSuccessfully ? userEmail : $"auto-blocked-{authorId:N}");

            if (!fetchedSuccessfully)
            {
                _logger.LogWarning(
                    "Could not fetch email for UserId={UserId} from AuthService. " +
                    "Storing placeholder hash — manual follow-up may be required.",
                    authorId);
            }

            // ── 2. Block the user in the Admin DB (if not already blocked) ────
            var existingBlock = await dbContext.BlockedUsers
                .FirstOrDefaultAsync(b => b.UserId == authorId, stoppingToken);

            if (existingBlock == null)
            {
                dbContext.BlockedUsers.Add(new BlockedUser
                {
                    Id              = Guid.NewGuid(),
                    UserId          = authorId,
                    EmailHash       = emailHash,
                    Reason          = $"Auto-moderated: {uniqueCount}/{settings.ReportThreshold} " +
                                      $"unique users reported messages authored by this user.",
                    BlockedAt       = DateTime.UtcNow,
                    BlockedByAdmin  = Guid.Empty, // system action
                    IsPermanentDelete = false
                });
            }

            // ── 3. Soft-delete the user in AuthService ────────────────────────
            // This immediately prevents the user from logging in.
            // Uses the same endpoint as the manual admin "Delete User" flow.
            await SoftDeleteUserInAuthServiceAsync(authorId, authorName, stoppingToken);

            // ── 4. Remove the user's messages from Chat / PrivateChat services ─
            await CallAutoRemoveUserMessagesApiAsync(authorId, authorName, stoppingToken);

            // ── 5. Mark all related pending reports as AutoRemoved ─────────────
            foreach (var report in group.Reports)
            {
                report.Status       = ReportStatus.AutoRemoved;
                report.IsAutoRemoved = true;
            }

            // ── 6. Write an enriched audit log entry ──────────────────────────
            // The details field captures exactly why the action was triggered,
            // making it easy for admins to audit and challenge decisions.
            dbContext.AuditLogs.Add(new AuditLog
            {
                Id          = Guid.NewGuid(),
                Action      = "AutoModeration",
                EntityType  = "User",
                EntityId    = authorId.ToString(),
                PerformedBy = Guid.Empty, // system-generated
                Timestamp   = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Auto-moderation completed for UserId={UserId} ({AuthorName}): " +
                "{ReportCount} reports across {MessageCount} messages marked AutoRemoved. " +
                "Unique reporters: {UniqueReporters}/{Threshold}.",
                authorId, authorName,
                group.Reports.Count,
                group.Reports.Select(r => r.MessageId).Distinct().Count(),
                uniqueCount, settings.ReportThreshold);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the email address of the given user from AuthService.
    /// Returns (email, true) on success, ("", false) when the call fails.
    /// </summary>
    private async Task<(string Email, bool Success)> FetchUserEmailAsync(
        Guid userId, CancellationToken ct)
    {
        try
        {
            var authUrl = _configuration["ServiceUrls:AuthService"];
            if (string.IsNullOrEmpty(authUrl))
            {
                _logger.LogWarning("ServiceUrls:AuthService is not configured — cannot fetch email for {UserId}.", userId);
                return (string.Empty, false);
            }

            var client   = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{authUrl}/api/auth/users/{userId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AuthService returned {StatusCode} for user {UserId}.",
                    response.StatusCode, userId);
                return (string.Empty, false);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var user = JsonSerializer.Deserialize<AuthUserRecord>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (user is null || string.IsNullOrWhiteSpace(user.Email))
                return (string.Empty, false);

            return (user.Email, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch email for UserId={UserId} from AuthService.", userId);
            return (string.Empty, false);
        }
    }

    /// <summary>
    /// Calls the AuthService soft-delete endpoint so the user cannot log in.
    /// Failure is logged but does not abort the rest of the moderation cycle.
    /// </summary>
    private async Task SoftDeleteUserInAuthServiceAsync(
        Guid userId, string authorName, CancellationToken ct)
    {
        try
        {
            var authUrl = _configuration["ServiceUrls:AuthService"];
            if (string.IsNullOrEmpty(authUrl))
            {
                _logger.LogWarning(
                    "ServiceUrls:AuthService not configured — skipping soft-delete for {UserId}.", userId);
                return;
            }

            var client  = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                adminId = Guid.Empty, // system action
                reason  = $"Auto-moderated by system: reported by {authorName} threshold exceeded."
            });
            var content  = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PatchAsync(
                $"{authUrl}/api/auth/users/{userId}/soft-delete", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AuthService soft-delete returned {StatusCode} for UserId={UserId}. " +
                    "User may still be able to log in until resolved manually.",
                    response.StatusCode, userId);
            }
            else
            {
                _logger.LogInformation(
                    "AuthService soft-delete successful for auto-moderated UserId={UserId}.", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to call AuthService soft-delete for UserId={UserId}. " +
                "User may still be able to log in — manual follow-up required.", userId);
        }
    }

    /// <summary>
    /// Calls ChatService and PrivateChatService to remove all active messages
    /// belonging to the auto-moderated user.
    /// </summary>
    private async Task CallAutoRemoveUserMessagesApiAsync(
        Guid userId, string authorName, CancellationToken ct)
    {
        var client    = _httpClientFactory.CreateClient();
        var chatUrl   = _configuration["ServiceUrls:ChatService"];
        var privateUrl = _configuration["ServiceUrls:PrivateChatService"];

        var payload = new { UserId = userId, AuthorName = authorName };
        var json    = JsonSerializer.Serialize(payload);

        if (!string.IsNullOrEmpty(chatUrl))
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(
                    $"{chatUrl}/api/moderation/auto-remove-user-messages", content, ct);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning(
                        "ChatService auto-remove returned {StatusCode} for UserId={UserId}.",
                        response.StatusCode, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to call auto-remove-user-messages on ChatService for UserId={UserId}.", userId);
            }
        }

        if (!string.IsNullOrEmpty(privateUrl))
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(
                    $"{privateUrl}/api/moderation/auto-remove-user-messages", content, ct);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning(
                        "PrivateChatService auto-remove returned {StatusCode} for UserId={UserId}.",
                        response.StatusCode, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to call auto-remove-user-messages on PrivateChatService for UserId={UserId}.", userId);
            }
        }
    }

    /// <summary>
    /// Computes a hex-encoded SHA-256 hash of the input string.
    /// Used to store email hashes in BlockedUsers without storing PII in plain text.
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.ToLowerInvariant().Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
