using Auth.Domain.Documents;
using MongoDB.Driver;
using ZapChat.Shared.Mongo;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Declares every index the auth database needs. Run at startup, idempotent.
/// </summary>
public sealed class AuthIndexes : IMongoIndexProvider
{
    public async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct)
    {
        var users = database.GetCollection<UserDocument>(AuthMongoContext.Users);
        await MongoIndex.EnsureAsync(users,
        [
            // Every login and registration check goes through this.
            MongoIndex.Asc<UserDocument>(u => u.EmailNormalized, "ux_email", unique: true),

            // Anonymous names must be globally unique — they are the public identity.
            MongoIndex.Asc<UserDocument>(u => u.Anonymous.Name, "ux_anonName", unique: true),

            // Admin list filters and sorts.
            MongoIndex.Asc<UserDocument>(u => u.IsDeleted, "ix_isDeleted"),
            MongoIndex.Desc<UserDocument>(u => u.CreatedAt, "ix_createdAt_desc"),
            MongoIndex.Asc<UserDocument>(u => u.Branch, "ix_branch"),
            MongoIndex.Asc<UserDocument>(u => u.Department, "ix_department"),

            // Resolving a report's author by the name shown on a message.
            MongoIndex.Compound<UserDocument>(
                Builders<UserDocument>.IndexKeys
                    .Ascending(u => u.Anonymous.PreviousNames),
                "ix_previousNames"),
        ], ct);

        var tokens = database.GetCollection<RefreshTokenDocument>(AuthMongoContext.RefreshTokens);
        await MongoIndex.EnsureAsync(tokens,
        [
            MongoIndex.Asc<RefreshTokenDocument>(t => t.TokenHash, "ux_tokenHash", unique: true),
            MongoIndex.Asc<RefreshTokenDocument>(t => t.UserId, "ix_userId"),
            MongoIndex.Asc<RefreshTokenDocument>(t => t.FamilyId, "ix_familyId"),

            // Mongo removes expired tokens. Grace period keeps a just-expired token
            // visible briefly so reuse detection can still see it.
            MongoIndex.Ttl<RefreshTokenDocument>(
                t => t.ExpiresAt, TimeSpan.FromDays(2), "ttl_expiresAt"),
        ], ct);

        var otps = database.GetCollection<OtpDocument>(AuthMongoContext.Otps);
        await MongoIndex.EnsureAsync(otps,
        [
            MongoIndex.Compound<OtpDocument>(
                Builders<OtpDocument>.IndexKeys
                    .Ascending(o => o.Email)
                    .Ascending(o => o.Purpose)
                    .Descending(o => o.CreatedAt),
                "ix_email_purpose_created"),

            MongoIndex.Asc<OtpDocument>(o => o.FollowUpTokenHash, "ix_followUpToken"),

            // Expired codes delete themselves.
            MongoIndex.Ttl<OtpDocument>(
                o => o.ExpiresAt, TimeSpan.FromMinutes(30), "ttl_expiresAt"),
        ], ct);

        // aiUsage is keyed by date string, so _id already covers every access path.
    }
}
