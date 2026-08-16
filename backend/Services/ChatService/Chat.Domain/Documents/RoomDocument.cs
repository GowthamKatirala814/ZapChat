using MongoDB.Bson.Serialization.Attributes;

namespace Chat.Domain.Documents;

/// <summary>
/// How a room decides who may read and post. Previously ChatRoom.RoomType was a
/// free string that was written and never read, so "HR Issues" and the branch
/// channels were ordinary public rooms with no access control at all.
/// </summary>
public enum RoomType
{
    /// <summary>Everybody.</summary>
    General = 0,

    /// <summary>Only users whose branch claim matches <see cref="RoomDocument.Branch"/>.</summary>
    Branch = 1,

    /// <summary>Everybody may post; only HR/Admin may moderate.</summary>
    Hr = 2,

    /// <summary>Explicit membership only.</summary>
    Custom = 3
}

/// <summary>
/// Collection "rooms".
///
/// Chat is the single owner of a room. Admin operates on rooms through Chat's
/// /api/chat-admin endpoints rather than holding its own copy, so there is exactly
/// one record of a room's name, type and archived state.
/// </summary>
public sealed class RoomDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Lower-cased name. Unique, and what lookups use, so a rename is safe.</summary>
    public string Slug { get; set; } = string.Empty;

    public RoomType Type { get; set; } = RoomType.General;

    /// <summary>Set only when <see cref="Type"/> is Branch.</summary>
    public string? Branch { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Denormalized so the sidebar renders from one read of this collection instead
    /// of a per-room message query.
    /// </summary>
    public LastMessageSummary? LastMessage { get; set; }

    /// <summary>Maintained with $inc. Real per-room counts, replacing hardcoded zeros.</summary>
    public int MemberCount { get; set; }

    public int MessageCount { get; set; }

    /// <summary>
    /// Soft archive. Chat used to hard-delete a room (cascading its messages) while
    /// Admin only soft-deleted its own copy.
    /// </summary>
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public Guid? ArchivedBy { get; set; }

    /// <summary>Rooms created at startup that must always exist.</summary>
    public bool IsSystemRoom { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static string ToSlug(string name) => name.Trim().ToLowerInvariant();
}

public sealed class LastMessageSummary
{
    public Guid MessageId { get; set; }
    public string Preview { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
