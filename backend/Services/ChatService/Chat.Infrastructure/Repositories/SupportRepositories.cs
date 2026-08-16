using Chat.Application.Abstractions;
using Chat.Domain.Documents;
using Chat.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Chat.Infrastructure.Repositories;

public sealed class ModerationEventRepository : IModerationEventRepository
{
    private readonly IMongoCollection<ModerationEventDocument> _events;

    public ModerationEventRepository(ChatMongoContext context) =>
        _events = context.ModerationEventsCollection;

    public Task InsertAsync(ModerationEventDocument document, CancellationToken ct = default) =>
        _events.InsertOneAsync(document, cancellationToken: ct);

    /// <summary>
    /// Computed with aggregation. The old version did ToListAsync() on the entire
    /// audit log and counted in memory.
    /// </summary>
    public async Task<ModerationStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var facet = new[]
        {
            new BsonDocument("$facet", new BsonDocument
            {
                ["totals"] = new BsonArray
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        ["_id"] = BsonNull.Value,
                        ["total"] = new BsonDocument("$sum", 1),
                        ["allowed"] = new BsonDocument("$sum",
                            new BsonDocument("$cond", new BsonArray { "$wasAllowed", 1, 0 })),
                        ["gemini"] = new BsonDocument("$sum",
                            new BsonDocument("$cond", new BsonArray
                            {
                                new BsonDocument("$eq", new BsonArray { "$engine", "Gemini" }), 1, 0
                            })),
                        ["rules"] = new BsonDocument("$sum",
                            new BsonDocument("$cond", new BsonArray
                            {
                                new BsonDocument("$eq", new BsonArray { "$engine", "Rules" }), 1, 0
                            }))
                    })
                },
                ["byCategory"] = new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument("wasAllowed", false)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        ["_id"] = "$category",
                        ["count"] = new BsonDocument("$sum", 1)
                    }),
                    new BsonDocument("$sort", new BsonDocument("count", -1)),
                    new BsonDocument("$limit", 20)
                },
                ["byRule"] = new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        ["wasAllowed"] = false,
                        ["matchedRule"] = new BsonDocument("$ne", BsonNull.Value)
                    }),
                    new BsonDocument("$group", new BsonDocument
                    {
                        ["_id"] = "$matchedRule",
                        ["count"] = new BsonDocument("$sum", 1)
                    }),
                    new BsonDocument("$sort", new BsonDocument("count", -1)),
                    new BsonDocument("$limit", 20)
                }
            })
        };

        var result = await _events.Aggregate<BsonDocument>(facet, cancellationToken: ct)
            .FirstOrDefaultAsync(ct);

        if (result is null)
            return new ModerationStatsDto(0, 0, 0, 0, 0, [], []);

        var totals = result["totals"].AsBsonArray.FirstOrDefault()?.AsBsonDocument;

        var total = totals?.GetValue("total", 0).ToInt64() ?? 0;
        var allowed = totals?.GetValue("allowed", 0).ToInt64() ?? 0;

        static Dictionary<string, int> ToMap(BsonValue array) =>
            array.AsBsonArray
                .Select(v => v.AsBsonDocument)
                .Where(d => !d["_id"].IsBsonNull)
                .ToDictionary(d => d["_id"].AsString, d => d["count"].ToInt32());

        return new ModerationStatsDto(
            total,
            allowed,
            total - allowed,
            totals?.GetValue("gemini", 0).ToInt64() ?? 0,
            totals?.GetValue("rules", 0).ToInt64() ?? 0,
            ToMap(result["byCategory"]),
            ToMap(result["byRule"]));
    }
}

public sealed class FileRepository : IFileRepository
{
    private readonly IMongoCollection<FileDocument> _files;

    public FileRepository(ChatMongoContext context) => _files = context.FilesCollection;

    public Task InsertAsync(FileDocument document, CancellationToken ct = default) =>
        _files.InsertOneAsync(document, cancellationToken: ct);

