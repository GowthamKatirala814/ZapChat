using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Moderation;

/// <summary>
/// What the caller asks the pipeline about. Scope is free text ("room" / "conversation")
/// so both chat services share one contract.
/// </summary>
public sealed record ModerationRequest(
    string Content,
    Guid? UserId,
    string AnonymousName,
    Guid ScopeId,
    string ScopeName,
    /// <summary>
    /// When true, an unavailable AI engine blocks the message instead of allowing it.
    /// Used for the HR channel, where the cost of leaking something outweighs the cost
    /// of a false rejection.
    /// </summary>
    bool FailClosed = false);

public sealed record ModerationVerdict(
    bool Allowed,
    string Category,
    double Confidence,
    string Reason,
    string Engine,
    string? MatchedRule)
{
    public static ModerationVerdict Allow(string engine) =>
        new(true, "SAFE", 1.0, string.Empty, engine, null);
}

/// <summary>
/// Two-stage content moderation, in ONE place.
///
/// This replaces Chat.Infrastructure.ContentModerationService (359 lines) and
/// PrivateChat.Infrastructure.ContentModerationService (267 lines), which were
/// near-identical copies that also re-declared the PII regexes already present in
/// <see cref="RuleBasedModerationService"/> — the same patterns written three times.
///
/// Stage 1: local rules. No I/O, no cost.
/// Stage 2: the AI classifier in Auth, only if stage 1 passes.
///
/// Identical content is decided once per 10 minutes via a content hash, so a burst of
/// repeated messages costs one classification.
/// </summary>
public interface IModerationPipeline
{
    Task<ModerationVerdict> EvaluateAsync(ModerationRequest request, CancellationToken ct = default);
}

public sealed class ModerationPipeline : IModerationPipeline
{
    public const string AiClientName = "ai-moderation";

    private static readonly TimeSpan CacheSliding = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CacheAbsolute = TimeSpan.FromMinutes(30);

    private readonly IRuleBasedModerationService _rules;
    private readonly IHttpClientFactory _httpClients;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ModerationPipeline> _logger;

    public ModerationPipeline(
        IRuleBasedModerationService rules,
        IHttpClientFactory httpClients,
        IMemoryCache cache,
        ILogger<ModerationPipeline> logger)
    {
        _rules = rules;
        _httpClients = httpClients;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ModerationVerdict> EvaluateAsync(
        ModerationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return ModerationVerdict.Allow("None");

        var cacheKey = "mod:" + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Content.Trim().ToLowerInvariant())));

        if (_cache.TryGetValue(cacheKey, out ModerationVerdict? cached) && cached is not null)
            return cached;

        var verdict = await EvaluateUncachedAsync(request, ct);

        // Only cache decisions the engine actually made. A verdict produced because
        // the classifier was unreachable must not be remembered for 10 minutes.
        if (verdict.Engine is not "Unavailable")
        {
            _cache.Set(cacheKey, verdict, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheSliding,
                AbsoluteExpirationRelativeToNow = CacheAbsolute,
                Size = 1
            });
        }

        return verdict;
    }

    private async Task<ModerationVerdict> EvaluateUncachedAsync(
        ModerationRequest request, CancellationToken ct)
    {
        // ── Stage 1: local rules ────────────────────────────────────────────────
        var rules = await _rules.ModerateAsync(request.Content);

        if (!rules.AllowMessage)
        {
            _logger.LogInformation(
                "Rules blocked a message from {Author} in {Scope}: {Category}.",
                request.AnonymousName, request.ScopeName, rules.Category);

            return new ModerationVerdict(
                false, rules.Category, rules.Confidence, rules.Reason,
                "Rules", rules.MatchedRules.FirstOrDefault());
        }

        // ── Stage 2: AI classification ──────────────────────────────────────────
        try
        {
            var client = _httpClients.CreateClient(AiClientName);

            if (client.BaseAddress is null)
            {
                _logger.LogWarning(
                    "The AI moderation client has no base address; ServiceUrls:AuthService is not configured.");
                return Unavailable(request, "AI moderation is not configured.");
            }

            var response = await client.PostAsJsonAsync(
                "api/ai-moderation/classify", new { content = request.Content }, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI moderation returned {Status}.", (int)response.StatusCode);
                return Unavailable(request, "AI moderation is unavailable.");
            }

            var result = await response.Content.ReadFromJsonAsync<AiVerdict>(ct);

            if (result is null || !result.EngineAvailable)
                return Unavailable(request, result?.Explanation ?? "AI moderation is unavailable.");

            if (result.IsSafe) return ModerationVerdict.Allow("Gemini");

            return new ModerationVerdict(
                false, result.Category, result.Confidence,
                BuildReason(result.Category), "Gemini", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI moderation call failed.");
            return Unavailable(request, "AI moderation failed.");
        }
    }

    /// <summary>
    /// The classifier could not be reached. The old services silently allowed the
    /// message and recorded the outcome as if it had been checked; here the caller is
    /// told, and a fail-closed scope rejects instead.
    /// </summary>
    private ModerationVerdict Unavailable(ModerationRequest request, string reason)
    {
        if (request.FailClosed)
        {
            return new ModerationVerdict(
                false, "UNVERIFIED", 0,
                "This channel requires content checks, which are temporarily unavailable. " +
                "Please try again shortly.",
                "Unavailable", null);
        }

        _logger.LogWarning(
            "Allowing an unverified message from {Author} in {Scope}: {Reason}",
            request.AnonymousName, request.ScopeName, reason);

        return new ModerationVerdict(true, "UNVERIFIED", 0, reason, "Unavailable", null);
    }

    private static string BuildReason(string category) => category.ToUpperInvariant() switch
    {
        "PROFANITY" => "Your message was blocked because it contains inappropriate language.",
        "HARASSMENT" => "Your message was blocked because it appears to contain harassment.",
        "THREAT" => "Your message was blocked because it was interpreted as a threat.",
        "HATE_SPEECH" => "Your message was blocked because it contains hateful content.",
        "SEXUAL" => "Your message was blocked because it contains sexual content.",
        "CONFIDENTIAL_INFORMATION" =>
            "Your message was blocked because it appears to contain confidential information.",
        "SPAM" => "Your message was blocked because it looks like spam.",
        _ => "Your message was blocked by content moderation."
    };

    private sealed record AiVerdict(
        bool IsSafe, string Category, double Confidence, string Explanation, bool EngineAvailable);
}

public static class ModerationRegistration
{
    /// <summary>
    /// Registers the rule engine and the two-stage pipeline. The caller registers the
    /// named HttpClient <see cref="ModerationPipeline.AiClientName"/> pointing at Auth.
    /// </summary>
    public static IServiceCollection AddModerationPipeline(
        this IServiceCollection services, string dictionariesPath = "")
    {
        services.AddRuleBasedModeration(dictionariesPath);
        services.AddScoped<IModerationPipeline, ModerationPipeline>();
        return services;
    }
}
