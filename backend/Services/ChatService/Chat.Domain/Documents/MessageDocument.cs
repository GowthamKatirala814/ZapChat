using MongoDB.Bson.Serialization.Attributes;

namespace Chat.Domain.Documents;

/// <summary>Who removed a message. The UI must distinguish these two cases.</summary>
public enum DeletionKind
{
    None = 0,

    /// <summary>The author deleted their own message.</summary>
    User = 1,

    /// <summary>An admin or the automated moderator removed it.</summary>
    Moderation = 2
}

/// <summary>
/// Collection "messages".
///
/// The single most important change: <see cref="MessageAuthor.UserId"/> exists.
/// The old Chat.Domain.Message had no user id at all — only AnonymousName — so
/// ownership checks compared strings, report attribution needed a name lookup
/// against Auth, and GET /api/messages/{id} returned senderId = Guid.Empty
/// unconditionally.
///
/// Reactions and attachments are embedded because they are bounded and always read
/// with the message; embedding also makes a reaction toggle a single atomic update.
/// </summary>
public sealed class MessageDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public MessageAuthor Author { get; set; } = new();

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reply target with a snippet copied in, so rendering a thread needs no second
    /// query and a later edit of the parent does not rewrite history.
    /// </summary>
    public ReplyReference? ReplyTo { get; set; }

    /// <summary>One entry per distinct emoji. Toggled with $addToSet / $pull.</summary>
    public List<MessageReaction> Reactions { get; set; } = [];

    public List<MessageAttachment> Attachments { get; set; } = [];

    public MessageState State { get; set; } = new();

    /// <summary>Set when the moderation pipeline had something to say.</summary>
    public ModerationStamp? Moderation { get; set; }

    /// <summary>Anonymous names extracted from @mentions, resolved at send time.</summary>
    public List<string> Mentions { get; set; } = [];

    public bool IsVisible => State.Deletion.Kind == DeletionKind.None;
}

public sealed class MessageAuthor
{
    /// <summary>The authenticated user. Never sent to other clients.</summary>
    public Guid UserId { get; set; }

    /// <summary>The identity other users see.</summary>
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

    /// <summary>Who reacted. Drives the atomic toggle and the "you reacted" flag.</summary>
    public List<Guid> UserIds { get; set; } = [];

    /// <summary>Anonymous names, so a tooltip needs no lookup.</summary>
    public List<string> Names { get; set; } = [];

    public int Count => UserIds.Count;
}

public sealed class MessageAttachment
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>Relative API path. Resolved against the gateway by the client.</summary>
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

    /// <summary>Who performed it. Never exposed for moderation deletions.</summary>
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
