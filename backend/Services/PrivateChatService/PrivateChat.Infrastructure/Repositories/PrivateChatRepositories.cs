using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using PrivateChat.Application;
using PrivateChat.Domain.Documents;
using PrivateChat.Infrastructure.Persistence;
using ZapChat.Shared.Results;

namespace PrivateChat.Infrastructure.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<ConversationDocument> _conversations;

    public ConversationRepository(PrivateChatMongoContext context) =>
        _conversations = context.ConversationsCollection;

    private static readonly FilterDefinitionBuilder<ConversationDocument> F =
        Builders<ConversationDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<ConversationDocument> U =
        Builders<ConversationDocument>.Update;

    public Task<ConversationDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _conversations.Find(c => c.Id == id).FirstOrDefaultAsync(ct)!;

    public Task<ConversationDocument?> GetByPairAsync(Guid a, Guid b, CancellationToken ct = default) =>
        _conversations
            .Find(F.Eq(c => c.ParticipantKey, ConversationDocument.KeyFor(a, b)))
            .FirstOrDefaultAsync(ct)!;

    /// <summary>Indexed on participants.userId + lastMessage.sentAt.</summary>
    public async Task<IReadOnlyList<ConversationDocument>> ListForUserAsync(
        Guid userId, CancellationToken ct = default) =>
        await _conversations
            .Find(F.ElemMatch(c => c.Participants, p => p.UserId == userId))
            .Sort(Builders<ConversationDocument>.Sort.Descending("lastMessage.sentAt"))
            .ToListAsync(ct);

    /// <summary>
    /// Upsert keyed on the sorted participant pair. Two simultaneous "start chat"
    /// requests converge on one document instead of racing to create two.
    /// </summary>
    public async Task<ConversationDocument> GetOrCreateAsync(
        Guid a, string aName, Guid b, string bName, CancellationToken ct = default)
    {
        var key = ConversationDocument.KeyFor(a, b);

        var existing = await _conversations
            .Find(F.Eq(c => c.ParticipantKey, key)).FirstOrDefaultAsync(ct);

        if (existing is not null) return existing;

        var (lowId, lowName, highId, highName) = a.CompareTo(b) <= 0
            ? (a, aName, b, bName)
            : (b, bName, a, aName);

        var created = await _conversations.FindOneAndUpdateAsync<ConversationDocument>(
            F.Eq(c => c.ParticipantKey, key),
            U.SetOnInsert(c => c.Id, Guid.NewGuid())
                .SetOnInsert(c => c.ParticipantKey, key)
                .SetOnInsert(c => c.CreatedAt, DateTime.UtcNow)
                .SetOnInsert(c => c.MessageCount, 0)
                .SetOnInsert(c => c.Participants, new List<Participant>
                {
                    new() { UserId = lowId, AnonymousName = lowName },
                    new() { UserId = highId, AnonymousName = highName }
                }),
            new FindOneAndUpdateOptions<ConversationDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            ct);

        return created;
    }

    /// <summary>
    /// Records the newest message, bumps the message count, and increments only the
    /// recipient's unread counter — all in one atomic update using a positional
    /// filtered operator.
    /// </summary>
    public Task SetLastMessageAsync(
        Guid conversationId, LastMessageSummary summary, Guid recipientUserId,
        CancellationToken ct = default)
    {
        var update = U
            .Set(c => c.LastMessage, summary)
            .Inc(c => c.MessageCount, 1)
            .Inc("participants.$[recipient].unreadCount", 1);

        var options = new UpdateOptions
        {
            ArrayFilters =
            [
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("recipient.userId", recipientUserId.ToString()))
            ]
        };

        return _conversations.UpdateOneAsync(
            F.Eq(c => c.Id, conversationId), update, options, ct);
    }

    public Task ReplaceLastMessageAsync(
        Guid conversationId, LastMessageSummary? replacement, CancellationToken ct = default) =>
        _conversations.UpdateOneAsync(
            F.Eq(c => c.Id, conversationId),
            U.Set(c => c.LastMessage, replacement),
            cancellationToken: ct);

    public async Task<bool> MarkReadAsync(
        Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        var result = await _conversations.UpdateOneAsync(
            F.Eq(c => c.Id, conversationId),
            U.Set("participants.$[me].unreadCount", 0)
                .Set("participants.$[me].lastReadAt", DateTime.UtcNow),
            new UpdateOptions
            {
                ArrayFilters =
                [
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument("me.userId", userId.ToString()))
                ]
            },
            ct);

        return result.MatchedCount > 0;
    }

    public Task RefreshAnonymousNameAsync(
        Guid userId, string anonymousName, CancellationToken ct = default) =>
        _conversations.UpdateManyAsync(
            F.ElemMatch(c => c.Participants, p => p.UserId == userId),
            U.Set("participants.$[target].anonymousName", anonymousName),
            new UpdateOptions
            {
                ArrayFilters =
                [
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument("target.userId", userId.ToString()))
                ]
            },
            ct);

    public Task<long> CountAsync(CancellationToken ct = default) =>
        _conversations.CountDocumentsAsync(F.Empty, cancellationToken: ct);
}

