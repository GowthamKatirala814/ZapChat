using MongoDB.Bson.Serialization.Attributes;

namespace Admin.Domain.Documents;

/// <summary>Which service owns the reported message.</summary>
public enum ReportTargetKind
{
    RoomMessage = 0,
    DirectMessage = 1
}

public enum ReportStatus
{
    Pending = 0,

    /// <summary>Reviewed and the content was removed.</summary>
    Actioned = 1,

    /// <summary>Reviewed and judged acceptable.</summary>
    Dismissed = 2,

    /// <summary>Resolved by the automated threshold rule.</summary>
    AutoActioned = 3
}

/// <summary>
/// Collection "reports".
///
/// The target is embedded as a snapshot taken at report time, so the moderation queue
/// still shows what was reported after the message is edited or removed. The old schema
/// stored the same fields flat and had three different creation paths that populated
/// them inconsistently.
/// </summary>
public sealed class ReportDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public ReportTarget Target { get; set; } = new();

    public Reporter ReportedBy { get; set; } = new();

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
}

public sealed class ReportTarget
{
    public ReportTargetKind Kind { get; set; }

    public Guid MessageId { get; set; }

    /// <summary>Content as it read when reported.</summary>
    public string ContentSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// The author's user id. Resolved once at report time; the threshold rule counts
    /// distinct reporters against this.
    /// </summary>
    public Guid AuthorUserId { get; set; }

    public string AuthorAnonymousName { get; set; } = string.Empty;

    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
}

public sealed class Reporter
{
    /// <summary>Always taken from the authenticated caller's token.</summary>
    public Guid UserId { get; set; }

    public string AnonymousName { get; set; } = string.Empty;
}

/// <summary>
/// Collection "auditLogs" — append-only.
///
/// Field naming is consistent end to end. The old entity used EntityType/EntityId while
/// its DTO and SQL script used TargetType/TargetId, and the provisioning script created
/// the wrong names then renamed them 123 lines later.
/// </summary>
public sealed class AuditLogDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Action { get; set; } = string.Empty;

    public AuditEntity Entity { get; set; } = new();

    public AuditActor Actor { get; set; } = new();

    /// <summary>Human-readable context, e.g. the reason given.</summary>
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEntity
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public sealed class AuditActor
{
    /// <summary>Guid.Empty means the automated moderator.</summary>
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsSystem => UserId == Guid.Empty;
}

/// <summary>
/// Collection "blockedUsers". Unique on the user id.
///
/// <see cref="EmailHash"/> is a SHA-256 of the account's address, fetched from Auth's
/// internal endpoint. The old flow tried to fetch the raw email over an unauthenticated
/// call that 401'd — and the endpoint did not return the address anyway — so the hash
/// was always a placeholder and re-registration was never actually blocked.
/// </summary>
public sealed class BlockedUserDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string AnonymousName { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of the normalised email. Never the address itself.</summary>
    public string EmailHash { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Guid.Empty for an automated block.</summary>
    public Guid BlockedBy { get; set; }

    /// <summary>"Manual" or "AutoModeration".</summary>
    public string Source { get; set; } = "Manual";
}

/// <summary>
/// Collection "settings" — one document, _id "moderation".
///
/// Using a fixed string key makes the singleton actually singular. The old table had a
/// Guid primary key, so nothing prevented a second settings row.
/// </summary>
public sealed class ModerationSettingsDocument
{
    public const string SingletonId = "moderation";

    [BsonId]
    public string Id { get; set; } = SingletonId;

    /// <summary>Distinct reporters against one author before an automatic action.</summary>
    public int ReportThreshold { get; set; } = 5;

    /// <summary>When false, the threshold rule only flags and never acts.</summary>
    public bool AutoActionEnabled { get; set; } = true;

    /// <summary>Whether an automatic action also removes the author's messages.</summary>
    public bool AutoRemoveMessages { get; set; } = true;

    /// <summary>Whether an automatic action disables the account.</summary>
    public bool AutoDisableAccount { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
}
