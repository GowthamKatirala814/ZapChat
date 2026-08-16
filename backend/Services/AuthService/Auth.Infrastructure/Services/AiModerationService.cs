using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Services;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Supplied by user-secrets or ZAPCHAT_GEMINI__APIKEY. Never committed.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-2.5-flash";
    public int TimeoutSeconds { get; set; } = 10;
    public int EstimatedDailyQuota { get; set; } = 1500;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// Classifies content with Gemini and records usage.
///
/// Returns <see cref="AiModerationResult.EngineAvailable"/> = false when the model
/// cannot be reached, rather than pretending the content is safe. The caller decides
/// what to do with that — Chat allows the message, the HR room does not. Previously
/// the failure was indistinguishable from a "safe" verdict.
/// </summary>
public sealed class AiModerationService : IAiModerationService
{
    private readonly IHttpClientFactory _httpClients;
    private readonly IAiUsageRepository _usage;
    private readonly GeminiOptions _options;
    private readonly ILogger<AiModerationService> _logger;

    public AiModerationService(
        IHttpClientFactory httpClients,
        IAiUsageRepository usage,
        IOptions<GeminiOptions> options,
        ILogger<AiModerationService> logger)
    {
        _httpClients = httpClients;
        _usage = usage;
        _options = options.Value;
        _logger = logger;
    }

    private const string Prompt = """
        You are a workplace chat moderator. Classify the message below.
        Respond with ONLY a JSON object, no markdown fence, in exactly this shape:
        {"category":"SAFE|PROFANITY|HARASSMENT|THREAT|HATE_SPEECH|SEXUAL|CONFIDENTIAL_INFORMATION|SPAM","confidence":0.0,"explanation":"one short sentence"}
        Use SAFE when the message is acceptable workplace communication.
        Message:
        """;

    public async Task<AiModerationResult> ClassifyAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new AiModerationResult(true, "SAFE", 1.0, "Empty message.", true);

        if (!_options.IsConfigured)
        {
            await _usage.RecordOutcomeAsync(false, false, "Configuration",
                "Gemini:ApiKey is not configured.", ct);

            return new AiModerationResult(true, "UNKNOWN", 0,
                "AI moderation is not configured.", EngineAvailable: false);
        }

        try
        {
            var client = _httpClients.CreateClient("gemini");

            var response = await client.PostAsJsonAsync(
                $"v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}",
                new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = Prompt + content } } }
                    },
                    generationConfig = new { temperature = 0.0, maxOutputTokens = 200 }
                }, ct);

            if (!response.IsSuccessStatusCode)
            {
                var kind = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "RateLimited",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Authentication",
                    HttpStatusCode.BadRequest => "Configuration",
                    _ when (int)response.StatusCode >= 500 => "Server",
                    _ => "InvalidResponse"
                };

                await _usage.RecordOutcomeAsync(false, false, kind,
                    $"Gemini returned {(int)response.StatusCode}.", ct);

                _logger.LogWarning("Gemini returned {Status} ({Kind}).", response.StatusCode, kind);

                return new AiModerationResult(true, "UNKNOWN", 0,
                    "AI moderation is unavailable.", EngineAvailable: false);
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(ct);
            var text = payload?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;

            if (string.IsNullOrWhiteSpace(text))
            {
                await _usage.RecordOutcomeAsync(false, false, "InvalidResponse",
                    "Gemini returned an empty candidate.", ct);
                return new AiModerationResult(true, "UNKNOWN", 0,
                    "AI moderation returned no verdict.", EngineAvailable: false);
            }

            var verdict = ParseVerdict(text);

            if (verdict is null)
            {
                await _usage.RecordOutcomeAsync(false, false, "InvalidResponse",
                    "Could not parse the Gemini verdict.", ct);
                return new AiModerationResult(true, "UNKNOWN", 0,
                    "AI moderation returned an unreadable verdict.", EngineAvailable: false);
            }

            var isSafe = verdict.category.Equals("SAFE", StringComparison.OrdinalIgnoreCase);
            await _usage.RecordOutcomeAsync(true, !isSafe, null, null, ct);

            return new AiModerationResult(
                isSafe, verdict.category.ToUpperInvariant(), verdict.confidence,
                verdict.explanation ?? string.Empty, EngineAvailable: true);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            await _usage.RecordOutcomeAsync(false, false, "Timeout", "Gemini call timed out.", ct);
            return new AiModerationResult(true, "UNKNOWN", 0,
                "AI moderation timed out.", EngineAvailable: false);
        }
        catch (Exception ex)
        {
            await _usage.RecordOutcomeAsync(false, false, "Server", ex.Message, ct);
            _logger.LogError(ex, "Gemini moderation call failed.");
            return new AiModerationResult(true, "UNKNOWN", 0,
                "AI moderation failed.", EngineAvailable: false);
        }
    }

    /// <summary>Tolerates a ```json fence, which the model sometimes adds.</summary>
    private static Verdict? ParseVerdict(string text)
    {
        var trimmed = text.Trim();

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            return JsonSerializer.Deserialize<Verdict>(
                trimmed[start..(end + 1)],
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<AiHealthDto> GetHealthAsync(CancellationToken ct = default)
    {
        var today = await _usage.GetOrCreateTodayAsync(ct);
        var quota = today.EstimatedDailyQuota > 0
            ? today.EstimatedDailyQuota
            : _options.EstimatedDailyQuota;

        return new AiHealthDto(
            Status: _options.IsConfigured ? today.Status : "NotConfigured",
            RequestsToday: today.Requests,
            EstimatedQuota: quota,
            UsagePercentage: quota > 0 ? Math.Round(today.Requests / (double)quota * 100, 2) : 0,
            Successful: today.Successful,
            Failed: today.Failed,
            BlockedMessages: today.BlockedMessages,
            SafeMessages: today.SafeMessages,
            Errors: today.Errors,
            LastSuccessAt: today.LastSuccessAt,
            LastFailureAt: today.LastFailureAt,
            LastErrorMessage: today.LastErrorMessage,
            Events: today.Events
                .OrderByDescending(e => e.Timestamp)
                .Take(50)
                .ToList());
    }

    // Minimal shapes for the parts of the Gemini response we read.
    private sealed record Verdict(string category, double confidence, string? explanation);
    private sealed record GeminiResponse(GeminiCandidate[]? candidates);
    private sealed record GeminiCandidate(GeminiContent? content);
    private sealed record GeminiContent(GeminiPart[]? parts);
    private sealed record GeminiPart(string? text);
}
