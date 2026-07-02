namespace Chat.Domain.Entities;

/// <summary>
/// Audit record written every time the content moderation service evaluates a message
/// (only on a cache miss — repeated identical content is not double-counted).
///
/// Designed so the Admin Dashboard can later surface:
///   - A timeline of blocked / allowed events
///   - Per-user or per-room moderation history
///   - Category distribution analytics
///   - Confidence trend over time
/// </summary>
public class ModerationAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Who sent the message ──────────────────────────────────────────────────

    /// <summary>JWT sub claim (null if extraction failed).</summary>
    public string? UserId { get; set; }

    /// <summary>The anonymous display name shown inside the chat room.</summary>
    public string AnonymousName { get; set; } = string.Empty;

    // ── Where the message was sent ────────────────────────────────────────────

    /// <summary>Foreign-key-style reference to ChatRooms.Id (no hard FK to avoid migration complexity).</summary>
    public Guid RoomId { get; set; }

    /// <summary>Denormalised room name for fast querying without a join.</summary>
    public string RoomName { get; set; } = string.Empty;

    // ── What the message contained ────────────────────────────────────────────

    /// <summary>
    /// First 200 characters of the message for audit review.
    /// Truncated to avoid storing large blobs in the audit table.
    /// </summary>
    public string MessageSnippet { get; set; } = string.Empty;

    // ── Moderation decision ───────────────────────────────────────────────────

    /// <summary>
    /// One of: SAFE | TOXIC | HARASSMENT | HATE_SPEECH | PROFANITY | SPAM |
    ///         CONFIDENTIAL_INFORMATION | PERSONAL_INFORMATION | THREAT | OTHER
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gemini confidence score (0.0–1.0). Rule-based blocks always report 1.0.</summary>
    public double Confidence { get; set; }

    /// <summary>True = message was delivered; false = message was silently dropped.</summary>
    public bool WasAllowed { get; set; }

    /// <summary>True = blocked by the local rule engine (Gemini was not called).</summary>
    public bool WasRuleBasedBlock { get; set; }

    /// <summary>
    /// Internal explanation from Gemini or the rule engine.
    /// Never exposed to end-users.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    // ── Timing ────────────────────────────────────────────────────────────────

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
