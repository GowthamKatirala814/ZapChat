using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Shared.Moderation;

public class RuleBasedModerationService : IRuleBasedModerationService
{
    private readonly ILogger<RuleBasedModerationService> _logger;
    private readonly HashSet<string> _profanity;
    private readonly HashSet<string> _threatWords;
    private readonly HashSet<string> _bullyingWords;
    private readonly HashSet<string> _confidentialKeywords;
    private readonly HashSet<string> _spamPatterns;

    // Pattern Detection Regexes
    private static readonly Regex JwtRegex = new(@"eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"(\+91[\-\s]?)?[6-9]\d{9}", RegexOptions.Compiled);
    private static readonly Regex AadhaarRegex = new(@"\b\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex PanRegex = new(@"\b[A-Z]{5}[0-9]{4}[A-Z]\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled); // Simple CCN regex
    private static readonly Regex SqlConnectionStringRegex = new(@"Server=.*;Database=.*;User Id=.*;Password=.*;", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BearerTokenRegex = new(@"Bearer\s+[A-Za-z0-9\-\._~\+\/]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex GoogleApiKeyRegex = new(@"AIza[0-9A-Za-z-_]{35}", RegexOptions.Compiled);
    private static readonly Regex InternalUrlRegex = new(@"https?:\/\/(?:internal\.|admin\.|corp\.|secret\.).+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PromptInjectionRegex = new(@"(?i)(ignore previous instructions|system prompt|bypass rules)", RegexOptions.Compiled);

    public RuleBasedModerationService(ILogger<RuleBasedModerationService> logger, string dictionariesPath = "")
    {
        _logger = logger;
        var loader = new ModerationDictionaryLoader(dictionariesPath);
        _profanity = loader.LoadDictionary("Profanity.txt");
        _threatWords = loader.LoadDictionary("ThreatWords.txt");
        _bullyingWords = loader.LoadDictionary("BullyingWords.txt");
        _confidentialKeywords = loader.LoadDictionary("ConfidentialKeywords.txt");
        _spamPatterns = loader.LoadDictionary("SpamPatterns.txt");
    }

    public Task<FallbackModerationResult> ModerateAsync(string content)
    {
        var matchedRules = new List<string>();
        var blockedWords = new List<string>();
        string category = "SAFE";
        string reason = string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(FallbackModerationResult.Allow());
        }

        var normalized = content.ToLowerInvariant();
        var words = normalized.Split(new[] { ' ', '.', ',', '!', '?', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // 1. Threat / Violence
        foreach (var word in words)
        {
            if (_threatWords.Contains(word))
            {
                matchedRules.Add("ThreatWords");
                blockedWords.Add(word);
                category = "THREAT";
                reason = "Your message was blocked because it contains content interpreted as a threat.";
            }
        }

        // 2. Bullying / Harassment
        if (category == "SAFE")
        {
            foreach (var word in words)
            {
                if (_bullyingWords.Contains(word))
                {
                    matchedRules.Add("BullyingWords");
                    blockedWords.Add(word);
                    category = "HARASSMENT";
                    reason = "Your message was blocked because it appears to contain harassment.";
                }
            }
        }

        // 3. Profanity / Hate Speech
        if (category == "SAFE")
        {
            foreach (var word in words)
            {
                if (_profanity.Contains(word))
                {
                    matchedRules.Add("Profanity");
                    blockedWords.Add(word);
                    category = "PROFANITY";
                    reason = "Your message was blocked because it contains inappropriate language.";
                }
            }
        }

        // 4. Confidential Keywords
        if (category == "SAFE")
        {
            foreach (var key in _confidentialKeywords)
            {
                if (normalized.Contains(key))
                {
                    matchedRules.Add("ConfidentialKeywords");
                    category = "CONFIDENTIAL_INFORMATION";
                    reason = "Your message was blocked because it may contain confidential information.";
                }
            }
        }

        // 5. Spam Patterns
        if (category == "SAFE")
        {
            foreach (var spam in _spamPatterns)
            {
                if (normalized.Contains(spam))
                {
                    matchedRules.Add("SpamPatterns");
                    category = "SPAM";
                    reason = "Your message was blocked because it appears to be spam.";
                }
            }
        }

        // 6. Pattern Detections
        if (category == "SAFE")
        {
            if (JwtRegex.IsMatch(content))
            {
                matchedRules.Add("JWT");
                category = "CONFIDENTIAL_INFORMATION";
            }
            else if (BearerTokenRegex.IsMatch(content))
            {
                matchedRules.Add("BearerToken");
                category = "CONFIDENTIAL_INFORMATION";
            }
            else if (GoogleApiKeyRegex.IsMatch(content))
            {
                matchedRules.Add("GoogleApiKey");
                category = "CONFIDENTIAL_INFORMATION";
            }
            else if (SqlConnectionStringRegex.IsMatch(content))
            {
                matchedRules.Add("SqlConnectionString");
                category = "CONFIDENTIAL_INFORMATION";
            }
            else if (InternalUrlRegex.IsMatch(content))
            {
                matchedRules.Add("InternalUrl");
                category = "CONFIDENTIAL_INFORMATION";
            }
            else if (PromptInjectionRegex.IsMatch(content))
            {
                matchedRules.Add("PromptInjection");
                category = "PROMPT_INJECTION";
            }
            else if (CreditCardRegex.IsMatch(content))
            {
                matchedRules.Add("CreditCard");
                category = "PERSONAL_INFORMATION";
            }
            else if (AadhaarRegex.IsMatch(content))
            {
                matchedRules.Add("Aadhaar");
                category = "PERSONAL_INFORMATION";
            }
            else if (PanRegex.IsMatch(content))
            {
                matchedRules.Add("Pan");
                category = "PERSONAL_INFORMATION";
            }
            else if (PhoneRegex.IsMatch(content))
            {
                matchedRules.Add("Phone");
                category = "PERSONAL_INFORMATION";
            }
            else if (EmailRegex.IsMatch(content))
            {
                matchedRules.Add("Email");
                category = "PERSONAL_INFORMATION";
            }

            if (category != "SAFE")
            {
                reason = category == "PERSONAL_INFORMATION" ? "Your message was blocked because it appears to contain personal information."
                       : category == "PROMPT_INJECTION" ? "Your message was blocked because it contains restricted commands."
                       : "Your message was blocked because it may contain confidential information.";
            }
        }

        if (category != "SAFE")
        {
            _logger.LogInformation("[RuleBasedModerationService] Blocked content. Category={Category}, MatchedRules={Rules}", category, string.Join(",", matchedRules));
            return Task.FromResult(FallbackModerationResult.Block(category, reason, 1.0, matchedRules, blockedWords));
        }

        return Task.FromResult(FallbackModerationResult.Allow());
    }
}
