using Auth.Application.Abstractions;
using Auth.Domain.Documents;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public sealed class AiUsageRepository : IAiUsageRepository
{
    private readonly IMongoCollection<AiUsageDocument> _usage;

    public AiUsageRepository(AuthMongoContext context) => _usage = context.AiUsageCollection;

    /// <summary>
    /// Upsert on the date key. Because the date is the _id, concurrent callers on the
    /// same day cannot create two documents — the second upsert matches the first.
    /// </summary>
    public async Task<AiUsageDocument> GetOrCreateTodayAsync(CancellationToken ct = default)
    {
        var key = AiUsageDocument.KeyFor(DateTime.UtcNow);

        return await _usage.FindOneAndUpdateAsync<AiUsageDocument>(
            Builders<AiUsageDocument>.Filter.Eq(d => d.Id, key),
            Builders<AiUsageDocument>.Update
                .SetOnInsert(d => d.Id, key)
                .SetOnInsert(d => d.Status, "Healthy")
                .SetOnInsert(d => d.EstimatedDailyQuota, 1500)
                .Set(d => d.UpdatedAt, DateTime.UtcNow),
            new FindOneAndUpdateOptions<AiUsageDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            ct);
    }

    /// <summary>
    /// Records one moderation outcome as a single atomic update. Every counter moves
    /// with $inc, so concurrent moderation calls cannot lose increments the way the
    /// old read-modify-write path could.
    /// </summary>
    public async Task RecordOutcomeAsync(
        bool success, bool blocked, string? errorKind, string? errorMessage,
        CancellationToken ct = default)
    {
        var key = AiUsageDocument.KeyFor(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var update = Builders<AiUsageDocument>.Update
            .SetOnInsert(d => d.Id, key)
            .SetOnInsert(d => d.EstimatedDailyQuota, 1500)
            .Inc(d => d.Requests, 1)
            .Set(d => d.UpdatedAt, now);

        if (success)
        {
            update = update
                .Inc(d => d.Successful, 1)
                .Set(d => d.LastSuccessAt, now)
                .Set(d => d.Status, "Healthy");

            update = blocked
                ? update.Inc(d => d.BlockedMessages, 1)
                : update.Inc(d => d.SafeMessages, 1);
        }
        else
        {
            update = update
                .Inc(d => d.Failed, 1)
                .Set(d => d.LastFailureAt, now)
                .Set(d => d.LastErrorMessage, errorMessage)
                .Set(d => d.Status, "Degraded");

            update = errorKind switch
            {
                "RateLimited" => update.Inc(d => d.Errors.RateLimited, 1),
                "Timeout" => update.Inc(d => d.Errors.Timeouts, 1),
                "Configuration" => update.Inc(d => d.Errors.Configuration, 1),
                "Authentication" => update.Inc(d => d.Errors.Authentication, 1),
                "Server" => update.Inc(d => d.Errors.Server, 1),
                _ => update.Inc(d => d.Errors.InvalidResponse, 1)
            };
        }

        await _usage.UpdateOneAsync(
            Builders<AiUsageDocument>.Filter.Eq(d => d.Id, key),
            update,
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    public async Task AppendHealthEventAsync(
        string previousStatus, string newStatus, string message, CancellationToken ct = default)
    {
        var key = AiUsageDocument.KeyFor(DateTime.UtcNow);

        await _usage.UpdateOneAsync(
            Builders<AiUsageDocument>.Filter.Eq(d => d.Id, key),
            Builders<AiUsageDocument>.Update
                .SetOnInsert(d => d.Id, key)
                // $push with $slice keeps the embedded array bounded, so a flapping
                // dependency cannot grow the document past Mongo's 16 MB limit.
                .PushEach(d => d.Events,
                    [new AiHealthEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        PreviousStatus = previousStatus,
                        NewStatus = newStatus,
                        Message = message
                    }],
                    slice: -200)
                .Set(d => d.Status, newStatus)
                .Set(d => d.UpdatedAt, DateTime.UtcNow),
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    public async Task<IReadOnlyList<AiUsageDocument>> GetRecentAsync(
        int days, CancellationToken ct = default)
    {
        var since = AiUsageDocument.KeyFor(DateTime.UtcNow.AddDays(-Math.Max(1, days)));

        // The _id is a sortable "yyyy-MM-dd" string, so a range scan on the primary
        // key gives the window with no extra index.
        return await _usage
            .Find(Builders<AiUsageDocument>.Filter.Gte(d => d.Id, since))
            .SortByDescending(d => d.Id)
            .ToListAsync(ct);
    }
}
