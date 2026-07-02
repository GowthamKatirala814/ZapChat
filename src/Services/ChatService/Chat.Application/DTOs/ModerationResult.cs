namespace Chat.Application.DTOs;

/// <summary>
/// The structured result returned by the moderation service.
/// If AllowMessage is false the message must be dropped silently on the backend.
/// </summary>
public class ModerationResult
{
    /// <summary>True = message is safe and can be saved + broadcast.</summary>
    public bool AllowMessage { get; set; } = true;

    /// <summary>
    /// Classification category returned by Gemini (or the rule engine).
    /// One of: SAFE | TOXIC | HARASSMENT | HATE_SPEECH | PROFANITY | SPAM |
    ///         CONFIDENTIAL_INFORMATION | PERSONAL_INFORMATION | THREAT | OTHER
    /// </summary>
    public string Category { get; set; } = "SAFE";

    /// <summary>Confidence score (0.0–1.0) returned by Gemini. Rule-engine blocks use 1.0.</summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>Internal explanation from Gemini (never sent to the frontend).</summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly reason shown in the frontend toast.
    /// Never reveals the AI prompt or internal category name.
    /// </summary>
    public string BlockedReason { get; set; } = string.Empty;

    /// <summary>Indicates whether the decision came from the local rule engine (true) or Gemini (false).</summary>
    public bool IsRuleBasedBlock { get; set; } = false;

    // ── Factory helpers ───────────────────────────────────────────────────────

    public static ModerationResult Allow() =>
        new() { AllowMessage = true, Category = "SAFE" };

    public static ModerationResult Block(
        string category,
        string explanation,
        string blockedReason,
        double confidence = 1.0,
        bool isRuleBased = false) =>
        new()
        {
            AllowMessage = false,
            Category = category,
            Confidence = confidence,
            Explanation = explanation,
            BlockedReason = blockedReason,
            IsRuleBasedBlock = isRuleBased
        };
}
