using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Documents;

/// <summary>
/// Collection "aiUsage" — one document per UTC day, keyed by "yyyy-MM-dd".
///
/// Replaces GeminiUsages + AiHealthEvents. Two design changes:
///   * The date string is the _id, so "the tracker for today" is a primary-key
///     lookup and duplicate-day rows are structurally impossible. The old schema
///     needed a separate unique index on Date to get the same guarantee.
///   * Health events are embedded rather than a second table. They are bounded
///     (a handful per day) and only ever read alongside that day's counters.
///
/// All counters are updated with $inc, never read-modify-write.
/// </summary>
public sealed class AiUsageDocument
{
    /// <summary>UTC date as "yyyy-MM-dd".</summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public int Requests { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }

    public int BlockedMessages { get; set; }
    public int SafeMessages { get; set; }

    public AiErrorCounters Errors { get; set; } = new();

    public string Status { get; set; } = "Healthy";

    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public string? LastErrorMessage { get; set; }

    public int EstimatedDailyQuota { get; set; } = 1500;

    /// <summary>Thresholds already notified, so an alert email is sent once.</summary>
    public List<int> NotifiedThresholds { get; set; } = [];

    /// <summary>Status transitions for this day.</summary>
    public List<AiHealthEvent> Events { get; set; } = [];

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static string KeyFor(DateTime utc) => utc.ToString("yyyy-MM-dd");
}

public sealed class AiErrorCounters
{
    public int RateLimited { get; set; }
    public int Timeouts { get; set; }
    public int Configuration { get; set; }
    public int Authentication { get; set; }
    public int Server { get; set; }
    public int InvalidResponse { get; set; }
}

public sealed class AiHealthEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
