using MongoDB.Bson;
using MongoDB.Driver;
using Poll.Application;
using Poll.Domain.Documents;
using ZapChat.Shared.Mongo;

namespace Poll.Infrastructure.Persistence;

public sealed class PollMongoContext
{
    public const string Polls = "polls";
    public const string PollVotes = "pollVotes";
    public const string PollReactions = "pollReactions";

    private readonly IMongoDatabase _database;

    public PollMongoContext(IMongoDatabase database) => _database = database;

    public IMongoCollection<PollDocument> PollsCollection =>
        _database.GetCollection<PollDocument>(Polls);

    public IMongoCollection<PollVoteDocument> VotesCollection =>
        _database.GetCollection<PollVoteDocument>(PollVotes);

    public IMongoCollection<PollReactionDocument> ReactionsCollection =>
        _database.GetCollection<PollReactionDocument>(PollReactions);
}

public sealed class PollIndexes : IMongoIndexProvider
{
    public async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct)
    {
        var polls = database.GetCollection<PollDocument>(PollMongoContext.Polls);
        await MongoIndex.EnsureAsync(polls,
        [
            MongoIndex.Desc<PollDocument>(p => p.CreatedAt, "ix_createdAt_desc"),
            MongoIndex.Asc<PollDocument>(p => p.Status, "ix_status"),
            MongoIndex.Asc<PollDocument>(p => p.CreatorId, "ix_creatorId"),
            MongoIndex.Desc<PollDocument>(p => p.TotalVotes, "ix_totalVotes_desc"),
        ], ct);

        var votes = database.GetCollection<PollVoteDocument>(PollMongoContext.PollVotes);
        await MongoIndex.EnsureAsync(votes,
        [
            // The constraint the old schema was missing. One vote per user per poll is
            // now guaranteed by the database, not by an application check.
            MongoIndex.Compound<PollVoteDocument>(
                Builders<PollVoteDocument>.IndexKeys
                    .Ascending(v => v.PollId)
                    .Ascending(v => v.UserId),
                "ux_poll_user", unique: true),

            MongoIndex.Asc<PollVoteDocument>(v => v.UserId, "ix_userId"),
        ], ct);

        var reactions = database.GetCollection<PollReactionDocument>(PollMongoContext.PollReactions);
        await MongoIndex.EnsureAsync(reactions,
        [
            MongoIndex.Compound<PollReactionDocument>(
                Builders<PollReactionDocument>.IndexKeys
                    .Ascending(r => r.PollId)
                    .Ascending(r => r.UserId),
                "ux_poll_user", unique: true),

            MongoIndex.Asc<PollReactionDocument>(r => r.UserId, "ix_userId"),
        ], ct);
    }
}

public sealed class PollRepository : IPollRepository
{
    private readonly IMongoCollection<PollDocument> _polls;

    public PollRepository(PollMongoContext context) => _polls = context.PollsCollection;

    private static readonly FilterDefinitionBuilder<PollDocument> F = Builders<PollDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<PollDocument> U = Builders<PollDocument>.Update;

    public Task<PollDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _polls.Find(p => p.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<PollDocument>> ListAsync(
        bool includeRemoved, int limit, CancellationToken ct = default)
    {
        var filter = includeRemoved
            ? F.Empty
            : F.Ne(p => p.Status, PollStatus.Removed);

        return await _polls.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Limit(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);
    }

    public Task InsertAsync(PollDocument poll, CancellationToken ct = default) =>
        _polls.InsertOneAsync(poll, cancellationToken: ct);

    /// <summary>
    /// Moves vote counts atomically. Both option counters and the poll total change in
    /// one update, so a reader can never see a total that disagrees with the options.
    /// </summary>
    public Task AdjustVoteAsync(
        Guid pollId, Guid? incrementOptionId, Guid? decrementOptionId, int totalDelta,
        CancellationToken ct = default)
    {
        var updates = new List<UpdateDefinition<PollDocument>>();
        var arrayFilters = new List<ArrayFilterDefinition>();

        if (incrementOptionId is { } inc)
        {
            updates.Add(U.Inc("options.$[up].voteCount", 1));
            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(
                new BsonDocument("up._id", inc.ToString())));
        }

        if (decrementOptionId is { } dec)
        {
            updates.Add(U.Inc("options.$[down].voteCount", -1));
            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(
                new BsonDocument("down._id", dec.ToString())));
        }

