using System.ComponentModel.DataAnnotations;
using Poll.Domain.Documents;

namespace Poll.Application;

// ── DTOs ────────────────────────────────────────────────────────────────────────

public sealed record PollDto(
    Guid Id,
    string Question,
    string CreatorName,
    bool IsMine,
    IReadOnlyList<PollOptionDto> Options,
    int TotalVotes,
    int Upvotes,
    int Downvotes,
    PollStatus Status,
    DateTime CreatedAt,
    /// <summary>The option this caller chose, if any.</summary>
    Guid? MyVoteOptionId,
    /// <summary>This caller's reaction: true up, false down, null none.</summary>
    bool? MyReaction);

public sealed record PollOptionDto(Guid Id, string Text, int VoteCount, double Percentage);

// ── Requests ────────────────────────────────────────────────────────────────────

/// <summary>
/// Creating a poll. There is no creatorId field — identity comes from the token. The
/// old endpoint accepted it in the body with no authorization at all.
/// </summary>
public sealed class CreatePollRequest
{
    [Required, StringLength(300, MinimumLength = 5)]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Server-enforced bounds. Previously nothing was validated: an empty question,
    /// zero options, or a thousand options were all accepted, and a null Options list
    /// threw a NullReferenceException.
    /// </summary>
    [Required, MinLength(2, ErrorMessage = "A poll needs at least 2 options.")]
    [MaxLength(10, ErrorMessage = "A poll can have at most 10 options.")]
    public List<string> Options { get; set; } = [];
}

public sealed class VoteRequest
{
    /// <summary>The chosen option, or null to withdraw the caller's vote.</summary>
    public Guid? OptionId { get; set; }
}

public sealed class ReactRequest
{
    /// <summary>true up, false down, null to clear.</summary>
    public bool? IsUpvote { get; set; }
}

// ── Abstractions ────────────────────────────────────────────────────────────────

public interface IPollRepository
{
    Task<PollDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<PollDocument>> ListAsync(
        bool includeRemoved, int limit, CancellationToken ct = default);

    Task InsertAsync(PollDocument poll, CancellationToken ct = default);

    /// <summary>Atomically adjusts one option's counter and the poll's total.</summary>
    Task AdjustVoteAsync(
        Guid pollId, Guid? incrementOptionId, Guid? decrementOptionId, int totalDelta,
        CancellationToken ct = default);

    Task AdjustReactionsAsync(
        Guid pollId, int upDelta, int downDelta, CancellationToken ct = default);

    Task<bool> CloseAsync(Guid pollId, Guid closedBy, CancellationToken ct = default);
    Task<bool> ReopenAsync(Guid pollId, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid pollId, Guid removedBy, CancellationToken ct = default);

    Task<long> CountAsync(CancellationToken ct = default);

    Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default);

    Task<IReadOnlyList<PollDocument>> TopByVotesAsync(int top, CancellationToken ct = default);
}

public interface IPollVoteRepository
{
    Task<PollVoteDocument?> GetAsync(Guid pollId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<PollVoteDocument>> GetForUserAsync(
        Guid userId, IReadOnlyCollection<Guid> pollIds, CancellationToken ct = default);

    /// <summary>Inserts a first vote. False when the user has already voted.</summary>
    Task<bool> TryInsertAsync(PollVoteDocument vote, CancellationToken ct = default);

    Task<bool> ChangeOptionAsync(
        Guid pollId, Guid userId, Guid optionId, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid pollId, Guid userId, CancellationToken ct = default);

    Task<long> CountDistinctVotersAsync(Guid pollId, CancellationToken ct = default);
}

public interface IPollReactionRepository
{
    Task<PollReactionDocument?> GetAsync(Guid pollId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<PollReactionDocument>> GetForUserAsync(
        Guid userId, IReadOnlyCollection<Guid> pollIds, CancellationToken ct = default);

    Task<bool> TryInsertAsync(PollReactionDocument reaction, CancellationToken ct = default);
    Task<bool> FlipAsync(Guid pollId, Guid userId, bool isUpvote, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid pollId, Guid userId, CancellationToken ct = default);
}

public interface IPollBroadcaster
{
    Task PollCreatedAsync(PollDto poll);
    Task PollUpdatedAsync(PollDto poll);
    Task PollClosedAsync(Guid pollId);
    Task PollRemovedAsync(Guid pollId);
}

public interface IPollService
{
    Task<IReadOnlyList<PollDto>> ListAsync(int limit, CancellationToken ct = default);
    Task<PollDto> GetAsync(Guid pollId, CancellationToken ct = default);
    Task<PollDto> CreateAsync(CreatePollRequest request, CancellationToken ct = default);
    Task<PollDto> VoteAsync(Guid pollId, VoteRequest request, CancellationToken ct = default);
    Task<PollDto> ReactAsync(Guid pollId, ReactRequest request, CancellationToken ct = default);

    /// <summary>Creator or admin.</summary>
    Task CloseAsync(Guid pollId, CancellationToken ct = default);

    /// <summary>Admin only.</summary>
    Task RemoveAsync(Guid pollId, CancellationToken ct = default);
}