public sealed class DirectMessageRepository : IDirectMessageRepository
{
    private const int MaxPageSize = 100;

    private readonly IMongoCollection<DirectMessageDocument> _messages;

    public DirectMessageRepository(PrivateChatMongoContext context) =>
        _messages = context.MessagesCollection;

    private static readonly FilterDefinitionBuilder<DirectMessageDocument> F =
        Builders<DirectMessageDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<DirectMessageDocument> U =
        Builders<DirectMessageDocument>.Update;

    public Task<DirectMessageDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _messages.Find(m => m.Id == id).FirstOrDefaultAsync(ct)!;

    public Task InsertAsync(DirectMessageDocument message, CancellationToken ct = default) =>
        _messages.InsertOneAsync(message, cancellationToken: ct);

    private static string Encode(DirectMessageDocument m) =>
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

    public async Task<CursorPage<DirectMessageDocument>> GetHistoryAsync(
        Guid conversationId, string? before, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxPageSize);

        var filter = F.Eq(m => m.ConversationId, conversationId);

        if (!string.IsNullOrWhiteSpace(before) && TryDecode(before, out var ticks, out var id))
        {
            var at = new DateTime(ticks, DateTimeKind.Utc);
            filter = F.And(filter, F.Or(
                F.Lt(m => m.SentAt, at),
                F.And(F.Eq(m => m.SentAt, at), F.Lt(m => m.Id, id))));
        }

        var batch = await _messages.Find(filter)
            .Sort(Builders<DirectMessageDocument>.Sort
                .Descending(m => m.SentAt)
                .Descending(m => m.Id))
            .Limit(limit + 1)
            .ToListAsync(ct);

        var hasMore = batch.Count > limit;
        var page = hasMore ? batch.Take(limit).ToList() : batch;
        page.Reverse();

