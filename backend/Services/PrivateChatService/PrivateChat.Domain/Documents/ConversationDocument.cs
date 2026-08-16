using MongoDB.Bson.Serialization.Attributes;

namespace PrivateChat.Domain.Documents;

/// <summary>
/// Collection "conversations".
///
/// Participants are embedded as a fixed pair, each carrying their own unread count
/// and read marker. That is the natural document shape: a conversation is never read
/// without its participants, and the pair is bounded at two.
///
/// <see cref="ParticipantKey"/> is a sorted "lowGuid:highGuid" string with a unique
/// index, so (A,B) and (B,A) resolve to one document and a duplicate conversation is
/// structurally impossible. The old code normalised the order in application logic on
/// every query instead.
/// </summary>
public sealed class ConversationDocument
{
    [BsonId]
    public Guid Id { get; set; }

    /// <summary>Sorted participant pair. Unique index.</summary>
    public string ParticipantKey { get; set; } = string.Empty;

    /// <summary>Exactly two entries.</summary>
    public List<Participant> Participants { get; set; } = [];

    public LastMessageSummary? LastMessage { get; set; }

    public int MessageCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string KeyFor(Guid a, Guid b)
    {
        var (low, high) = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        return $"{low}:{high}";
    }

    public Participant? ParticipantFor(Guid userId) =>
        Participants.FirstOrDefault(p => p.UserId == userId);

    public Participant? Other(Guid userId) =>
        Participants.FirstOrDefault(p => p.UserId != userId);

    /// <summary>Membership check. Every operation on a conversation goes through this.</summary>
    public bool Includes(Guid userId) => Participants.Any(p => p.UserId == userId);
}

public sealed class Participant
{
    public Guid UserId { get; set; }

    /// <summary>Denormalized so rendering a conversation needs no call to Auth.</summary>
    public string AnonymousName { get; set; } = string.Empty;

    public int UnreadCount { get; set; }
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
}

public sealed class LastMessageSummary
{
    public Guid MessageId { get; set; }
    public string Preview { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Collection "userBlocks". Unique on (blockerId, blockedId).
/// </summary>
public sealed class UserBlockDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BlockerId { get; set; }
    public Guid BlockedId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
