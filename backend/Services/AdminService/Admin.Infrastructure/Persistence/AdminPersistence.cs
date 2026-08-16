using Admin.Application;
using Admin.Domain.Documents;
using MongoDB.Bson;
using MongoDB.Driver;
using ZapChat.Shared.Mongo;
using ZapChat.Shared.Results;

namespace Admin.Infrastructure.Persistence;

public sealed class AdminMongoContext
{
    public const string Reports = "reports";
    public const string AuditLogs = "auditLogs";
    public const string BlockedUsers = "blockedUsers";
    public const string Settings = "settings";

    private readonly IMongoDatabase _database;

    public AdminMongoContext(IMongoDatabase database) => _database = database;

    public IMongoCollection<ReportDocument> ReportsCollection =>
        _database.GetCollection<ReportDocument>(Reports);

    public IMongoCollection<AuditLogDocument> AuditLogsCollection =>
        _database.GetCollection<AuditLogDocument>(AuditLogs);

    public IMongoCollection<BlockedUserDocument> BlockedUsersCollection =>
        _database.GetCollection<BlockedUserDocument>(BlockedUsers);

    public IMongoCollection<ModerationSettingsDocument> SettingsCollection =>
        _database.GetCollection<ModerationSettingsDocument>(Settings);
}

public sealed class AdminIndexes : IMongoIndexProvider
{
    public async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct)
    {
        var reports = database.GetCollection<ReportDocument>(AdminMongoContext.Reports);
        await MongoIndex.EnsureAsync(reports,
        [
            // One report per user per message, enforced by the database. This is what
            // makes the threshold rule meaningful.
            MongoIndex.Compound<ReportDocument>(
                Builders<ReportDocument>.IndexKeys
                    .Ascending("target.messageId")
                    .Ascending("reportedBy.userId"),
                "ux_message_reporter", unique: true),

            // The threshold query: distinct reporters per author.
            MongoIndex.Compound<ReportDocument>(
                Builders<ReportDocument>.IndexKeys
                    .Ascending("target.authorUserId")
                    .Ascending(r => r.Status),
                "ix_author_status"),

            MongoIndex.Asc<ReportDocument>(r => r.Status, "ix_status"),
            MongoIndex.Desc<ReportDocument>(r => r.CreatedAt, "ix_createdAt_desc"),
            MongoIndex.Compound<ReportDocument>(
                Builders<ReportDocument>.IndexKeys.Ascending("target.roomId"), "ix_roomId"),
        ], ct);

        var auditLogs = database.GetCollection<AuditLogDocument>(AdminMongoContext.AuditLogs);
        await MongoIndex.EnsureAsync(auditLogs,
        [
            MongoIndex.Desc<AuditLogDocument>(a => a.Timestamp, "ix_timestamp_desc"),
            MongoIndex.Compound<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys
                    .Ascending("entity.type")
                    .Ascending("entity.id"),
                "ix_entity"),
            MongoIndex.Compound<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending("actor.userId"), "ix_actor"),
            MongoIndex.Asc<AuditLogDocument>(a => a.Action, "ix_action"),
        ], ct);

        var blocked = database.GetCollection<BlockedUserDocument>(AdminMongoContext.BlockedUsers);
        await MongoIndex.EnsureAsync(blocked,
        [
            MongoIndex.Asc<BlockedUserDocument>(b => b.UserId, "ux_userId", unique: true),
            // Looked up at registration to stop a banned address returning.
            MongoIndex.Asc<BlockedUserDocument>(b => b.EmailHash, "ix_emailHash"),
        ], ct);

        // settings is a single document keyed "moderation" — _id covers it.
    }
}

public sealed class ReportRepository : IReportRepository
{
    private readonly IMongoCollection<ReportDocument> _reports;

    public ReportRepository(AdminMongoContext context) => _reports = context.ReportsCollection;