        return new CursorPage<DirectMessageDocument>
        {
            Items = page,
            HasMore = hasMore,
            NextCursor = page.Count > 0 ? Encode(page[0]) : null
        };
    }

    public async Task<bool> EditAsync(
        Guid id, Guid senderUserId, string content, CancellationToken ct = default)
    {
        var result = await _messages.UpdateOneAsync(
            F.And(
                F.Eq(m => m.Id, id),
                F.Eq(m => m.Sender.UserId, senderUserId),
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

        if (kind == DeletionKind.User && actorUserId.HasValue)
            filter = F.And(filter, F.Eq(m => m.Sender.UserId, actorUserId.Value));

        var result = await _messages.UpdateOneAsync(
            filter,
            U.Set(m => m.State.Deletion, new Deletion
            {
                Kind = kind, At = DateTime.UtcNow, By = actorUserId, Reason = reason
            }),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<long> SoftDeleteAllBySenderAsync(
        Guid senderUserId, string reason, CancellationToken ct = default)
    {
        var result = await _messages.UpdateManyAsync(
            F.And(
                F.Eq(m => m.Sender.UserId, senderUserId),
                F.Eq(m => m.State.Deletion.Kind, DeletionKind.None)),
            U.Set(m => m.State.Deletion, new Deletion
            {
                Kind = DeletionKind.Moderation, At = DateTime.UtcNow, Reason = reason
            }),
            cancellationToken: ct);

        return result.ModifiedCount;
    }

    /// <summary>Same three-step atomic toggle used for room messages.</summary>
    public async Task<DirectMessageDocument?> ToggleReactionAsync(
        Guid messageId, Guid userId, string anonymousName, string emoji,
        CancellationToken ct = default)
    {
        var visible = F.And(
            F.Eq(m => m.Id, messageId),
            F.Eq(m => m.State.Deletion.Kind, DeletionKind.None));

        var after = new FindOneAndUpdateOptions<DirectMessageDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var removed = await _messages.FindOneAndUpdateAsync(
            F.And(visible, F.ElemMatch(m => m.Reactions,
                r => r.Emoji == emoji && r.UserIds.Contains(userId))),
            U.Pull("reactions.$.userIds", userId).Pull("reactions.$.names", anonymousName),
            after, ct);

        if (removed is not null)
        {
            await _messages.UpdateOneAsync(
                F.Eq(m => m.Id, messageId),
                U.PullFilter(m => m.Reactions, r => r.UserIds.Count == 0),
                cancellationToken: ct);

            return await GetByIdAsync(messageId, ct);
        }

        var added = await _messages.FindOneAndUpdateAsync(
            F.And(visible, F.ElemMatch(m => m.Reactions, r => r.Emoji == emoji)),
            U.AddToSet("reactions.$.userIds", userId).AddToSet("reactions.$.names", anonymousName),
            after, ct);

        if (added is not null) return added;

        return await _messages.FindOneAndUpdateAsync(
            F.And(visible, F.Not(F.ElemMatch(m => m.Reactions, r => r.Emoji == emoji))),
            U.Push(m => m.Reactions, new MessageReaction
            {
                Emoji = emoji, UserIds = [userId], Names = [anonymousName]
            }),
            after, ct);
    }

    /// <summary>
    /// Marks every unread inbound message read in one command and returns which ids
    /// changed, so the sender's read ticks can be updated precisely.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> MarkConversationReadAsync(
        Guid conversationId, Guid readerUserId, CancellationToken ct = default)
    {
        var filter = F.And(
            F.Eq(m => m.ConversationId, conversationId),
            F.Ne(m => m.Sender.UserId, readerUserId),
            F.Eq(m => m.ReadAt, null));

        var ids = await _messages.Find(filter).Project(m => m.Id).ToListAsync(ct);

        if (ids.Count == 0) return [];

        await _messages.UpdateManyAsync(
            filter, U.Set(m => m.ReadAt, DateTime.UtcNow), cancellationToken: ct);

        return ids;
    }

    public Task<DirectMessageDocument?> GetNewestVisibleAsync(
        Guid conversationId, CancellationToken ct = default) =>
        _messages.Find(F.And(
                F.Eq(m => m.ConversationId, conversationId),
                F.Eq(m => m.State.Deletion.Kind, DeletionKind.None)))
            .Sort(Builders<DirectMessageDocument>.Sort
                .Descending(m => m.SentAt).Descending(m => m.Id))
            .FirstOrDefaultAsync(ct)!;

    public Task<long> CountAsync(CancellationToken ct = default) =>
        _messages.CountDocumentsAsync(
            F.Eq(m => m.State.Deletion.Kind, DeletionKind.None), cancellationToken: ct);

    public async Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

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

        return raw.Select(d => (
            Day: DateTime.SpecifyKind(DateTime.Parse(d["_id"].AsString), DateTimeKind.Utc),
            Count: d["count"].ToInt32())).ToList();
    }
}

public sealed class UserBlockRepository : IUserBlockRepository
{
    private readonly IMongoCollection<UserBlockDocument> _blocks;

    public UserBlockRepository(PrivateChatMongoContext context) => _blocks = context.BlocksCollection;

    private static readonly FilterDefinitionBuilder<UserBlockDocument> F =
        Builders<UserBlockDocument>.Filter;

    public async Task<bool> BlockAsync(
        Guid blockerId, Guid blockedId, CancellationToken ct = default)
    {
        var result = await _blocks.UpdateOneAsync(
            F.And(F.Eq(b => b.BlockerId, blockerId), F.Eq(b => b.BlockedId, blockedId)),
            Builders<UserBlockDocument>.Update
                .SetOnInsert(b => b.Id, Guid.NewGuid())
                .SetOnInsert(b => b.BlockerId, blockerId)
                .SetOnInsert(b => b.BlockedId, blockedId)
                .SetOnInsert(b => b.CreatedAt, DateTime.UtcNow),
            new UpdateOptions { IsUpsert = true },
            ct);

        return result.UpsertedId is not null;
    }

    public async Task<bool> UnblockAsync(
        Guid blockerId, Guid blockedId, CancellationToken ct = default)
    {
        var result = await _blocks.DeleteOneAsync(
            F.And(F.Eq(b => b.BlockerId, blockerId), F.Eq(b => b.BlockedId, blockedId)), ct);

        return result.DeletedCount > 0;
    }

    public async Task<bool> AnyBetweenAsync(Guid a, Guid b, CancellationToken ct = default) =>
        await _blocks.Find(F.Or(
                F.And(F.Eq(x => x.BlockerId, a), F.Eq(x => x.BlockedId, b)),
                F.And(F.Eq(x => x.BlockerId, b), F.Eq(x => x.BlockedId, a))))
            .Project(x => x.Id)
            .AnyAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListBlockedByAsync(
        Guid blockerId, CancellationToken ct = default) =>
        await _blocks.Find(F.Eq(b => b.BlockerId, blockerId))
            .Project(b => b.BlockedId).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListBlockersOfAsync(
        Guid blockedId, CancellationToken ct = default) =>
        await _blocks.Find(F.Eq(b => b.BlockedId, blockedId))
            .Project(b => b.BlockerId).ToListAsync(ct);
}

public sealed class ModerationEventRepository : IModerationEventRepository
{
    private readonly IMongoCollection<ModerationEventDocument> _events;

    public ModerationEventRepository(PrivateChatMongoContext context) =>
        _events = context.ModerationEventsCollection;

    public Task InsertAsync(ModerationEventDocument document, CancellationToken ct = default) =>
        _events.InsertOneAsync(document, cancellationToken: ct);

    public async Task<PrivateModerationStats> GetStatsAsync(CancellationToken ct = default)
    {
        var total = await _events.CountDocumentsAsync(
            Builders<ModerationEventDocument>.Filter.Empty, cancellationToken: ct);

        var allowed = await _events.CountDocumentsAsync(
            Builders<ModerationEventDocument>.Filter.Eq(e => e.WasAllowed, true),
            cancellationToken: ct);

        var byCategory = await _events.Aggregate()
            .Match(Builders<ModerationEventDocument>.Filter.Eq(e => e.WasAllowed, false))
            .Group(e => e.Category, g => new { Category = g.Key, Count = g.Count() })
            .SortByDescending(x => x.Count)
            .Limit(20)
            .ToListAsync(ct);

        return new PrivateModerationStats(
            total, allowed, total - allowed,
            byCategory.ToDictionary(x => x.Category ?? "UNKNOWN", x => x.Count));
    }
}
