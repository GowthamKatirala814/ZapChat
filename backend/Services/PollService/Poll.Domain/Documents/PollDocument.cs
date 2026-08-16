using MongoDB.Bson.Serialization.Attributes;

namespace Poll.Domain.Documents;

public enum PollStatus
{
    Open = 0,

    /// <summary>Closed by its creator or an admin. Results visible, voting refused.</summary>
    Closed = 1,

    /// <summary>Removed by an admin. Hidden from everyone.</summary>
    Removed = 2
}

/// <summary>
/// Collection "polls".
///
/// Options are embedded: they are bounded (2–10), meaningless apart from the poll, and
/// always read with it. That also means the vote counter lives in the same document as
/// the poll, so a vote is one atomic update.
///
/// Votes and reactions are NOT embedded — those are unbounded and are queried by user,
/// so they get their own collections with unique indexes.
/// </summary>
public sealed class PollDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public string Question { get; set; } = string.Empty;

    public List<PollOption> Options { get; set; } = [];

    public Guid CreatorId { get; set; }

    /// <summary>Creator's anonymous name. A poll author is as anonymous as a message author.</summary>
    public string CreatorName { get; set; } = string.Empty;

    /// <summary>Aggregate reaction totals, kept with $inc.</summary>
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }

    /// <summary>Distinct voters, kept with $inc. Avoids a count query per poll.</summary>
    public int TotalVotes { get; set; }

    public PollStatus Status { get; set; } = PollStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }

    public bool AcceptsVotes => Status == PollStatus.Open;
}

public sealed class PollOption
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>Updated only through $inc, never read-modify-write.</summary>
    public int VoteCount { get; set; }
}

/// <summary>
/// Collection "pollVotes". Unique on (pollId, userId), which is the constraint the old
/// schema lacked entirely — one vote per user is now enforced by the database rather
/// than by a check the caller could sidestep by sending a different userId.
/// </summary>
public sealed class PollVoteDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PollId { get; set; }
    public Guid UserId { get; set; }
    public Guid OptionId { get; set; }

    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Collection "pollReactions". Unique on (pollId, userId).</summary>
public sealed class PollReactionDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PollId { get; set; }
    public Guid UserId { get; set; }

    public bool IsUpvote { get; set; }

    public DateTime ReactedAt { get; set; } = DateTime.UtcNow;
}