    private static readonly FilterDefinitionBuilder<ReportDocument> F =
        Builders<ReportDocument>.Filter;

    public Task<ReportDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _reports.Find(r => r.Id == id).FirstOrDefaultAsync(ct)!;

    /// <summary>
    /// Relies on the unique index instead of a check-then-insert, so two simultaneous
    /// reports from one user cannot both land.
    /// </summary>
    public async Task<bool> TryInsertAsync(ReportDocument report, CancellationToken ct = default)
    {
        try
        {
            await _reports.InsertOneAsync(report, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            return false;
        }
    }

    public async Task<PagedResult<ReportDocument>> SearchAsync(
        ReportQuery query, CancellationToken ct = default)
    {
        var filter = query.Status is { } status
            ? F.Eq(r => r.Status, status)
            : F.Empty;

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 200);

        var total = await _reports.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _reports.Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync(ct);

        return new PagedResult<ReportDocument>
        {
            Items = items, TotalCount = total, Page = page, PageSize = size
        };
    }

    public async Task<bool> ResolveAsync(
        Guid id, ReportStatus status, Guid resolvedBy, string? note,
        CancellationToken ct = default)
    {
        var result = await _reports.UpdateOneAsync(
            F.And(F.Eq(r => r.Id, id), F.Eq(r => r.Status, ReportStatus.Pending)),
            Builders<ReportDocument>.Update
                .Set(r => r.Status, status)
                .Set(r => r.ResolvedAt, DateTime.UtcNow)
                .Set(r => r.ResolvedBy, resolvedBy)
                .Set(r => r.ResolutionNote, note),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Counts DISTINCT reporters, so one person reporting several of the same author's
    /// messages still counts once toward the threshold.
    /// </summary>
    public async Task<int> CountDistinctReportersForAuthorAsync(
        Guid authorUserId, CancellationToken ct = default)
    {
        var reporters = await _reports.Distinct<Guid>(
            "reportedBy.userId",
            F.And(
                F.Eq("target.authorUserId", authorUserId),
                F.Eq(r => r.Status, ReportStatus.Pending)),
            cancellationToken: ct).ToListAsync(ct);

        return reporters.Count;
    }

    public async Task<IReadOnlyList<(Guid AuthorUserId, string AuthorName, int Reporters)>>
        FindAuthorsOverThresholdAsync(int threshold, CancellationToken ct = default)
    {
        // One aggregation: group pending reports by author, count distinct reporters,
        // keep those at or above the threshold.
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("status", "Pending")),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$target.authorUserId",
                ["name"] = new BsonDocument("$first", "$target.authorAnonymousName"),
                ["reporters"] = new BsonDocument("$addToSet", "$reportedBy.userId")
            }),
            new BsonDocument("$project", new BsonDocument
            {
                ["name"] = 1,
                ["count"] = new BsonDocument("$size", "$reporters")
            }),
            new BsonDocument("$match",
                new BsonDocument("count", new BsonDocument("$gte", threshold)))
        };

        var raw = await _reports.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        return raw
            .Where(d => !d["_id"].IsBsonNull)
            .Select(d => (
                AuthorUserId: Guid.Parse(d["_id"].AsString),
                AuthorName: d.GetValue("name", "").ToString() ?? string.Empty,
                Reporters: d["count"].ToInt32()))
            .ToList();
    }

    public async Task<long> ResolvePendingForAuthorAsync(
        Guid authorUserId, ReportStatus status, string note, CancellationToken ct = default)
    {
        var result = await _reports.UpdateManyAsync(
            F.And(
                F.Eq("target.authorUserId", authorUserId),
                F.Eq(r => r.Status, ReportStatus.Pending)),
            Builders<ReportDocument>.Update
                .Set(r => r.Status, status)
                .Set(r => r.ResolvedAt, DateTime.UtcNow)
                .Set(r => r.ResolvedBy, Guid.Empty)
                .Set(r => r.ResolutionNote, note),
            cancellationToken: ct);

        return result.ModifiedCount;
    }

    public Task<long> CountAsync(ReportStatus? status = null, CancellationToken ct = default) =>
        _reports.CountDocumentsAsync(
            status is { } s ? F.Eq(r => r.Status, s) : F.Empty, cancellationToken: ct);

    public async Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        var pipeline = new[]
        {
            new BsonDocument("$match",
                new BsonDocument("createdAt", new BsonDocument("$gte", since))),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument("$dateToString",
                    new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$createdAt" } }),
                ["count"] = new BsonDocument("$sum", 1)
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var raw = await _reports.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        return raw.Select(d => (
            Day: DateTime.SpecifyKind(DateTime.Parse(d["_id"].AsString), DateTimeKind.Utc),
            Count: d["count"].ToInt32())).ToList();
    }

    public async Task<IReadOnlyList<(string Reason, int Count)>> CountByReasonAsync(
        int top, CancellationToken ct = default)
    {
        var results = await _reports.Aggregate()
            .Group(r => r.Reason, g => new { Reason = g.Key, Count = g.Count() })
            .SortByDescending(x => x.Count)
            .Limit(Math.Clamp(top, 1, 50))
            .ToListAsync(ct);

        return results.Select(r => (r.Reason ?? "Unspecified", r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(Guid RoomId, int Count)>> CountByRoomAsync(
        CancellationToken ct = default)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match",
                new BsonDocument("target.roomId", new BsonDocument("$ne", BsonNull.Value))),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$target.roomId",
                ["count"] = new BsonDocument("$sum", 1)
            })
        };

        var raw = await _reports.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        return raw
            .Where(d => !d["_id"].IsBsonNull)
            .Select(d => (Guid.Parse(d["_id"].AsString), d["count"].ToInt32()))
            .ToList();
    }
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLogDocument> _logs;

    public AuditLogRepository(AdminMongoContext context) => _logs = context.AuditLogsCollection;

    public Task InsertAsync(AuditLogDocument document, CancellationToken ct = default) =>
        _logs.InsertOneAsync(document, cancellationToken: ct);

    public async Task<PagedResult<AuditLogDocument>> SearchAsync(
        int page, int pageSize, string? entityType, string? entityId,
        CancellationToken ct = default)
    {
        var f = Builders<AuditLogDocument>.Filter;
        var filters = new List<FilterDefinition<AuditLogDocument>>();

        if (!string.IsNullOrWhiteSpace(entityType))
            filters.Add(f.Eq("entity.type", entityType));

        if (!string.IsNullOrWhiteSpace(entityId))
            filters.Add(f.Eq("entity.id", entityId));

        var filter = filters.Count > 0 ? f.And(filters) : f.Empty;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var total = await _logs.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _logs.Find(filter)
            .SortByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDocument>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<AuditLogDocument>> RecentAsync(
        int count, CancellationToken ct = default) =>
        await _logs.Find(Builders<AuditLogDocument>.Filter.Empty)
            .SortByDescending(a => a.Timestamp)
            .Limit(Math.Clamp(count, 1, 200))
            .ToListAsync(ct);
}

