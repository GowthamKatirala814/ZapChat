using Auth.Application.Abstractions;
using Auth.Domain.Documents;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Auth.Infrastructure.Repositories;

public sealed class OtpRepository : IOtpRepository
{
    private readonly IMongoCollection<OtpDocument> _otps;

    public OtpRepository(AuthMongoContext context) => _otps = context.OtpsCollection;

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public Task InsertAsync(OtpDocument otp, CancellationToken ct = default)
    {
        otp.Email = Normalize(otp.Email);
        return _otps.InsertOneAsync(otp, cancellationToken: ct);
    }

    public Task<OtpDocument?> GetLatestAsync(
        string email, OtpPurpose purpose, CancellationToken ct = default) =>
        _otps.Find(o => o.Email == Normalize(email) && o.Purpose == purpose && !o.IsConsumed)
            .SortByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct)!;

    public Task<OtpDocument?> GetByFollowUpTokenAsync(string tokenHash, CancellationToken ct = default) =>
        _otps.Find(o => o.FollowUpTokenHash == tokenHash && o.IsVerified && !o.IsConsumed)
            .FirstOrDefaultAsync(ct)!;

    /// <summary>
    /// Atomically increments and only succeeds while attempts remain, so a race
    /// cannot let a caller exceed the limit.
    /// </summary>
    public async Task<bool> IncrementAttemptsAsync(Guid id, CancellationToken ct = default)
    {
        var updated = await _otps.FindOneAndUpdateAsync(
            Builders<OtpDocument>.Filter.Eq(o => o.Id, id),
            Builders<OtpDocument>.Update.Inc(o => o.Attempts, 1),
            new FindOneAndUpdateOptions<OtpDocument> { ReturnDocument = ReturnDocument.After },
            ct);

        return updated is not null && updated.Attempts <= updated.MaxAttempts;
    }

    public async Task<bool> MarkVerifiedAsync(
        Guid id, string followUpTokenHash, CancellationToken ct = default)
    {
        var result = await _otps.UpdateOneAsync(
            o => o.Id == id && !o.IsConsumed,
            Builders<OtpDocument>.Update
                .Set(o => o.IsVerified, true)
                .Set(o => o.VerifiedAt, DateTime.UtcNow)
                .Set(o => o.FollowUpTokenHash, followUpTokenHash),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> ConsumeAsync(Guid id, CancellationToken ct = default)
    {
        // Guarded on !IsConsumed so a replayed final step cannot create two accounts.
        var result = await _otps.UpdateOneAsync(
            o => o.Id == id && !o.IsConsumed,
            Builders<OtpDocument>.Update.Set(o => o.IsConsumed, true),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<long> InvalidatePendingAsync(
        string email, OtpPurpose purpose, CancellationToken ct = default)
    {
        var result = await _otps.UpdateManyAsync(
            o => o.Email == Normalize(email) && o.Purpose == purpose && !o.IsConsumed,
            Builders<OtpDocument>.Update.Set(o => o.IsConsumed, true),
            cancellationToken: ct);

        return result.ModifiedCount;
    }
}
