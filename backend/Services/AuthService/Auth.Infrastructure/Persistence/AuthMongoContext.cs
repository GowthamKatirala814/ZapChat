using Auth.Domain.Documents;
using MongoDB.Driver;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Typed access to the auth database's collections.
///
/// Deliberately just a set of named <see cref="IMongoCollection{T}"/> handles: there
/// is no change tracking and no unit of work, so every repository method issues its
/// own command and its atomicity is visible at the call site rather than deferred to
/// a later save.
/// </summary>
public sealed class AuthMongoContext
{
    public const string Users = "users";
    public const string RefreshTokens = "refreshTokens";
    public const string Otps = "otps";
    public const string AiUsage = "aiUsage";

    private readonly IMongoDatabase _database;

    public AuthMongoContext(IMongoDatabase database) => _database = database;

    public IMongoCollection<UserDocument> UsersCollection =>
        _database.GetCollection<UserDocument>(Users);

    public IMongoCollection<RefreshTokenDocument> RefreshTokensCollection =>
        _database.GetCollection<RefreshTokenDocument>(RefreshTokens);

    public IMongoCollection<OtpDocument> OtpsCollection =>
        _database.GetCollection<OtpDocument>(Otps);

    public IMongoCollection<AiUsageDocument> AiUsageCollection =>
        _database.GetCollection<AiUsageDocument>(AiUsage);
}