        if (totalDelta != 0)
            updates.Add(U.Inc(p => p.TotalVotes, totalDelta));

        if (updates.Count == 0) return Task.CompletedTask;

        return _polls.UpdateOneAsync(
            F.Eq(p => p.Id, pollId),
            U.Combine(updates),
            new UpdateOptions { ArrayFilters = arrayFilters },
            ct);
    }

    public Task AdjustReactionsAsync(
        Guid pollId, int upDelta, int downDelta, CancellationToken ct = default)
    {
        var updates = new List<UpdateDefinition<PollDocument>>();

        if (upDelta != 0) updates.Add(U.Inc(p => p.Upvotes, upDelta));
        if (downDelta != 0) updates.Add(U.Inc(p => p.Downvotes, downDelta));

        if (updates.Count == 0) return Task.CompletedTask;

        return _polls.UpdateOneAsync(
            F.Eq(p => p.Id, pollId), U.Combine(updates), cancellationToken: ct);
    }

    public async Task<bool> CloseAsync(Guid pollId, Guid closedBy, CancellationToken ct = default)
    {
        var result = await _polls.UpdateOneAsync(
            F.And(F.Eq(p => p.Id, pollId), F.Eq(p => p.Status, PollStatus.Open)),
            U.Set(p => p.Status, PollStatus.Closed)
                .Set(p => p.ClosedAt, DateTime.UtcNow)
                .Set(p => p.ClosedBy, closedBy),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> ReopenAsync(Guid pollId, CancellationToken ct = default)
    {
        var result = await _polls.UpdateOneAsync(
            F.And(F.Eq(p => p.Id, pollId), F.Eq(p => p.Status, PollStatus.Closed)),
            U.Set(p => p.Status, PollStatus.Open)
                .Unset(p => p.ClosedAt)
                .Unset(p => p.ClosedBy),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> RemoveAsync(Guid pollId, Guid removedBy, CancellationToken ct = default)
    {
        var result = await _polls.UpdateOneAsync(
            F.And(F.Eq(p => p.Id, pollId), F.Ne(p => p.Status, PollStatus.Removed)),
            U.Set(p => p.Status, PollStatus.Removed)
                .Set(p => p.ClosedAt, DateTime.UtcNow)
                .Set(p => p.ClosedBy, removedBy),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public Task<long> CountAsync(CancellationToken ct = default) =>
        _polls.CountDocumentsAsync(
            F.Ne(p => p.Status, PollStatus.Removed), cancellationToken: ct);

    public async Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "createdAt", new BsonDocument("$gte", since) },
                { "status", new BsonDocument("$ne", "Removed") }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument("$dateToString",
                    new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$createdAt" } }) },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var raw = await _polls.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        return raw.Select(d => (
            Day: DateTime.SpecifyKind(DateTime.Parse(d["_id"].AsString), DateTimeKind.Utc),
            Count: d["count"].ToInt32())).ToList();
    }

    public async Task<IReadOnlyList<PollDocument>> TopByVotesAsync(
        int top, CancellationToken ct = default) =>
        await _polls.Find(F.Ne(p => p.Status, PollStatus.Removed))
            .SortByDescending(p => p.TotalVotes)
            .Limit(Math.Clamp(top, 1, 100))
            .ToListAsync(ct);
}

public sealed class PollVoteRepository : IPollVoteRepository
{
    private readonly IMongoCollection<PollVoteDocument> _votes;

    public PollVoteRepository(PollMongoContext context) => _votes = context.VotesCollection;

    private static readonly FilterDefinitionBuilder<PollVoteDocument> F =
        Builders<PollVoteDocument>.Filter;

    public Task<PollVoteDocument?> GetAsync(
        Guid pollId, Guid userId, CancellationToken ct = default) =>
        _votes.Find(v => v.PollId == pollId && v.UserId == userId).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<PollVoteDocument>> GetForUserAsync(
        Guid userId, IReadOnlyCollection<Guid> pollIds, CancellationToken ct = default)
    {
        if (pollIds.Count == 0) return [];

        return await _votes
            .Find(F.And(F.Eq(v => v.UserId, userId), F.In(v => v.PollId, pollIds)))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Relies on the unique index to reject a concurrent second vote, rather than a
    /// check-then-insert that a race can slip through.
    /// </summary>
    public async Task<bool> TryInsertAsync(
        PollVoteDocument vote, CancellationToken ct = default)
    {
        try
        {
            await _votes.InsertOneAsync(vote, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            return false;
        }
    }

    public async Task<bool> ChangeOptionAsync(
        Guid pollId, Guid userId, Guid optionId, CancellationToken ct = default)
    {
        var result = await _votes.UpdateOneAsync(
            F.And(
                F.Eq(v => v.PollId, pollId),
                F.Eq(v => v.UserId, userId),
                F.Ne(v => v.OptionId, optionId)),
            Builders<PollVoteDocument>.Update
                .Set(v => v.OptionId, optionId)
                .Set(v => v.VotedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(
        Guid pollId, Guid userId, CancellationToken ct = default)
    {
        var result = await _votes.DeleteOneAsync(
            F.And(F.Eq(v => v.PollId, pollId), F.Eq(v => v.UserId, userId)), ct);

        return result.DeletedCount > 0;
    }

    public Task<long> CountDistinctVotersAsync(Guid pollId, CancellationToken ct = default) =>
        _votes.CountDocumentsAsync(F.Eq(v => v.PollId, pollId), cancellationToken: ct);
}

public sealed class PollReactionRepository : IPollReactionRepository
{
    private readonly IMongoCollection<PollReactionDocument> _reactions;

    public PollReactionRepository(PollMongoContext context) =>
        _reactions = context.ReactionsCollection;

    private static readonly FilterDefinitionBuilder<PollReactionDocument> F =
        Builders<PollReactionDocument>.Filter;

    public Task<PollReactionDocument?> GetAsync(
        Guid pollId, Guid userId, CancellationToken ct = default) =>
        _reactions.Find(r => r.PollId == pollId && r.UserId == userId).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<PollReactionDocument>> GetForUserAsync(
        Guid userId, IReadOnlyCollection<Guid> pollIds, CancellationToken ct = default)
    {
        if (pollIds.Count == 0) return [];

        return await _reactions
            .Find(F.And(F.Eq(r => r.UserId, userId), F.In(r => r.PollId, pollIds)))
            .ToListAsync(ct);
    }

    public async Task<bool> TryInsertAsync(
        PollReactionDocument reaction, CancellationToken ct = default)
    {
        try
        {
            await _reactions.InsertOneAsync(reaction, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            return false;
        }
    }

    public async Task<bool> FlipAsync(
        Guid pollId, Guid userId, bool isUpvote, CancellationToken ct = default)
    {
        var result = await _reactions.UpdateOneAsync(
            F.And(
                F.Eq(r => r.PollId, pollId),
                F.Eq(r => r.UserId, userId),
                F.Ne(r => r.IsUpvote, isUpvote)),
            Builders<PollReactionDocument>.Update
                .Set(r => r.IsUpvote, isUpvote)
                .Set(r => r.ReactedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(
        Guid pollId, Guid userId, CancellationToken ct = default)
    {
        var result = await _reactions.DeleteOneAsync(
            F.And(F.Eq(r => r.PollId, pollId), F.Eq(r => r.UserId, userId)), ct);

        return result.DeletedCount > 0;
    }
}
