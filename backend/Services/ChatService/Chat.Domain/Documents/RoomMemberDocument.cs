using MongoDB.Bson.Serialization.Attributes;

namespace Chat.Domain.Documents;

/// <summary>
/// Collection "roomMembers" — membership and per-user read state in one document.
///
/// This collection is the fix for the worst functional defect in the old system.
/// Membership used to live in the Admin database, so ChatHub had to call
/// GET admin/api/admin/rooms/{id}/members on every single message. That call was
/// unauthenticated against an Admin-only endpoint, returned 401, and the exception
/// was swallowed — silently disabling unread badges, @mentions, read receipts and
/// room notifications all at once.
///
/// Two design choices remove the cross-service call entirely rather than just
/// authenticating it:
///
///  1. Membership lives in Chat, next to the messages that need it.
///  2. <see cref="AnonymousName"/> is denormalized here, so resolving an @mention
///     needs no call to Auth either.
///
/// It is a separate collection rather than an array on the room because membership
/// is unbounded and every message touches one member's unread count — embedding
/// would rewrite the whole room document per message.
/// </summary>
public sealed class RoomMemberDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Copy of the user's anonymous name. Refreshed opportunistically from the JWT
    /// claim whenever the member acts, so it cannot drift for an active user.
    /// </summary>
    public string AnonymousName { get; set; } = string.Empty;

    /// <summary>Maintained with $inc, never read-modify-write.</summary>
    public int UnreadCount { get; set; }

    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    /// <summary>Suppresses notifications without leaving the room.</summary>
    public bool IsMuted { get; set; }
}
