using MongoDB.Bson.Serialization.Attributes;

namespace Chat.Domain.Documents;

/// <summary>
/// Collection "moderationEvents" — one row per moderation decision, allowed or blocked.
///
/// Replaces ModerationAuditLogs. Kept as its own collection (not embedded on the
/// message) because a blocked message is never stored, so most events have no
/// message to attach to.
/// </summary>
public sealed class ModerationEventDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public string AnonymousName { get; set; } = string.Empty;

    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    /// <summary>First 200 characters only. Never the whole message.</summary>
    public string Snippet { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }

    public bool WasAllowed { get; set; }

    /// <summary>"Rules", "Gemini", or "Unavailable".</summary>
    public string Engine { get; set; } = string.Empty;

    public string? MatchedRule { get; set; }
    public string Explanation { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Collection "files" — upload metadata.
///
/// New. Uploads previously had no record at all: FileController wrote a file to disk
/// and returned a URL that nothing served, with no owner, no size, and no way to
/// authorize a download.
/// </summary>
public sealed class FileDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Name as supplied by the client, sanitised. Display only.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Server-generated name on disk. Never derived from client input.</summary>
    public string StoredName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>Uploader. Downloads are authorized against room membership.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Set once the file is attached to a message.</summary>
    public Guid? RoomId { get; set; }
    public Guid? MessageId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>SHA-256 of the content, for dedup and integrity.</summary>
    public string Sha256 { get; set; } = string.Empty;
}

/// <summary>
/// Collection "presence" — who is connected, per room.
///
/// Replaces a static ConcurrentDictionary that was process-local, lost on restart,
/// and returned one flat global list of names for every room. Stored so presence
/// survives a restart and so a second instance sees the same picture.
/// </summary>
public sealed class PresenceDocument
{
    /// <summary>The SignalR connection id.</summary>
    [BsonId]
    public string ConnectionId { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public string AnonymousName { get; set; } = string.Empty;

    /// <summary>Rooms this connection has joined.</summary>
    public List<Guid> RoomIds { get; set; } = [];

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Refreshed on activity. A TTL index on this field reaps connections that were
    /// lost without a clean disconnect, which the in-memory tracker leaked forever.
    /// </summary>
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
