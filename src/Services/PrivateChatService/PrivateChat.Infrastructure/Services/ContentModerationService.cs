using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PrivateChat.Application.DTOs;
using PrivateChat.Application.Interfaces;
using PrivateChat.Domain.Entities;
using PrivateChat.Infrastructure.Persistence.DbContexts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PrivateChat.Infrastructure.Services;

/// <summary>
/// Two-stage content moderation service for private messages.
///
/// Stage 1 — Fast local rule-based checks (no network, no API cost, no DB write).
/// Stage 2 — Gemini AI classification (only when rules pass).
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentModerationService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly PrivateChatDbContext _context;

    private const string CacheKeyPrefix = "zc:privmod:v1:";
    private static readonly TimeSpan CacheSlidingExpiry       = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CacheAbsoluteExpiry      = TimeSpan.FromMinutes(30);

    // ── Pre-compiled regexes (static = compiled once, shared across all instances) ──

    private static readonly Regex EmailRegex = new(
        @"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"(\+91[\-\s]?)?[6-9]\d{9}",
        RegexOptions.Compiled);

    private static readonly Regex AadhaarRegex = new(
        @"\b\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex PanRegex = new(
        @"\b[A-Z]{5}[0-9]{4}[A-Z]\b",
        RegexOptions.Compiled);

    public ContentModerationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ContentModerationService> logger,
        IMemoryCache memoryCache,
        PrivateChatDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
        _logger            = logger;
        _memoryCache       = memoryCache;
        _context           = context;
    }

    public async Task<ModerationResult> ModerateAsync(ModerationRequest request)
    {
        var cacheKey = ComputeCacheKey(request.Content);

        if (_memoryCache.TryGetValue(cacheKey, out ModerationResult? cached) && cached is not null)
        {
            _logger.LogDebug(
                "[PrivateModeration:Cache] Hit. User={User} Conv={Conv} CachedDecision={Decision}",
                request.AnonymousName, request.ConversationId, cached.AllowMessage ? "ALLOW" : "BLOCK");
            return cached;
        }

        ModerationResult result;

        var ruleResult = ApplyRules(request.Content);
        if (ruleResult is not null)
        {
            _logger.LogWarning(
                "[PrivateModeration:Rule] Blocked. User={User} Conv={Conv} Category={Category} Explanation={Explanation}",
                request.AnonymousName, request.ConversationId, ruleResult.Category, ruleResult.Explanation);
            result = ruleResult;
        }
        else
        {
            result = await CallGeminiAsync(request);
        }

        await SaveAuditLogAsync(request, result);
        CacheResult(cacheKey, result);

        return result;
    }

    private static string ComputeCacheKey(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content.ToLowerInvariant().Trim());
        var hash  = SHA256.HashData(bytes);
        return CacheKeyPrefix + Convert.ToHexString(hash);
    }

    private void CacheResult(string key, ModerationResult result)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration              = CacheSlidingExpiry,
            AbsoluteExpirationRelativeToNow = CacheAbsoluteExpiry
        };
        _memoryCache.Set(key, result, options);
    }

    private ModerationResult? ApplyRules(string content)
    {
        var settings = _configuration.GetSection("ModerationSettings");

        if (string.IsNullOrWhiteSpace(content))
            return ModerationResult.Block("SPAM", "Empty message", "Your message appears to be empty.", isRuleBased: true);

        var maxLength = settings.GetValue<int>("MaxMessageLength", 2000);
        if (content.Length > maxLength)
            return ModerationResult.Block("SPAM", $"Message exceeds {maxLength} characters", $"Your message is too long.", isRuleBased: true);

        var maxRepeated = settings.GetValue<int>("MaxRepeatedChars", 8);
        if (Regex.IsMatch(content, $@"(.)\1{{{maxRepeated},}}"))
            return ModerationResult.Block("SPAM", "Repeated character spam detected", "Your message looks like spam. Please avoid repetitive characters.", isRuleBased: true);

        var bannedWords = settings.GetSection("BannedWords").Get<string[]>() ?? Array.Empty<string>();
        foreach (var word in bannedWords.Where(w => !string.IsNullOrWhiteSpace(w)))
        {
            if (Regex.IsMatch(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
                return ModerationResult.Block("PROFANITY", $"Matched banned word: {word}", "Your message contains language that is not allowed.", isRuleBased: true);
        }

        var blockedKeywords = settings.GetSection("BlockedKeywords").Get<string[]>() ?? Array.Empty<string>();
        foreach (var keyword in blockedKeywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return ModerationResult.Block("CONFIDENTIAL_INFORMATION", $"Matched blocked keyword: {keyword}", "Your message contains restricted content.", isRuleBased: true);
        }

        if (settings.GetValue<bool>("BlockEmailAddresses", true) && EmailRegex.IsMatch(content))
            return ModerationResult.Block("PERSONAL_INFORMATION", "Email address detected", "Sharing email addresses is not allowed.", isRuleBased: true);

        if (settings.GetValue<bool>("BlockPhoneNumbers", true) && PhoneRegex.IsMatch(content))
            return ModerationResult.Block("PERSONAL_INFORMATION", "Phone number detected", "Sharing phone numbers is not permitted.", isRuleBased: true);

        if (settings.GetValue<bool>("BlockAadhaarPan", true))
        {
            if (AadhaarRegex.IsMatch(content) || PanRegex.IsMatch(content))
                return ModerationResult.Block("PERSONAL_INFORMATION", "Government ID detected", "Sharing government IDs is strictly prohibited.", isRuleBased: true);
        }

        if (settings.GetValue<bool>("BlockUrls", false) && Regex.IsMatch(content, @"https?://\S+", RegexOptions.IgnoreCase))
            return ModerationResult.Block("SPAM", "URL detected", "Sharing links is not allowed.", isRuleBased: true);

        return null;
    }

    private async Task<ModerationResult> CallGeminiAsync(ModerationRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var requestBody = new { content = request.Content };
            var httpResponse = await client.PostAsJsonAsync("/api/gemini-moderation/moderate", requestBody);
            
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("[PrivateModeration:Gemini] AuthService returned non-success HTTP {Status}. Failing open.", (int)httpResponse.StatusCode);
                return ModerationResult.Allow();
            }
            
            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            
            var allowMessage = root.GetProperty("allowMessage").GetBoolean();
            var category = root.GetProperty("category").GetString() ?? "OTHER";
            var confidence = root.GetProperty("confidence").GetDouble();
            var explanation = root.GetProperty("explanation").GetString() ?? string.Empty;
            
            if (allowMessage)
            {
                _logger.LogInformation("[PrivateModeration:Gemini] SAFE. User={User} Conv={Conv} Confidence={Confidence:F2}", request.AnonymousName, request.ConversationId, confidence);
                return ModerationResult.Allow();
            }

            _logger.LogWarning("[PrivateModeration:Gemini] Blocked. User={User} Conv={Conv} Category={Category} Confidence={Confidence:F2} Explanation={Explanation}", request.AnonymousName, request.ConversationId, category, confidence, explanation);
            return ModerationResult.Block(category, explanation, BuildUserFriendlyReason(category), confidence);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[PrivateModeration:Gemini] Request timed out. Failing open.");
            return ModerationResult.Allow();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PrivateModeration:Gemini] Unexpected error. Failing open.");
            return ModerationResult.Allow();
        }
    }

    private async Task SaveAuditLogAsync(ModerationRequest request, ModerationResult result)
    {
        try
        {
            var snippet = request.Content.Length <= 200 ? request.Content : request.Content[..200];

            var log = new PrivateModerationAuditLog
            {
                UserId             = request.UserId,
                AnonymousName      = request.AnonymousName,
                ConversationId     = request.ConversationId,
                MessageSnippet     = snippet,
                Category           = result.Category,
                Confidence         = result.Confidence,
                WasAllowed         = result.AllowMessage,
                WasRuleBasedBlock  = result.IsRuleBasedBlock,
                Explanation        = result.Explanation,
                Timestamp          = DateTime.UtcNow
            };

            _context.PrivateModerationAuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PrivateModeration:Audit] Failed to save audit log entry. Decision is unaffected.");
        }
    }

    private static string BuildUserFriendlyReason(string category) => category switch
    {
        "TOXIC"                    => "Your message was blocked because it contains toxic content.",
        "HARASSMENT"               => "Your message was blocked because it appears to contain harassment.",
        "HATE_SPEECH"              => "Your message was blocked because it contains language that is not tolerated.",
        "PROFANITY"                => "Your message was blocked because it contains inappropriate language.",
        "SPAM"                     => "Your message was blocked because it appears to be spam.",
        "CONFIDENTIAL_INFORMATION" => "Your message was blocked because it may contain confidential information.",
        "PERSONAL_INFORMATION"     => "Your message was blocked because it appears to contain personal information.",
        "THREAT"                   => "Your message was blocked because it contains content interpreted as a threat.",
        _                          => "Your message was blocked because it violates communication guidelines."
    };
}
