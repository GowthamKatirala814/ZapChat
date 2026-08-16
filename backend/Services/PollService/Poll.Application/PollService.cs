using Microsoft.Extensions.Logging;
using Poll.Domain.Documents;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Poll.Application;

public sealed class PollService : IPollService
{
    private readonly IPollRepository _polls;
    private readonly IPollVoteRepository _votes;
    private readonly IPollReactionRepository _reactions;
    private readonly IPollBroadcaster _broadcaster;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PollService> _logger;

    public PollService(
        IPollRepository polls,
        IPollVoteRepository votes,
        IPollReactionRepository reactions,
        IPollBroadcaster broadcaster,
        ICurrentUser currentUser,
        ILogger<PollService> logger)
    {
        _polls = polls;
        _votes = votes;
        _reactions = reactions;
        _broadcaster = broadcaster;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PollDto>> ListAsync(int limit, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();
        var polls = await _polls.ListAsync(includeRemoved: _currentUser.IsAdmin, limit, ct);

        if (polls.Count == 0) return [];

        var ids = polls.Select(p => p.Id).ToList();

        // Two batch queries for the caller's own votes and reactions across every poll,
        // rather than a pair of queries per poll.
        var myVotes = (await _votes.GetForUserAsync(userId, ids, ct))
            .ToDictionary(v => v.PollId, v => v.OptionId);

        var myReactions = (await _reactions.GetForUserAsync(userId, ids, ct))
            .ToDictionary(r => r.PollId, r => r.IsUpvote);

        return polls.Select(p => ToDto(p, userId, myVotes, myReactions)).ToList();
    }

    public async Task<PollDto> GetAsync(Guid pollId, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();
        var poll = await RequireVisibleAsync(pollId, ct);

        var vote = await _votes.GetAsync(pollId, userId, ct);
        var reaction = await _reactions.GetAsync(pollId, userId, ct);

        return ToDto(poll, userId,
            vote is null ? [] : new() { [pollId] = vote.OptionId },
            reaction is null ? [] : new() { [pollId] = reaction.IsUpvote });
    }

    public async Task<PollDto> CreateAsync(
        CreatePollRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        var options = request.Options
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();

        if (options.Count < 2)
            throw new ValidationException("A poll needs at least 2 non-empty options.");

        // Duplicate options make results meaningless.
        if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
            throw new ValidationException("Poll options must be distinct.");

        if (options.Any(o => o.Length > 200))
            throw new ValidationException("Each option must be 200 characters or fewer.");

        var poll = new PollDocument
        {
            Id = Guid.NewGuid(),
            Question = request.Question.Trim(),
            CreatorId = userId,
            CreatorName = _currentUser.AnonymousName,
            Options = options
                .Select(text => new PollOption { Id = Guid.NewGuid(), Text = text })
                .ToList(),
            CreatedAt = DateTime.UtcNow
        };

        await _polls.InsertAsync(poll, ct);

        var dto = ToDto(poll, userId, [], []);

        // The broadcast goes to every connected client, so it must be viewer-neutral —
        // exactly as PollUpdatedAsync already is. Sending the creator's DTO would give
        // every recipient IsMine=true and a "close poll" control on someone else's poll.
        await _broadcaster.PollCreatedAsync(ToDto(poll, Guid.Empty, [], []));

        _logger.LogInformation("User {UserId} created poll {PollId}.", userId, poll.Id);

        return dto;
    }

    /// <summary>
    /// Casts, changes, or withdraws the caller's vote.
    ///
    /// The voter is the authenticated caller. The old endpoint took UserId from the
    /// request body with no authorization, so any GUID could stuff the ballot and the
    /// "one vote per user" rule was unenforceable.
    ///
    /// Every counter change is an $inc, so concurrent votes cannot lose updates the way
    /// the old read-modify-write path did.
    /// </summary>
    public async Task<PollDto> VoteAsync(
        Guid pollId, VoteRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();
        var poll = await RequireVisibleAsync(pollId, ct);

        if (!poll.AcceptsVotes)
            throw new ConflictException("This poll is closed.");

        if (request.OptionId is { } optionId &&
            poll.Options.All(o => o.Id != optionId))
        {
            throw new ValidationException("That option does not belong to this poll.");
        }

        var existing = await _votes.GetAsync(pollId, userId, ct);

        if (request.OptionId is null)
        {
            // Withdraw.
            if (existing is not null && await _votes.DeleteAsync(pollId, userId, ct))
            {
                await _polls.AdjustVoteAsync(
                    pollId, incrementOptionId: null,
                    decrementOptionId: existing.OptionId, totalDelta: -1, ct);
            }
        }
        else if (existing is null)
        {
            // First vote. The unique index on (pollId, userId) is what actually
            // prevents a double vote under concurrency; a false return means another
            // request won the race.
            var inserted = await _votes.TryInsertAsync(new PollVoteDocument
            {
                PollId = pollId,
                UserId = userId,
                OptionId = request.OptionId.Value
            }, ct);

            if (inserted)
            {
                await _polls.AdjustVoteAsync(
                    pollId, incrementOptionId: request.OptionId.Value,
                    decrementOptionId: null, totalDelta: +1, ct);
            }
        }
        else if (existing.OptionId == request.OptionId.Value)
        {
            // Clicking the same option again withdraws — matches the existing UI.
            if (await _votes.DeleteAsync(pollId, userId, ct))
            {
                await _polls.AdjustVoteAsync(
                    pollId, incrementOptionId: null,
                    decrementOptionId: existing.OptionId, totalDelta: -1, ct);
            }
        }
        else
        {
            // Change of mind: move one vote between options, total unchanged.
            if (await _votes.ChangeOptionAsync(pollId, userId, request.OptionId.Value, ct))
            {
                await _polls.AdjustVoteAsync(
                    pollId, incrementOptionId: request.OptionId.Value,
                    decrementOptionId: existing.OptionId, totalDelta: 0, ct);
            }
        }

        return await PublishAsync(pollId, userId, ct);
    }

    public async Task<PollDto> ReactAsync(
        Guid pollId, ReactRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();
        await RequireVisibleAsync(pollId, ct);

        var existing = await _reactions.GetAsync(pollId, userId, ct);

        if (request.IsUpvote is null)
        {
            if (existing is not null && await _reactions.DeleteAsync(pollId, userId, ct))
            {
                await _polls.AdjustReactionsAsync(
                    pollId, existing.IsUpvote ? -1 : 0, existing.IsUpvote ? 0 : -1, ct);
            }
        }
        else if (existing is null)
        {
            var inserted = await _reactions.TryInsertAsync(new PollReactionDocument
            {
                PollId = pollId, UserId = userId, IsUpvote = request.IsUpvote.Value
            }, ct);

            if (inserted)
            {
                await _polls.AdjustReactionsAsync(
                    pollId, request.IsUpvote.Value ? +1 : 0, request.IsUpvote.Value ? 0 : +1, ct);
            }
        }
        else if (existing.IsUpvote == request.IsUpvote.Value)
        {
            if (await _reactions.DeleteAsync(pollId, userId, ct))
            {
                await _polls.AdjustReactionsAsync(
                    pollId, existing.IsUpvote ? -1 : 0, existing.IsUpvote ? 0 : -1, ct);
            }
        }
        else
        {
            if (await _reactions.FlipAsync(pollId, userId, request.IsUpvote.Value, ct))
            {
                await _polls.AdjustReactionsAsync(
                    pollId,
                    request.IsUpvote.Value ? +1 : -1,
                    request.IsUpvote.Value ? -1 : +1, ct);
            }
        }

        return await PublishAsync(pollId, userId, ct);
    }

    /// <summary>New in this migration: a poll can be closed. Creator or admin.</summary>
    public async Task CloseAsync(Guid pollId, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();
        var poll = await RequireVisibleAsync(pollId, ct);

        if (poll.CreatorId != userId && !_currentUser.IsAdmin)
            throw new ForbiddenException("Only the poll's author or an administrator can close it.");

        if (!await _polls.CloseAsync(pollId, userId, ct))
            throw new ConflictException("That poll is already closed.");

        await _broadcaster.PollClosedAsync(pollId);
    }

    /// <summary>New in this migration: admin removal. There was no way to delete a poll.</summary>
    public async Task RemoveAsync(Guid pollId, CancellationToken ct = default)
    {
        if (!_currentUser.IsAdmin)
            throw new ForbiddenException("Only an administrator can remove a poll.");

        if (!await _polls.RemoveAsync(pollId, _currentUser.RequireUserId(), ct))
            throw new NotFoundException("That poll does not exist.");

        _logger.LogWarning(
            "Admin {AdminId} removed poll {PollId}.", _currentUser.UserId, pollId);

        await _broadcaster.PollRemovedAsync(pollId);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private async Task<PollDocument> RequireVisibleAsync(Guid pollId, CancellationToken ct)
    {
        var poll = await _polls.GetByIdAsync(pollId, ct)
                   ?? throw new NotFoundException("That poll does not exist.");

        if (poll.Status == PollStatus.Removed && !_currentUser.IsAdmin)
            throw new NotFoundException("That poll does not exist.");

        return poll;
    }

    /// <summary>Re-reads the poll and broadcasts the authoritative counts.</summary>
    private async Task<PollDto> PublishAsync(Guid pollId, Guid userId, CancellationToken ct)
    {
        var updated = await _polls.GetByIdAsync(pollId, ct)
                      ?? throw new NotFoundException("That poll does not exist.");

        var vote = await _votes.GetAsync(pollId, userId, ct);
        var reaction = await _reactions.GetAsync(pollId, userId, ct);

        var mine = ToDto(updated, userId,
            vote is null ? [] : new() { [pollId] = vote.OptionId },
            reaction is null ? [] : new() { [pollId] = reaction.IsUpvote });

        // Broadcast a copy with no viewer-specific fields. Each client keeps its own
        // vote state, which is why the payload must not carry someone else's.
        await _broadcaster.PollUpdatedAsync(ToDto(updated, Guid.Empty, [], []));

        return mine;
    }

    private static PollDto ToDto(
        PollDocument p, Guid viewerId,
        Dictionary<Guid, Guid> myVotes, Dictionary<Guid, bool> myReactions)
    {
        var total = p.Options.Sum(o => o.VoteCount);

        return new PollDto(
            p.Id,
            p.Question,
            p.CreatorName,
            IsMine: p.CreatorId == viewerId,
            Options: p.Options.Select(o => new PollOptionDto(
                o.Id, o.Text, o.VoteCount,
                total > 0 ? Math.Round(o.VoteCount / (double)total * 100, 1) : 0)).ToList(),
            TotalVotes: total,
            Upvotes: p.Upvotes,
            Downvotes: p.Downvotes,
            Status: p.Status,
            CreatedAt: p.CreatedAt,
            MyVoteOptionId: myVotes.TryGetValue(p.Id, out var option) ? option : null,
            MyReaction: myReactions.TryGetValue(p.Id, out var r) ? r : null);
    }
}
