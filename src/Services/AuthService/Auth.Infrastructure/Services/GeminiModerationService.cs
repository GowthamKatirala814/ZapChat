using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Services;

public class GeminiModerationService : IGeminiModerationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiModerationService> _logger;
    private readonly AuthDbContext _context;
    private readonly IEmailService _emailService;

    public GeminiModerationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiModerationService> logger,
        AuthDbContext context,
        IEmailService emailService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _emailService = emailService;
    }

    public async Task<GeminiModerationResponse> ModerateContentAsync(GeminiModerationRequest request)
    {
        var apiKey = _configuration["GeminiSettings:ApiKey"];
        var model = _configuration["GeminiSettings:Model"] ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("[GeminiModeration] API key is not configured. Allowing message through.");
            await UpdateHealthStatusAsync("Configuration Error", "Missing API Key", isConfigurationError: true);
            return Allow();
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("Gemini");
            var endpoint = $"v1beta/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role  = "user",
                        parts = new[] { new { text = BuildPrompt(request.Content) } }
                    }
                },
                safetySettings = new[]
                {
                    new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                    new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature      = 0.1
                }
            };

            var httpResponse = await client.PostAsJsonAsync(endpoint, requestBody);
            sw.Stop();

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                    httpResponse.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    _logger.LogWarning("[GeminiModeration] Quota exhausted (429) after {ElapsedMs}ms.", sw.ElapsedMilliseconds);
                    await UpdateHealthStatusAsync("Rate Limited", "HTTP 429 Rate Limit Exceeded", is429: true);
                }
                else if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                         httpResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("[GeminiModeration] Authentication error {Status} after {ElapsedMs}ms.", (int)httpResponse.StatusCode, sw.ElapsedMilliseconds);
                    await UpdateHealthStatusAsync("Authentication Error", $"HTTP {(int)httpResponse.StatusCode} Auth Error", isAuthenticationError: true);
                }
                else if ((int)httpResponse.StatusCode >= 500)
                {
                    _logger.LogWarning("[GeminiModeration] Server error {Status} after {ElapsedMs}ms.", (int)httpResponse.StatusCode, sw.ElapsedMilliseconds);
                    await UpdateHealthStatusAsync("Server Error", $"HTTP {(int)httpResponse.StatusCode} Server Error", isServerError: true);
                }
                else
                {
                    _logger.LogWarning("[GeminiModeration] Non-success HTTP {Status} after {ElapsedMs}ms. Failing open.", (int)httpResponse.StatusCode, sw.ElapsedMilliseconds);
                    await UpdateHealthStatusAsync("Offline", $"HTTP {(int)httpResponse.StatusCode} Error", isFailure: true);
                }
                return Allow();
            }

            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            var response = ParseGeminiResponse(responseJson);

            // Track usage on success (Healthy state)
            _logger.LogDebug("[GeminiModeration] Request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            await UpdateHealthStatusAsync("Healthy", null, isSuccess: true, isBlocked: !response.AllowMessage);

            return response;
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException)
        {
            sw.Stop();
            _logger.LogWarning("[GeminiModeration] Circuit broken. Failing open. ({ElapsedMs}ms)", sw.ElapsedMilliseconds);
            await UpdateHealthStatusAsync("Rate Limited", "Circuit Broken (Rate Limit/Failures)", is429: true);
            return Allow();
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("[GeminiModeration] Request timed out after {ElapsedMs}ms. Failing open.", sw.ElapsedMilliseconds);
            await UpdateHealthStatusAsync("Degraded", "Connection Timeout", isTimeout: true);
            return Allow();
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[GeminiModeration] Unexpected error after {ElapsedMs}ms. Failing open.", sw.ElapsedMilliseconds);
            await UpdateHealthStatusAsync("Offline", ex.Message, isFailure: true);
            return Allow();
        }
    }

    private static GeminiModerationResponse Allow() => new() { AllowMessage = true, Category = "SAFE", Confidence = 1.0 };

    private static string BuildPrompt(string messageContent)
    {
        return $"""
            You are a workplace communication content moderation AI for an internal company chat application.

            Evaluate the following chat message and classify it into exactly one of these categories:
            SAFE, TOXIC, HARASSMENT, HATE_SPEECH, PROFANITY, SPAM,
            CONFIDENTIAL_INFORMATION, PERSONAL_INFORMATION, THREAT, OTHER

            Rules:
            - URLs and links are ALLOWED. Do NOT classify a message as SPAM purely because it contains a URL.
            - Judge the OVERALL CONTEXT of the message, not isolated words.
            - If in doubt, lean toward SAFE.

            Respond with ONLY a valid JSON object — no markdown fences, no backticks, no extra text.
            The JSON must have exactly these four fields:
              "allowMessage": boolean  (true only when category is SAFE)
              "category":    string    (one of the categories listed above)
              "confidence":  number    (0.0 to 1.0)
              "explanation": string    (brief internal reason, ≤ 100 characters)

            Message to evaluate:
            {messageContent}
            """;
    }

    private GeminiModerationResponse ParseGeminiResponse(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var candidates = doc.RootElement.GetProperty("candidates");

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            text = text.Trim();
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text[7..];
            else if (text.StartsWith("```"))
                text = text[3..];
            if (text.EndsWith("```"))
                text = text[..^3];
            text = text.Trim();

            using var inner = JsonDocument.Parse(text);
            var root = inner.RootElement;

            return new GeminiModerationResponse
            {
                AllowMessage = root.GetProperty("allowMessage").GetBoolean(),
                Category = root.GetProperty("category").GetString() ?? "OTHER",
                Confidence = root.GetProperty("confidence").GetDouble(),
                Explanation = root.GetProperty("explanation").GetString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GeminiModeration] Failed to parse response. Failing open.");
            // Record invalid response asynchronously without awaiting here to avoid modifying signature
            _ = UpdateHealthStatusAsync("Parse Error", "Invalid JSON Response", isInvalidResponse: true);
            return Allow();
        }
    }

    private async Task<GeminiUsage> GetOrCreateDailyTrackerAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tracker = await _context.GeminiUsages.FirstOrDefaultAsync(t => t.Date == today);

        if (tracker == null)
        {
            tracker = new GeminiUsage
            {
                Id = Guid.NewGuid(),
                Date = today,
                RequestsToday = 0,
                EstimatedDailyQuota = _configuration.GetValue<int>("GeminiMonitoring:EstimatedDailyQuota", 1500),
                UsagePercentage = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.GeminiUsages.Add(tracker);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Automatic Daily Reset handles itself if today is a new day, but in this logic we check by Date == today.
            // So if it's a new day, we create a new row anyway. "There should only ever be one active row per day."
        }

        return tracker;
    }

    private async Task UpdateHealthStatusAsync(
        string newStatus, 
        string? errorMessage, 
        bool isSuccess = false, 
        bool isBlocked = false, 
        bool is429 = false, 
        bool isTimeout = false, 
        bool isFailure = false,
        bool isConfigurationError = false,
        bool isAuthenticationError = false,
        bool isServerError = false,
        bool isInvalidResponse = false)
    {
        try
        {
            var tracker = await GetOrCreateDailyTrackerAsync();
            
            var previousStatus = tracker.CurrentStatus;
            
            // Increment appropriate counters
            tracker.RequestsToday++;
            
            if (isSuccess)
            {
                tracker.SuccessfulRequests++;
                tracker.LastSuccessfulModeration = DateTime.UtcNow;
                if (isBlocked) tracker.BlockedMessages++;
                else tracker.SafeMessages++;
                
                if (previousStatus != "Healthy")
                {
                    tracker.RecoveryTime = DateTime.UtcNow;
                }
            }
            else
            {
                tracker.FailedRequests++;
                tracker.LastFailedModeration = DateTime.UtcNow;
                tracker.LastErrorMessage = errorMessage;
                
                if (is429) tracker.Error429s++;
                if (isTimeout) tracker.TimeoutErrors++;
                if (isConfigurationError) tracker.ConfigurationErrors++;
                if (isAuthenticationError) tracker.AuthenticationErrors++;
                if (isServerError) tracker.ServerErrors++;
                if (isInvalidResponse) tracker.InvalidResponses++;
            }
            
            tracker.CurrentStatus = newStatus;
            
            // Usage percentage updates
            var quota = tracker.EstimatedDailyQuota > 0 ? tracker.EstimatedDailyQuota : 1500;
            tracker.UsagePercentage = (double)tracker.RequestsToday / quota * 100;
            tracker.LastUpdated = DateTime.UtcNow;
            
            // Status changed!
            if (previousStatus != newStatus)
            {
                _logger.LogInformation("[GeminiModeration] Status changed from {Old} to {New}", previousStatus, newStatus);
                
                // Add event to timeline
                _context.AiHealthEvents.Add(new AiHealthEvent
                {
                    Id = Guid.NewGuid(),
                    Date = tracker.Date,
                    Timestamp = DateTime.UtcNow,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Message = errorMessage ?? "Recovered to Healthy"
                });
                
                // Notify admin via Email
                await SendStateChangeEmailAsync(previousStatus, newStatus, errorMessage);
            }

            await _context.SaveChangesAsync();
            await CheckThresholdsAndNotifyAsync(tracker);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GeminiModeration] Failed to track Gemini health metrics.");
        }
    }

    private async Task SendStateChangeEmailAsync(string previousStatus, string newStatus, string? errorMessage)
    {
        var isCritical = newStatus != "Healthy";
        var subject = $"ZapChat AI Health Alert: {newStatus}";
        var recommendation = isCritical ? $"AI Moderation is currently experiencing issues. Error: {errorMessage}" : "AI Moderation has recovered and is operating normally.";
        
        await SendNotificationEmailAsync(newStatus, subject, recommendation, 0, 0, isCritical);
    }

    private async Task CheckThresholdsAndNotifyAsync(GeminiUsage tracker)
    {
        var percentage = tracker.UsagePercentage;

        if (percentage >= 100 && !tracker.EmailSent100)
        {
            tracker.EmailSent100 = true;
            tracker.LastThresholdReached = "100%";
            await _context.SaveChangesAsync();
            await SendNotificationEmailAsync("100%", "Gemini AI Quota Exhausted", "Gemini AI quota has been exhausted.", tracker.RequestsToday, tracker.EstimatedDailyQuota, isCritical: true);
        }
        else if (percentage >= 90 && percentage < 100 && !tracker.EmailSent90)
        {
            tracker.EmailSent90 = true;
            tracker.LastThresholdReached = "90%";
            await _context.SaveChangesAsync();
            await SendNotificationEmailAsync("90%", "Gemini AI Usage Critical", "Gemini AI usage is critically high.", tracker.RequestsToday, tracker.EstimatedDailyQuota, isCritical: true);
        }
        else if (percentage >= 50 && percentage < 90 && !tracker.EmailSent50)
        {
            tracker.EmailSent50 = true;
            tracker.LastThresholdReached = "50%";
            await _context.SaveChangesAsync();
            await SendNotificationEmailAsync("50%", "Gemini AI Usage Alert", "Gemini AI usage has reached 50%.", tracker.RequestsToday, tracker.EstimatedDailyQuota, isCritical: false);
        }
    }

    private async Task SendNotificationEmailAsync(string threshold, string subject, string recommendation, int currentRequests, int quota, bool isCritical)
    {
        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("[GeminiModeration] AdminEmail is not configured. Cannot send usage notification.");
            return;
        }

        var dateStr = DateTime.UtcNow.ToString("f");
        var color = isCritical ? "#d32f2f" : "#1976d2";
        var percentage = (double)currentRequests / quota * 100;
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }}
        .header {{ text-align: center; padding-bottom: 20px; border-bottom: 2px solid #007bff; margin-bottom: 20px; }}
        .header h1 {{ color: #007bff; margin: 0; }}
        .content {{ padding: 10px 0; }}
        .highlight {{ font-weight: bold; color: {color}; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ZapChat Admin</h1>
        </div>
        <div class='content'>
            <h2>{subject}</h2>
            <p>{recommendation}</p>
            <ul>
                <li><strong>Threshold Reached:</strong> <span class='highlight'>{threshold}</span></li>
                <li><strong>Current Percentage:</strong> {percentage:F2}%</li>
                <li><strong>Requests Used:</strong> {currentRequests}</li>
                <li><strong>Estimated Quota:</strong> {quota}</li>
                <li><strong>Time:</strong> {dateStr} UTC</li>
            </ul>
            <p>Please monitor your Google Cloud / AI Studio dashboard for more detailed usage metrics.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from ZapChat Administration.</p>
        </div>
    </div>
</body>
</html>";

        try
        {
            await _emailService.SendEmailAsync(adminEmail, subject, body);
            _logger.LogInformation("[GeminiModeration] Successfully sent {Threshold} threshold email to {AdminEmail}", threshold, adminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GeminiModeration] Failed to send {Threshold} threshold email to {AdminEmail}", threshold, adminEmail);
        }
    }

    public async Task<object> GetUsageStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tracker = await _context.GeminiUsages.FirstOrDefaultAsync(t => t.Date == today);

        if (tracker == null)
        {
            return new
            {
                RequestsToday = 0,
                EstimatedQuota = _configuration.GetValue<int>("GeminiMonitoring:EstimatedDailyQuota", 1500),
                UsagePercentage = 0.0,
                RemainingEstimatedRequests = _configuration.GetValue<int>("GeminiMonitoring:EstimatedDailyQuota", 1500),
                LastThresholdReached = (string?)null,
                QuotaStatus = "OK",
                LastUpdated = DateTime.UtcNow
            };
        }

        var remaining = tracker.EstimatedDailyQuota - tracker.RequestsToday;
        
        // Calculate Uptime Percentage for today
        double uptimePercentage = 100.0;
        if (tracker.RequestsToday > 0)
        {
            uptimePercentage = ((double)tracker.SuccessfulRequests / tracker.RequestsToday) * 100;
        }

        return new
        {
            RequestsToday = tracker.RequestsToday,
            EstimatedQuota = tracker.EstimatedDailyQuota,
            UsagePercentage = tracker.UsagePercentage,
            RemainingEstimatedRequests = remaining < 0 ? 0 : remaining,
            LastThresholdReached = tracker.LastThresholdReached,
            QuotaStatus = tracker.QuotaExhausted ? "EXHAUSTED" : "OK",
            
            // New AI Health Fields
            CurrentStatus = tracker.CurrentStatus,
            SuccessfulRequests = tracker.SuccessfulRequests,
            BlockedMessages = tracker.BlockedMessages,
            SafeMessages = tracker.SafeMessages,
            FailedRequests = tracker.FailedRequests,
            Error429s = tracker.Error429s,
            TimeoutErrors = tracker.TimeoutErrors,
            ConfigurationErrors = tracker.ConfigurationErrors,
            AuthenticationErrors = tracker.AuthenticationErrors,
            ServerErrors = tracker.ServerErrors,
            InvalidResponses = tracker.InvalidResponses,
            LastSuccessfulModeration = tracker.LastSuccessfulModeration,
            LastFailedModeration = tracker.LastFailedModeration,
            LastErrorMessage = tracker.LastErrorMessage,
            RecoveryTime = tracker.RecoveryTime,
            UptimePercentage = Math.Round(uptimePercentage, 2),
            
            LastUpdated = tracker.LastUpdated,
            
            // Attach today's timeline events
            Events = await _context.AiHealthEvents
                        .Where(e => e.Date == tracker.Date)
                        .OrderByDescending(e => e.Timestamp)
                        .ToListAsync()
        };
    }
}
