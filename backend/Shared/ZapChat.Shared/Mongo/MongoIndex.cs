using MongoDB.Driver;

namespace ZapChat.Shared.Mongo;

/// <summary>
/// Small helpers so index declarations read as one line each instead of five.
/// </summary>
public static class MongoIndex
{
    public static CreateIndexModel<T> Asc<T>(
        System.Linq.Expressions.Expression<Func<T, object?>> field,
        string? name = null,
        bool unique = false)
        => new(Builders<T>.IndexKeys.Ascending(field),
            new CreateIndexOptions { Name = name, Unique = unique });

    public static CreateIndexModel<T> Desc<T>(
        System.Linq.Expressions.Expression<Func<T, object?>> field,
        string? name = null)
        => new(Builders<T>.IndexKeys.Descending(field),
            new CreateIndexOptions { Name = name });

    public static CreateIndexModel<T> Compound<T>(
        IndexKeysDefinition<T> keys,
        string? name = null,
        bool unique = false)
        => new(keys, new CreateIndexOptions { Name = name, Unique = unique });

    /// <summary>
    /// A TTL index. Mongo deletes the document once <paramref name="field"/> is older
    /// than <paramref name="expireAfter"/>. Used for OTPs and refresh tokens so expired
    /// credentials are removed by the database rather than by cleanup code nobody wrote.
    /// </summary>
    public static CreateIndexModel<T> Ttl<T>(
        System.Linq.Expressions.Expression<Func<T, object?>> field,
        TimeSpan expireAfter,
        string? name = null)
        => new(Builders<T>.IndexKeys.Ascending(field),
            new CreateIndexOptions { Name = name, ExpireAfter = expireAfter });

    /// <summary>
    /// Creates indexes, tolerating the case where an equivalent index already exists
    /// under a different name (Mongo error 85/86).
    /// </summary>
    public static async Task EnsureAsync<T>(
        IMongoCollection<T> collection,
        IEnumerable<CreateIndexModel<T>> indexes,
        CancellationToken ct = default)
    {
        foreach (var index in indexes)
        {
            try
            {
                await collection.Indexes.CreateOneAsync(index, cancellationToken: ct);
            }
            catch (MongoCommandException ex) when (ex.Code is 85 or 86)
            {
                // IndexOptionsConflict / IndexKeySpecsConflict — an equivalent index
                // is already present. Nothing to do.
            }
        }
    }
}