    public Task<FileDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _files.Find(f => f.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<FileDocument>> GetManyAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await _files.Find(Builders<FileDocument>.Filter.In(f => f.Id, ids)).ToListAsync(ct);
    }

    public Task BindToMessageAsync(
        IReadOnlyCollection<Guid> fileIds, Guid roomId, Guid messageId,
        CancellationToken ct = default) =>
        _files.UpdateManyAsync(
            Builders<FileDocument>.Filter.In(f => f.Id, fileIds),
            Builders<FileDocument>.Update
                .Set(f => f.RoomId, roomId)
                .Set(f => f.MessageId, messageId),
            cancellationToken: ct);
}

/// <summary>
/// Presence backed by Mongo with a TTL index, replacing a static in-memory dictionary
/// that was per-process, lost on restart, and not room-scoped.
/// </summary>
public sealed class PresenceRepository : IPresenceRepository
{
    private readonly IMongoCollection<PresenceDocument> _presence;

    public PresenceRepository(ChatMongoContext context) => _presence = context.PresenceCollection;

    private static readonly FilterDefinitionBuilder<PresenceDocument> F =
        Builders<PresenceDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<PresenceDocument> U =
        Builders<PresenceDocument>.Update;

    public Task ConnectAsync(
        string connectionId, Guid userId, string anonymousName, CancellationToken ct = default) =>
        _presence.ReplaceOneAsync(
            F.Eq(p => p.ConnectionId, connectionId),
            new PresenceDocument
            {
                ConnectionId = connectionId,
                UserId = userId,
                AnonymousName = anonymousName,
                ConnectedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            },
            new ReplaceOptions { IsUpsert = true },
            ct);

    public Task DisconnectAsync(string connectionId, CancellationToken ct = default) =>
        _presence.DeleteOneAsync(F.Eq(p => p.ConnectionId, connectionId), ct);

    public Task JoinRoomAsync(string connectionId, Guid roomId, CancellationToken ct = default) =>
        _presence.UpdateOneAsync(
            F.Eq(p => p.ConnectionId, connectionId),
            U.AddToSet(p => p.RoomIds, roomId).Set(p => p.LastSeenAt, DateTime.UtcNow),
            cancellationToken: ct);

    public Task LeaveRoomAsync(string connectionId, Guid roomId, CancellationToken ct = default) =>
        _presence.UpdateOneAsync(
            F.Eq(p => p.ConnectionId, connectionId),
            U.Pull(p => p.RoomIds, roomId).Set(p => p.LastSeenAt, DateTime.UtcNow),
            cancellationToken: ct);

    public Task HeartbeatAsync(string connectionId, CancellationToken ct = default) =>
        _presence.UpdateOneAsync(
            F.Eq(p => p.ConnectionId, connectionId),
            U.Set(p => p.LastSeenAt, DateTime.UtcNow),
            cancellationToken: ct);

    /// <summary>Distinct users, so two browser tabs count once.</summary>
    public async Task<IReadOnlyList<Guid>> GetOnlineUserIdsAsync(
        Guid roomId, CancellationToken ct = default)
    {
        var ids = await _presence
            .Distinct(p => p.UserId, F.AnyEq(p => p.RoomIds, roomId), cancellationToken: ct)
            .ToListAsync(ct);

        return ids;
    }

    public async Task<IReadOnlyList<Guid>> GetAllOnlineUserIdsAsync(CancellationToken ct = default)
    {
        var ids = await _presence
            .Distinct(p => p.UserId, F.Empty, cancellationToken: ct)
            .ToListAsync(ct);

        return ids;
    }

    public async Task<IReadOnlyList<Guid>> GetRoomsForConnectionAsync(
        string connectionId, CancellationToken ct = default)
    {
        var document = await _presence
            .Find(F.Eq(p => p.ConnectionId, connectionId))
            .Project(p => p.RoomIds)
            .FirstOrDefaultAsync(ct);

        return document ?? [];
    }
}
