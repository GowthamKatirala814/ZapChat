namespace Shared.Moderation;

public class FallbackModerationResult
{
    public bool AllowMessage { get; set; }
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> MatchedRules { get; set; } = new();
    public List<string> BlockedWords { get; set; } = new();
    public string EngineUsed { get; set; } = "FallbackRules";

    public static FallbackModerationResult Allow() => new()
    {
        AllowMessage = true,
        Category = "SAFE",
        Confidence = 1.0,
        EngineUsed = "FallbackRules"
    };

    public static FallbackModerationResult Block(string category, string reason, double confidence, List<string> matchedRules, List<string> blockedWords) => new()
    {
        AllowMessage = false,
        Category = category,
        Reason = reason,
        Confidence = confidence,
        MatchedRules = matchedRules,
        BlockedWords = blockedWords,
        EngineUsed = "FallbackRules"
    };
}
