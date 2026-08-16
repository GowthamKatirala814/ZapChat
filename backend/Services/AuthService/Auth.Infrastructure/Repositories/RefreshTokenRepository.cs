using Auth.Application.Abstractions;
using Auth.Domain.Documents;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshTokenDocument> _tokens;

    public RefreshTokenRepository(AuthMongoContext context) =>
        _tokens = context.RefreshTokensCollection;

    public Task InsertAsync(RefreshTokenDocument token, CancellationToken ct = default) =>
        _tokens.InsertOneAsync(token, cancellationToken: ct);

    public Task<RefreshTokenDocument?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _tokens.Find(t => t.TokenHash == tokenHash).FirstOrDefaultAsync(ct)!;

    public async Task<bool> RevokeAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var result = await _tokens.UpdateOneAsync(
            t => t.Id == id && t.RevokedAt == null,
            Builders<RefreshTokenDocument>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedReason, reason),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<long> RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct = default)
    {
        var result = await _tokens.UpdateManyAsync(
            t => t.FamilyId == familyId && t.RevokedAt == null,
            Builders<RefreshTokenDocument>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedReason, reason),
            cancellationToken: ct);

        return result.ModifiedCount;
    }

    public async Task<long> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var result = await _tokens.UpdateManyAsync(
            t => t.UserId == userId && t.RevokedAt == null,
            Builders<RefreshTokenDocument>.Update
                .Set(t => t.RevokedAt, DateTime.UtcNow)
                .Set(t => t.RevokedReason, reason),
            cancellationToken: ct);

        return result.ModifiedCount;
    }
}
