using System.Text;
using Chat.Application.Abstractions;
using Chat.Domain.Documents;
using Chat.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using ZapChat.Shared.Results;

namespace Chat.Infrastructure.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private const int MaxPageSize = 100;

    private readonly IMongoCollection<MessageDocument> _messages;

    public MessageRepository(ChatMongoContext context) => _messages = context.MessagesCollection;

    private static readonly FilterDefinitionBuilder<MessageDocument> F =
        Builders<MessageDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<MessageDocument> U =
        Builders<MessageDocument>.Update;

    public Task<MessageDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _messages.Find(m => m.Id == id).FirstOrDefaultAsync(ct)!;

    public Task InsertAsync(MessageDocument message, CancellationToken ct = default) =>
        _messages.InsertOneAsync(message, cancellationToken: ct);

    // ── Cursor pagination ────────────────────────────────────────────────────────
    // The cursor carries both sentAt and id. Two messages can share a millisecond,
    // so sorting on time alone is not a total order and a page boundary could drop
    // or repeat a message.

    private static string Encode(MessageDocument m) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{m.SentAt.Ticks}:{m.Id}"));

    private static bool TryDecode(string cursor, out long ticks, out Guid id)
    {
        ticks = 0;
        id = Guid.Empty;

        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':', 2);
            return parts.Length == 2
                   && long.TryParse(parts[0], out ticks)
                   && Guid.TryParse(parts[1], out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<CursorPage<MessageDocument>> GetHistoryAsync(
        Guid roomId, string? before, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxPageSize);

        var filter = F.Eq(m => m.RoomId, roomId);

        if (!string.IsNullOrWhiteSpace(before) && TryDecode(before, out var ticks, out var id))
        {
            var at = new DateTime(ticks, DateTimeKind.Utc);

            // Strictly older than the cursor: earlier timestamp, or same timestamp
            // with a lower id.
            filter = F.And(filter, F.Or(
                F.Lt(m => m.SentAt, at),
                F.And(F.Eq(m => m.SentAt, at), F.Lt(m => m.Id, id))));
        }

        // Fetch one extra to learn whether another page exists without a count query.
        var batch = await _messages.Find(filter)
            .Sort(Builders<MessageDocument>.Sort
                .Descending(m => m.SentAt)
                .Descending(m => m.Id))
            .Limit(limit + 1)
            .ToListAsync(ct);

        var hasMore = batch.Count > limit;
        var page = hasMore ? batch.Take(limit).ToList() : batch;

        // Returned oldest-first so the client can append without reversing.
        page.Reverse();

        return new CursorPage<MessageDocument>
        {
            Items = page,
            HasMore = hasMore,
            NextCursor = page.Count > 0 ? Encode(page[0]) : null
        };
    }

    /// <summary>
    /// Only the author can edit, and only a visible message. Both conditions are in
    /// the filter, so authorization cannot be bypassed by a race between the check
    /// and the write.
    /// </summary>
    public async Task<bool> EditAsync(
        Guid id, Guid authorUserId, string content, CancellationToken ct = default)
    {
        var result = await _messages.UpdateOneAsync(
            F.And(
                F.Eq(m => m.Id, id),
                F.Eq(m => m.Author.UserId, authorUserId),
                F.Eq(m => m.State.Deletion.Kind, DeletionKind.None)),
            U.Set(m => m.Content, content)
                .Set(m => m.State.IsEdited, true)
                .Set(m => m.State.EditedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid id, Guid? actorUserId, DeletionKind kind, string? reason,
        CancellationToken ct = default)
    {
        var filter = F.And(
            F.Eq(m => m.Id, id),
            F.Eq(m => m.State.Deletion.Kind, DeletionKind.None));

        // A user may only delete their own; moderation may delete any.
        if (kind == DeletionKind.User && actorUserId.HasValue)
            filter = F.And(filter, F.Eq(m => m.Author.UserId, actorUserId.Value));

        var result = await _messages.UpdateOneAsync(
            filter,
            U.Set(m => m.State.Deletion, new Deletion
            {
                Kind = kind,
                At = DateTime.UtcNow,
                By = actorUserId,
                Reason = reason
            }),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<long> SoftDeleteAllByAuthorAsync(
        Guid authorUserId, string reason, CancellationToken ct = default)
    {
        // A single UpdateMany rather than reading the author's messages and looping:
        // the server does the whole sweep in one pass, and no message sent between the
        // read and the write can slip through.
        var result = await _messages.UpdateManyAsync(
            F.And(
                F.Eq(m => m.Author.UserId, authorUserId),
                F.Eq(m => m.State.Deletion.Kind, DeletionKind.None)),
            U.Set(m => m.State.Deletion, new Deletion
            {
                Kind = DeletionKind.Moderation,
                At = DateTime.UtcNow,
                Reason = reason
            }),
            cancellationToken: ct);

        return result.ModifiedCount;
    }

    /// <summary>
    /// Server-authoritative reaction toggle.
    ///
    /// The old hub always inserted a reaction row while the React client toggled
    /// locally, so the two diverged: reacting twice wrote two rows and displayed
    /// none. Here the server decides, in at most three attempted updates, and returns
    /// the resulting document so the client renders exactly what was stored.
    /// </summary>
    public async Task<MessageDocument?> ToggleReactionAsync(
        Guid messageId, Guid userId, string anonymousName, string emoji,
        CancellationToken ct = default)
    {
        var visible = F.And(
            F.Eq(m => m.Id, messageId),
            F.Eq(m => m.State.Deletion.Kind, DeletionKind.None));

        var after = new FindOneAndUpdateOptions<MessageDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        // 1. Already reacted with this emoji -> remove.
        var removed = await _messages.FindOneAndUpdateAsync(
            F.And(visible, F.ElemMatch(m => m.Reactions,
                r => r.Emoji == emoji && r.UserIds.Contains(userId))),
            U.Pull("reactions.$.userIds", userId)
                .Pull("reactions.$.names", anonymousName),
            after, ct);

        if (removed is not null)
        {
            // Drop any group left with no users so the array does not accumulate
            // empty entries.
            await _messages.UpdateOneAsync(
                F.Eq(m => m.Id, messageId),
                U.PullFilter(m => m.Reactions, r => r.UserIds.Count == 0),
                cancellationToken: ct);

            return await GetByIdAsync(messageId, ct);
        }

        // 2. Group exists but this user is not in it -> add.
        var added = await _messages.FindOneAndUpdateAsync(
            F.And(visible, F.ElemMatch(m => m.Reactions, r => r.Emoji == emoji)),
            U.AddToSet("reactions.$.userIds", userId)
                .AddToSet("reactions.$.names", anonymousName),
            after, ct);

        if (added is not null) return added;

        // 3. No group for this emoji yet -> create it.
        return await _messages.FindOneAndUpdateAsync(
            F.And(visible, F.Not(F.ElemMatch(m => m.Reactions, r => r.Emoji == emoji))),
            U.Push(m => m.Reactions, new MessageReaction
            {
                Emoji = emoji,
                UserIds = [userId],
                Names = [anonymousName]
            }),
            after, ct);
    }

    public Task<MessageDocument?> GetNewestVisibleAsync(Guid roomId, CancellationToken ct = default) =>
        _messages.Find(F.And(
                F.Eq(m => m.RoomId, roomId),
                F.Eq(m => m.State.Deletion.Kind, DeletionKind.None)))
            .Sort(Builders<MessageDocument>.Sort
                .Descending(m => m.SentAt)
                .Descending(m => m.Id))
            .FirstOrDefaultAsync(ct)!;

    public Task AttachFilesAsync(
        Guid messageId, IReadOnlyList<MessageAttachment> attachments,
        CancellationToken ct = default) =>
        _messages.UpdateOneAsync(
            F.Eq(m => m.Id, messageId),
            U.PushEach(m => m.Attachments, attachments),
            cancellationToken: ct);

    // ── Analytics ────────────────────────────────────────────────────────────────
    // Aggregation pipelines on the one database. These replace seventeen
    // cross-service HTTP calls, several of which loaded whole tables into memory.

    public Task<long> CountAsync(CancellationToken ct = default) =>
        _messages.CountDocumentsAsync(
            F.Eq(m => m.State.Deletion.Kind, DeletionKind.None), cancellationToken: ct);

    public async Task<IReadOnlyList<(Guid RoomId, int Count)>> CountByRoomAsync(
        int top, CancellationToken ct = default)
    {
        var results = await _messages.Aggregate()
            .Match(F.Eq(m => m.State.Deletion.Kind, DeletionKind.None))
            .Group(m => m.RoomId, g => new { RoomId = g.Key, Count = g.Count() })
            .SortByDescending(x => x.Count)
            .Limit(Math.Clamp(top, 1, 100))
            .ToListAsync(ct);

        return results.Select(r => (r.RoomId, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(string AnonymousName, int Count)>> CountByAuthorAsync(
        int top, CancellationToken ct = default)
    {
        var results = await _messages.Aggregate()
            .Match(F.Eq(m => m.State.Deletion.Kind, DeletionKind.None))
            .Group(m => m.Author.AnonymousName, g => new { Name = g.Key, Count = g.Count() })
            .SortByDescending(x => x.Count)
            .Limit(Math.Clamp(top, 1, 100))
            .ToListAsync(ct);

        return results.Select(r => (r.Name, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        // Grouping happens in the database via $dateToString rather than by pulling
        // every row back and grouping in memory.
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "sentAt", new BsonDocument("$gte", since) },
                { "state.deletion.kind", "None" }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument("$dateToString",
                    new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$sentAt" } }) },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var raw = await _messages.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        return raw
            .Select(d => (
                Day: DateTime.SpecifyKind(DateTime.Parse(d["_id"].AsString), DateTimeKind.Utc),
                Count: d["count"].ToInt32()))
            .ToList();
    }

    public async Task<IReadOnlyList<(int Hour, int Count)>> CountByHourAsync(
        CancellationToken ct = default)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("state.deletion.kind", "None")),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument("$hour", "$sentAt") },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var raw = await _messages.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        var counts = raw.ToDictionary(d => d["_id"].ToInt32(), d => d["count"].ToInt32());

        return Enumerable.Range(0, 24)
            .Select(h => (Hour: h, Count: counts.GetValueOrDefault(h)))
            .ToList();
    }
}
