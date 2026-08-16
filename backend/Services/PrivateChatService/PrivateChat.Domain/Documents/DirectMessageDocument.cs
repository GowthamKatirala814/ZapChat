using MongoDB.Bson.Serialization.Attributes;

namespace PrivateChat.Domain.Documents;

public enum DeletionKind
{
    None = 0,
    User = 1,
    Moderation = 2
}

/// <summary>
/// Collection "directMessages". Same shape as a room message so the two services can
/// share one client-side renderer, with conversationId in place of roomId.
/// </summary>
public sealed class DirectMessageDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public MessageSender Sender { get; set; } = new();

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the recipient has seen it. Drives the read tick.</summary>
    public DateTime? ReadAt { get; set; }

    public ReplyReference? ReplyTo { get; set; }

    public List<MessageReaction> Reactions { get; set; } = [];

    public List<MessageAttachment> Attachments { get; set; } = [];

    public MessageState State { get; set; } = new();

    public ModerationStamp? Moderation { get; set; }

    public bool IsVisible => State.Deletion.Kind == DeletionKind.None;
}

public sealed class MessageSender
{
    public Guid UserId { get; set; }
    public string AnonymousName { get; set; } = string.Empty;
}

public sealed class ReplyReference
{
    public Guid MessageId { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
}

public sealed class MessageReaction
{
    public string Emoji { get; set; } = string.Empty;
    public List<Guid> UserIds { get; set; } = [];
    public List<string> Names { get; set; } = [];
}

public sealed class MessageAttachment
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Url { get; set; } = string.Empty;
}

public sealed class MessageState
{
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public Deletion Deletion { get; set; } = new();
}

public sealed class Deletion
{
    public DeletionKind Kind { get; set; } = DeletionKind.None;
    public DateTime? At { get; set; }
    public Guid? By { get; set; }
    public string? Reason { get; set; }
}

public sealed class ModerationStamp
{
    public string Engine { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? MatchedRule { get; set; }
}

/// <summary>Collection "moderationEvents" — private-chat moderation decisions.</summary>
public sealed class ModerationEventDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public string AnonymousName { get; set; } = string.Empty;

    public Guid ConversationId { get; set; }

    public string Snippet { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool WasAllowed { get; set; }
    public string Engine { get; set; } = string.Empty;
    public string? MatchedRule { get; set; }
    public string Explanation { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
