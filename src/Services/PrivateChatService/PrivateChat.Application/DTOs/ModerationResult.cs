namespace PrivateChat.Application.DTOs;

/// <summary>
/// Encapsulates the decision made by the content moderation service.
/// </summary>
public class ModerationResult
{
    /// <summary>True if the message should be delivered; false if it should be blocked.</summary>
    public bool AllowMessage { get; set; }

    /// <summary>
    /// The primary category assigned (e.g. SAFE, TOXIC, SPAM).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Internal explanation from Gemini or the rule engine.</summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>User-facing reason that can be safely displayed in the UI toast.</summary>
    public string BlockedReason { get; set; } = string.Empty;

    /// <summary>Confidence score of the decision (0.0 to 1.0).</summary>
    public double Confidence { get; set; }

    /// <summary>True if the message was blocked by local Regex/rules rather than Gemini.</summary>
    public bool IsRuleBasedBlock { get; set; }

    public static ModerationResult Allow() => new()
    {
        AllowMessage = true,
        Category     = "SAFE",
        Confidence   = 1.0
    };

    public static ModerationResult Block(string category, string explanation, string userFriendlyReason, double confidence = 1.0, bool isRuleBased = false) => new()
    {
        AllowMessage     = false,
        Category         = category,
        Explanation      = explanation,
        BlockedReason    = userFriendlyReason,
        Confidence       = confidence,
        IsRuleBasedBlock = isRuleBased
    };
}
