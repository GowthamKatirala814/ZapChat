using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Documents;

/// <summary>
/// Collection "refreshTokens".
///
/// Changes from the SQL table:
///   * Only a SHA-256 hash is stored. Previously the raw token sat in the database,
///     so a read of that table yielded live credentials.
///   * <see cref="FamilyId"/> links every token descended from one login, so
///     detecting a replayed token can revoke the whole family rather than
///     treating it as merely invalid.
///   * <see cref="ExpiresAt"/> carries a TTL index — Mongo deletes expired tokens
///     itself. The old table accumulated them forever (57 rows for 28 users).
/// </summary>
public sealed class RefreshTokenDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>SHA-256 of the token. Unique index.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    /// <summary>Shared by every token in one rotation chain.</summary>
    public Guid FamilyId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