public sealed class BlockedUserRepository : IBlockedUserRepository
{
    private readonly IMongoCollection<BlockedUserDocument> _blocked;

    public BlockedUserRepository(AdminMongoContext context) =>
        _blocked = context.BlockedUsersCollection;

    public async Task<bool> BlockAsync(
        BlockedUserDocument document, CancellationToken ct = default)
    {
        var result = await _blocked.UpdateOneAsync(
            Builders<BlockedUserDocument>.Filter.Eq(b => b.UserId, document.UserId),
            Builders<BlockedUserDocument>.Update
                .SetOnInsert(b => b.Id, document.Id)
                .SetOnInsert(b => b.UserId, document.UserId)
                .SetOnInsert(b => b.BlockedAt, DateTime.UtcNow)
                .Set(b => b.AnonymousName, document.AnonymousName)
                .Set(b => b.EmailHash, document.EmailHash)
                .Set(b => b.Reason, document.Reason)
                .Set(b => b.BlockedBy, document.BlockedBy)
                .Set(b => b.Source, document.Source),
            new UpdateOptions { IsUpsert = true },
            ct);

        return result.UpsertedId is not null;
    }

    public async Task<bool> UnblockAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _blocked.DeleteOneAsync(
            Builders<BlockedUserDocument>.Filter.Eq(b => b.UserId, userId), ct);

