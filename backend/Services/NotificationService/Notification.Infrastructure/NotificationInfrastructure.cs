using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Notification.Application;
using Notification.Domain.Documents;
using WebPush;
using ZapChat.Shared.Mongo;

namespace Notification.Infrastructure;

// ══════════════════════════════════════════════════════════════════════════════
//  Persistence
// ══════════════════════════════════════════════════════════════════════════════

public sealed class NotificationMongoContext
{
    public const string Notifications = "notifications";
    public const string PushSubscriptions = "pushSubscriptions";

    private readonly IMongoDatabase _database;

    public NotificationMongoContext(IMongoDatabase database) => _database = database;

    public IMongoCollection<NotificationDocument> NotificationsCollection =>
        _database.GetCollection<NotificationDocument>(Notifications);

    public IMongoCollection<PushSubscriptionDocument> SubscriptionsCollection =>
        _database.GetCollection<PushSubscriptionDocument>(PushSubscriptions);
}

public sealed class NotificationIndexes : IMongoIndexProvider
{
    public async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct)
    {
        var notifications = database
            .GetCollection<NotificationDocument>(NotificationMongoContext.Notifications);

        await MongoIndex.EnsureAsync(notifications,
        [
            // The panel query: this user's notifications, newest first.
            MongoIndex.Compound<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Descending(n => n.CreatedAt),
                "ix_user_createdAt"),

            // Unread badge count.
            MongoIndex.Compound<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Ascending(n => n.IsRead),
                "ix_user_isRead"),

            // Withdrawing notifications when their source message is deleted.
            MongoIndex.Asc<NotificationDocument>(n => n.SourceId, "ix_sourceId"),

            // Notifications are transient. Ninety days is long enough to be useful and
            // short enough that the collection does not grow without bound — the old
            // table had no cleanup at all.
            MongoIndex.Ttl<NotificationDocument>(
                n => n.CreatedAt, TimeSpan.FromDays(90), "ttl_createdAt"),
        ], ct);

        var subscriptions = database
            .GetCollection<PushSubscriptionDocument>(NotificationMongoContext.PushSubscriptions);

        await MongoIndex.EnsureAsync(subscriptions,
        [
            MongoIndex.Asc<PushSubscriptionDocument>(s => s.Endpoint, "ux_endpoint", unique: true),
            MongoIndex.Asc<PushSubscriptionDocument>(s => s.UserId, "ix_userId"),
        ], ct);
    }
}

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<NotificationDocument> _notifications;

    public NotificationRepository(NotificationMongoContext context) =>
        _notifications = context.NotificationsCollection;

    private static readonly FilterDefinitionBuilder<NotificationDocument> F =
        Builders<NotificationDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<NotificationDocument> U =
        Builders<NotificationDocument>.Update;

    public Task InsertAsync(NotificationDocument document, CancellationToken ct = default) =>
        _notifications.InsertOneAsync(document, cancellationToken: ct);

    /// <summary>
    /// The owner is part of the filter, not checked afterwards, so a mismatched caller
    /// simply gets nothing back.
    /// </summary>
    public Task<NotificationDocument?> GetOwnedAsync(
        Guid id, Guid userId, CancellationToken ct = default) =>
        _notifications.Find(n => n.Id == id && n.UserId == userId).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<NotificationDocument>> ListForUserAsync(
        Guid userId, int limit, bool unreadOnly, CancellationToken ct = default)
    {
        var filter = unreadOnly
            ? F.And(F.Eq(n => n.UserId, userId), F.Eq(n => n.IsRead, false))
            : F.Eq(n => n.UserId, userId);

        return await _notifications.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Limit(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);
    }

    public Task<long> CountUnreadAsync(Guid userId, CancellationToken ct = default) =>
        _notifications.CountDocumentsAsync(
            F.And(F.Eq(n => n.UserId, userId), F.Eq(n => n.IsRead, false)),
            cancellationToken: ct);

    public async Task<bool> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var result = await _notifications.UpdateOneAsync(
            F.And(F.Eq(n => n.Id, id), F.Eq(n => n.UserId, userId), F.Eq(n => n.IsRead, false)),
            U.Set(n => n.IsRead, true).Set(n => n.ReadAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    public async Task<long> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _notifications.UpdateManyAsync(
            F.And(F.Eq(n => n.UserId, userId), F.Eq(n => n.IsRead, false)),
            U.Set(n => n.IsRead, true).Set(n => n.ReadAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var result = await _notifications.DeleteOneAsync(
            F.And(F.Eq(n => n.Id, id), F.Eq(n => n.UserId, userId)), ct);

        return result.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<NotificationDocument>> DeleteBySourceAsync(
        Guid sourceId, CancellationToken ct = default)
    {
        // Read first so the affected recipients can be told to remove the badge.
        var affected = await _notifications
            .Find(F.Eq(n => n.SourceId, sourceId)).ToListAsync(ct);

        if (affected.Count == 0) return [];

        await _notifications.DeleteManyAsync(F.Eq(n => n.SourceId, sourceId), ct);

        return affected;
    }

    public Task<long> CountAsync(CancellationToken ct = default) =>
        _notifications.CountDocumentsAsync(F.Empty, cancellationToken: ct);

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
                { "_id", new BsonDocument("$dateToString",
                    new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$createdAt" } }) },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var raw = await _notifications.Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        return raw.Select(d => (
            Day: DateTime.SpecifyKind(DateTime.Parse(d["_id"].AsString), DateTimeKind.Utc),
            Count: d["count"].ToInt32())).ToList();
    }
}

public sealed class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly IMongoCollection<PushSubscriptionDocument> _subscriptions;

    public PushSubscriptionRepository(NotificationMongoContext context) =>
        _subscriptions = context.SubscriptionsCollection;

    private static readonly FilterDefinitionBuilder<PushSubscriptionDocument> F =
        Builders<PushSubscriptionDocument>.Filter;

    /// <summary>
    /// Upsert on the endpoint. A shared browser profile can move between users, so the
    /// owner is reassigned rather than duplicated.
    /// </summary>
    public Task UpsertAsync(PushSubscriptionDocument document, CancellationToken ct = default) =>
        _subscriptions.UpdateOneAsync(
            F.Eq(s => s.Endpoint, document.Endpoint),
            Builders<PushSubscriptionDocument>.Update
                .SetOnInsert(s => s.Id, document.Id)
                .SetOnInsert(s => s.Endpoint, document.Endpoint)
                .SetOnInsert(s => s.CreatedAt, DateTime.UtcNow)
                .Set(s => s.UserId, document.UserId)
                .Set(s => s.P256dh, document.P256dh)
                .Set(s => s.Auth, document.Auth)
                .Set(s => s.LastUsedAt, DateTime.UtcNow)
                .Set(s => s.FailureCount, 0),
            new UpdateOptions { IsUpsert = true },
            ct);

    public async Task<bool> DeleteAsync(
        string endpoint, Guid userId, CancellationToken ct = default)
    {
        var result = await _subscriptions.DeleteOneAsync(
            F.And(F.Eq(s => s.Endpoint, endpoint), F.Eq(s => s.UserId, userId)), ct);

        return result.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<PushSubscriptionDocument>> ListForUserAsync(
        Guid userId, CancellationToken ct = default) =>
        await _subscriptions.Find(F.Eq(s => s.UserId, userId)).ToListAsync(ct);

    public Task RecordFailureAsync(Guid id, CancellationToken ct = default) =>
        _subscriptions.UpdateOneAsync(
            F.Eq(s => s.Id, id),
            Builders<PushSubscriptionDocument>.Update.Inc(s => s.FailureCount, 1),
            cancellationToken: ct);

    public Task RetireAsync(Guid id, CancellationToken ct = default) =>
        _subscriptions.DeleteOneAsync(F.Eq(s => s.Id, id), ct);
}

// ══════════════════════════════════════════════════════════════════════════════
//  Web push
// ══════════════════════════════════════════════════════════════════════════════

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    /// <summary>
    /// VAPID keys. Both must be supplied by configuration for push to be enabled.
    ///
    /// The old code hardcoded a public key and a placeholder private key labelled
    /// "DEMO ONLY", so every send threw and the exception was swallowed — push appeared
    /// to work and delivered nothing. Absent keys now disable the feature explicitly.
    ///
    /// Generate a pair with:  npx web-push generate-vapid-keys
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    public string Subject { get; set; } = "mailto:admin@zapchat.local";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}

public sealed class WebPushDispatcher : IPushDispatcher
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushDispatcher> _logger;
    private readonly WebPushClient? _client;
    private readonly VapidDetails? _vapid;

    public WebPushDispatcher(
        IPushSubscriptionRepository subscriptions,
        IOptions<WebPushOptions> options,
        ILogger<WebPushDispatcher> logger)
    {
        _subscriptions = subscriptions;
        _options = options.Value;
        _logger = logger;

        if (!_options.IsConfigured)
        {
            _logger.LogInformation(
                "Web push is disabled: WebPush:PublicKey / WebPush:PrivateKey are not configured. " +
                "In-app notifications still work.");
            return;
        }

        try
        {
            _vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
            _client = new WebPushClient();
        }
        catch (Exception ex)
        {
            // A malformed key is a configuration error worth surfacing at startup rather
            // than failing silently on every send.
            _logger.LogError(ex,
                "Web push is disabled: the configured VAPID keys are not valid.");
        }
    }

    public bool IsEnabled => _client is not null && _vapid is not null;

    public async Task DispatchAsync(
        Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        if (!IsEnabled) return;

        var subscriptions = await _subscriptions.ListForUserAsync(userId, ct);
        if (subscriptions.Count == 0) return;

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = notification.Title,
            body = notification.Message,
            url = "/"
        });

        foreach (var subscription in subscriptions)
        {
            try
            {
                await _client!.SendNotificationAsync(
                    new WebPush.PushSubscription(
                        subscription.Endpoint, subscription.P256dh, subscription.Auth),
                    payload, _vapid, ct);
            }
            catch (WebPushException ex) when (
                ex.StatusCode is System.Net.HttpStatusCode.NotFound
                              or System.Net.HttpStatusCode.Gone)
            {
                // The browser retired this endpoint. Drop it.
                await _subscriptions.RetireAsync(subscription.Id, ct);
                _logger.LogInformation(
                    "Retired an expired push subscription for user {UserId}.", userId);
            }
            catch (Exception ex)
            {
                await _subscriptions.RecordFailureAsync(subscription.Id, ct);
                _logger.LogWarning(ex,
                    "Push delivery failed for user {UserId}.", userId);
            }
        }
    }
}

/// <summary>Used when VAPID keys are absent, so callers need no null checks.</summary>
public sealed class DisabledPushDispatcher : IPushDispatcher
{
    public bool IsEnabled => false;

    public Task DispatchAsync(
        Guid userId, NotificationDto notification, CancellationToken ct = default) =>
        Task.CompletedTask;
}
