using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chat.Infrastructure.Services;

/// <summary>
/// Two-stage content moderation service.
///
/// Stage 1 — Fast local rule-based checks (no network, no API cost, no DB write).
/// Stage 2 — Gemini AI classification (only when rules pass).
///
/// Deduplication: identical message text (SHA-256 of lowercase-trimmed content)
/// is evaluated only once per 10-minute sliding window via IMemoryCache.
/// Cache hits skip both rule evaluation and the Gemini call entirely.
/// Audit logs are written only on cache misses (first occurrence of each message hash).
///
/// Reliability: the service is deliberately FAIL-OPEN.
/// A Gemini outage, timeout, or parse error logs a warning and returns Allow()
/// so the existing chat flow is never blocked by a third-party dependency.
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentModerationService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ChatDbContext _context;

    private const string CacheKeyPrefix = "zc:mod:v1:";
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

    // PAN: AAAAA9999A
    private static readonly Regex PanRegex = new(
        @"\b[A-Z]{5}[0-9]{4}[A-Z]\b",
        RegexOptions.Compiled);

    public ContentModerationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ContentModerationService> logger,
        IMemoryCache memoryCache,
        ChatDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
        _logger            = logger;
        _memoryCache       = memoryCache;
        _context           = context;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public entry point
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ModerationResult> ModerateAsync(ModerationRequest request)
    {
        var cacheKey = ComputeCacheKey(request.Content);

        // ── Cache hit: return immediately, no Gemini call, no audit log ───────
        if (_memoryCache.TryGetValue(cacheKey, out ModerationResult? cached) && cached is not null)
        {
            _logger.LogDebug(
                "[Moderation:Cache] Hit. User={User} Room={Room} CachedDecision={Decision}",
                request.AnonymousName, request.RoomName, cached.AllowMessage ? "ALLOW" : "BLOCK");
            return cached;
        }

        // ── Cache miss: run the full two-stage pipeline ───────────────────────
        ModerationResult result;

        var ruleResult = ApplyRules(request.Content);
        if (ruleResult is not null)
        {
            // Stage 1 blocked it
            _logger.LogWarning(
                "[Moderation:Rule] Blocked. User={User} Room={Room} Category={Category} Explanation={Explanation}",
                request.AnonymousName, request.RoomName, ruleResult.Category, ruleResult.Explanation);

            result = ruleResult;
        }
        else
        {
            // Stage 2: call Gemini
            result = await CallGeminiAsync(request);
        }

        // Write audit log for every cache-miss decision
        await SaveAuditLogAsync(request, result);

        // Store result in cache so identical subsequent messages skip all processing
        CacheResult(cacheKey, result);

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cache helpers
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // Stage 1 — Rule-based validation (synchronous, no I/O)
    // ─────────────────────────────────────────────────────────────────────────

    private ModerationResult? ApplyRules(string content)
    {
        var settings = _configuration.GetSection("ModerationSettings");

        // 1. Empty / whitespace
        if (string.IsNullOrWhiteSpace(content))
            return ModerationResult.Block(
                "SPAM", "Empty message",
                "Your message appears to be empty.",
                isRuleBased: true);

        // 2. Maximum length
        var maxLength = settings.GetValue<int>("MaxMessageLength", 2000);
        if (content.Length > maxLength)
            return ModerationResult.Block(
                "SPAM", $"Message exceeds {maxLength} characters",
                $"Your message is too long. Please keep messages under {maxLength} characters.",
                isRuleBased: true);

        // 3. Repeated-character spam (e.g. "aaaaaaaaaa")
        var maxRepeated = settings.GetValue<int>("MaxRepeatedChars", 8);
        if (Regex.IsMatch(content, $@"(.)\1{{{maxRepeated},}}"))
            return ModerationResult.Block(
                "SPAM", "Repeated character spam detected",
                "Your message looks like spam. Please avoid repetitive characters.",
                isRuleBased: true);

        // 4. Banned words (whole-word, case-insensitive)
        var bannedWords = settings.GetSection("BannedWords").Get<string[]>() ?? Array.Empty<string>();
        foreach (var word in bannedWords.Where(w => !string.IsNullOrWhiteSpace(w)))
        {
            if (Regex.IsMatch(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
                return ModerationResult.Block(
                    "PROFANITY", $"Matched banned word: {word}",
                    "Your message contains language that is not allowed in this workspace.",
                    isRuleBased: true);
        }

        // 5. Company-specific blocked keywords (substring, case-insensitive)
        var blockedKeywords = settings.GetSection("BlockedKeywords").Get<string[]>() ?? Array.Empty<string>();
        foreach (var keyword in blockedKeywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return ModerationResult.Block(
                    "CONFIDENTIAL_INFORMATION", $"Matched blocked keyword: {keyword}",
                    "Your message contains restricted content that cannot be shared in this channel.",
                    isRuleBased: true);
        }

        // 6. Email addresses
        if (settings.GetValue<bool>("BlockEmailAddresses", true) && EmailRegex.IsMatch(content))
            return ModerationResult.Block(
                "PERSONAL_INFORMATION", "Email address detected",
                "Sharing email addresses is not allowed. Please use approved communication channels.",
                isRuleBased: true);

        // 7. Phone numbers (Indian mobile numbers)
        if (settings.GetValue<bool>("BlockPhoneNumbers", true) && PhoneRegex.IsMatch(content))
            return ModerationResult.Block(
                "PERSONAL_INFORMATION", "Phone number detected",
                "Sharing phone numbers is not permitted in this workspace chat.",
                isRuleBased: true);

        // 8. Aadhaar / PAN (Indian government IDs)
        if (settings.GetValue<bool>("BlockAadhaarPan", true))
        {
            if (AadhaarRegex.IsMatch(content))
                return ModerationResult.Block(
                    "PERSONAL_INFORMATION", "Aadhaar number pattern detected",
                    "Sharing government identification numbers is strictly prohibited.",
                    isRuleBased: true);

            if (PanRegex.IsMatch(content))
                return ModerationResult.Block(
                    "PERSONAL_INFORMATION", "PAN number pattern detected",
                    "Sharing government identification numbers is strictly prohibited.",
                    isRuleBased: true);
        }

        // 9. URLs — disabled by default (BlockUrls: false); let Gemini judge context
        if (settings.GetValue<bool>("BlockUrls", false) && Regex.IsMatch(content, @"https?://\S+", RegexOptions.IgnoreCase))
            return ModerationResult.Block(
                "SPAM", "URL detected",
                "Sharing links is not allowed in this channel.",
                isRuleBased: true);

        return null; // All rules passed — escalate to Gemini
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stage 2 — Gemini AI classification
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ModerationResult> CallGeminiAsync(ModerationRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var requestBody = new { content = request.Content };
            var httpResponse = await client.PostAsJsonAsync("/api/gemini-moderation/moderate", requestBody);
            
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Moderation:Gemini] AuthService returned non-success HTTP {Status}. Failing open.", (int)httpResponse.StatusCode);
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
                _logger.LogInformation("[Moderation:Gemini] SAFE. User={User} Room={Room} Confidence={Confidence:F2}", request.AnonymousName, request.RoomName, confidence);
                return ModerationResult.Allow();
            }

            _logger.LogWarning("[Moderation:Gemini] Blocked. User={User} Room={Room} Category={Category} Confidence={Confidence:F2} Explanation={Explanation}", request.AnonymousName, request.RoomName, category, confidence, explanation);
            return ModerationResult.Block(category, explanation, BuildUserFriendlyReason(category), confidence);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[Moderation:Gemini] Request timed out. Failing open.");
            return ModerationResult.Allow();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Moderation:Gemini] Unexpected error. Failing open.");
            return ModerationResult.Allow();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Audit log persistence
    // ─────────────────────────────────────────────────────────────────────────

    private async Task SaveAuditLogAsync(ModerationRequest request, ModerationResult result)
    {
        try
        {
            var snippet = request.Content.Length <= 200
                ? request.Content
                : request.Content[..200];

            var log = new ModerationAuditLog
            {
                UserId             = request.UserId,
                AnonymousName      = request.AnonymousName,
                RoomId             = request.RoomId,
                RoomName           = request.RoomName,
                MessageSnippet     = snippet,
                Category           = result.Category,
                Confidence         = result.Confidence,
                WasAllowed         = result.AllowMessage,
                WasRuleBasedBlock  = result.IsRuleBasedBlock,
                Explanation        = result.Explanation,
                Timestamp          = DateTime.UtcNow
            };

            _context.ModerationAuditLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogDebug(
                "[Moderation:Audit] Saved. Id={Id} Allowed={Allowed} Category={Category}",
                log.Id, log.WasAllowed, log.Category);
        }
        catch (Exception ex)
        {
            // Audit log failure must NEVER affect the moderation decision itself
            _logger.LogWarning(ex, "[Moderation:Audit] Failed to save audit log entry. Decision is unaffected.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // User-facing reason map (internal category → safe UI message)
    // ─────────────────────────────────────────────────────────────────────────

    private static string BuildUserFriendlyReason(string category) => category switch
    {
        "TOXIC"                    => "Your message was blocked because it contains toxic content that violates workplace communication guidelines.",
        "HARASSMENT"               => "Your message was blocked because it appears to contain harassment. Please maintain respectful communication.",
        "HATE_SPEECH"              => "Your message was blocked because it contains language that is not tolerated in this workspace.",
        "PROFANITY"                => "Your message was blocked because it contains language that is inappropriate for a professional environment.",
        "SPAM"                     => "Your message was blocked because it appears to be spam. Please send meaningful messages.",
        "CONFIDENTIAL_INFORMATION" => "Your message was blocked because it may contain confidential company information.",
        "PERSONAL_INFORMATION"     => "Your message was blocked because it appears to contain personal or sensitive information.",
        "THREAT"                   => "Your message was blocked because it contains content that may be interpreted as a threat.",
        _                          => "Your message was blocked because it violates workplace communication guidelines."
    };
}