        return result.DeletedCount > 0;
    }

    public async Task<bool> IsBlockedAsync(Guid userId, CancellationToken ct = default) =>
        await _blocked.Find(b => b.UserId == userId).Project(b => b.Id).AnyAsync(ct);

    public async Task<IReadOnlyList<BlockedUserDocument>> ListAsync(CancellationToken ct = default) =>
        await _blocked.Find(Builders<BlockedUserDocument>.Filter.Empty)
            .SortByDescending(b => b.BlockedAt)
            .ToListAsync(ct);

    public Task<long> CountAsync(CancellationToken ct = default) =>
        _blocked.CountDocumentsAsync(
            Builders<BlockedUserDocument>.Filter.Empty, cancellationToken: ct);
}

public sealed class ModerationSettingsRepository : IModerationSettingsRepository
{
    private readonly IMongoCollection<ModerationSettingsDocument> _settings;

    public ModerationSettingsRepository(AdminMongoContext context) =>
        _settings = context.SettingsCollection;

    /// <summary>Upsert on the fixed key, so exactly one settings document can exist.</summary>
    public Task<ModerationSettingsDocument> GetAsync(CancellationToken ct = default) =>
        _settings.FindOneAndUpdateAsync<ModerationSettingsDocument>(
            Builders<ModerationSettingsDocument>.Filter.Eq(
                s => s.Id, ModerationSettingsDocument.SingletonId),
            Builders<ModerationSettingsDocument>.Update
                .SetOnInsert(s => s.Id, ModerationSettingsDocument.SingletonId)
                .SetOnInsert(s => s.ReportThreshold, 5)
                .SetOnInsert(s => s.AutoActionEnabled, true)
                .SetOnInsert(s => s.AutoRemoveMessages, true)
                .SetOnInsert(s => s.AutoDisableAccount, true)
                .SetOnInsert(s => s.UpdatedAt, DateTime.UtcNow),
            new FindOneAndUpdateOptions<ModerationSettingsDocument>
            {
                IsUpsert = true, ReturnDocument = ReturnDocument.After
            },
            ct);

    public Task<ModerationSettingsDocument> UpdateAsync(
        UpdateModerationSettingsRequest request, Guid updatedBy, CancellationToken ct = default) =>
        _settings.FindOneAndUpdateAsync<ModerationSettingsDocument>(
            Builders<ModerationSettingsDocument>.Filter.Eq(
                s => s.Id, ModerationSettingsDocument.SingletonId),
            Builders<ModerationSettingsDocument>.Update
                .SetOnInsert(s => s.Id, ModerationSettingsDocument.SingletonId)
                .Set(s => s.ReportThreshold, request.ReportThreshold)
                .Set(s => s.AutoActionEnabled, request.AutoActionEnabled)
                .Set(s => s.AutoRemoveMessages, request.AutoRemoveMessages)
                .Set(s => s.AutoDisableAccount, request.AutoDisableAccount)
                .Set(s => s.UpdatedAt, DateTime.UtcNow)
                .Set(s => s.UpdatedBy, updatedBy),
            new FindOneAndUpdateOptions<ModerationSettingsDocument>
            {
                IsUpsert = true, ReturnDocument = ReturnDocument.After
            },
            ct);
}
