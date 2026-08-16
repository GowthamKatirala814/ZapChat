using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Auth.Domain.Documents;
using Auth.Infrastructure.Persistence;
using MongoDB.Driver;
using ZapChat.Shared.Results;

namespace Auth.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> _users;

    public UserRepository(AuthMongoContext context) => _users = context.UsersCollection;

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public Task<UserDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _users.Find(u => u.Id == id).FirstOrDefaultAsync(ct)!;

    public Task<UserDocument?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _users.Find(u => u.EmailNormalized == Normalize(email)).FirstOrDefaultAsync(ct)!;

    /// <summary>
    /// Also matches a previous name, so a report against an older message still
    /// resolves to the right account after a rename.
    /// </summary>
    public Task<UserDocument?> GetByAnonymousNameAsync(string anonymousName, CancellationToken ct = default)
    {
        var filter = Builders<UserDocument>.Filter.Or(
            Builders<UserDocument>.Filter.Eq(u => u.Anonymous.Name, anonymousName),
            Builders<UserDocument>.Filter.AnyEq(u => u.Anonymous.PreviousNames, anonymousName));

        return _users.Find(filter).FirstOrDefaultAsync(ct)!;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await _users.Find(u => u.EmailNormalized == Normalize(email))
            .Project(u => u.Id).AnyAsync(ct);

    public async Task<bool> AnonymousNameExistsAsync(string name, CancellationToken ct = default) =>
        await _users.Find(u => u.Anonymous.Name == name).Project(u => u.Id).AnyAsync(ct);

    /// <summary>
    /// One query for the whole candidate batch. The old generator issued up to 20,000
    /// sequential existence checks inside a nested loop.
    /// </summary>
    public async Task<HashSet<string>> FindTakenAnonymousNamesAsync(
        IReadOnlyCollection<string> candidates, CancellationToken ct = default)
    {
        if (candidates.Count == 0) return [];

        var taken = await _users
            .Find(Builders<UserDocument>.Filter.In(u => u.Anonymous.Name, candidates))
            .Project(u => u.Anonymous.Name)
            .ToListAsync(ct);

        return taken.ToHashSet(StringComparer.Ordinal);
    }

    public Task InsertAsync(UserDocument user, CancellationToken ct = default)
    {
        user.EmailNormalized = Normalize(user.Email);
        return _users.InsertOneAsync(user, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<UserDocument>> ListAsync(
        bool excludeDeleted, CancellationToken ct = default)
    {
        var filter = excludeDeleted
            ? Builders<UserDocument>.Filter.Eq(u => u.IsDeleted, false)
            : Builders<UserDocument>.Filter.Empty;

        return await _users.Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserDocument>> GetManyByIdAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await _users.Find(Builders<UserDocument>.Filter.In(u => u.Id, ids)).ToListAsync(ct);
    }

    public async Task<PagedResult<UserDocument>> SearchAsync(
        UserQueryParameters query, CancellationToken ct = default)
    {
        var f = Builders<UserDocument>.Filter;
        var filters = new List<FilterDefinition<UserDocument>>();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            !query.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add(query.Status.Equals("Deleted", StringComparison.OrdinalIgnoreCase)
                ? f.Eq(u => u.IsDeleted, true)
                : f.Eq(u => u.IsDeleted, false));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
            filters.Add(f.Eq(u => u.Department, query.Department));

        if (!string.IsNullOrWhiteSpace(query.Branch))
            filters.Add(f.Eq(u => u.Branch, query.Branch));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Anonymous name, department and branch only. Email and full name are
            // deliberately not searchable — searching them was one half of the
            // de-anonymization path.
            var escaped = System.Text.RegularExpressions.Regex.Escape(query.Search.Trim());
            var rx = new MongoDB.Bson.BsonRegularExpression(escaped, "i");
            filters.Add(f.Or(
                f.Regex(u => u.Anonymous.Name, rx),
                f.Regex(u => u.Department, rx),
                f.Regex(u => u.Branch, rx)));
        }

        var filter = filters.Count > 0 ? f.And(filters) : f.Empty;

        var sort = (query.SortBy?.ToLowerInvariant()) switch
        {
            "name" => query.SortDesc
                ? Builders<UserDocument>.Sort.Descending(u => u.Anonymous.Name)
                : Builders<UserDocument>.Sort.Ascending(u => u.Anonymous.Name),
            "department" => query.SortDesc
                ? Builders<UserDocument>.Sort.Descending(u => u.Department)
                : Builders<UserDocument>.Sort.Ascending(u => u.Department),
            "branch" => query.SortDesc
                ? Builders<UserDocument>.Sort.Descending(u => u.Branch)
                : Builders<UserDocument>.Sort.Ascending(u => u.Branch),
            "status" => query.SortDesc
                ? Builders<UserDocument>.Sort.Descending(u => u.IsDeleted)
                : Builders<UserDocument>.Sort.Ascending(u => u.IsDeleted),
            _ => query.SortDesc
                ? Builders<UserDocument>.Sort.Descending(u => u.CreatedAt)
                : Builders<UserDocument>.Sort.Ascending(u => u.CreatedAt)
        };

        var total = await _users.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _users.Find(filter)
            .Sort(sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDocument>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<bool> UpdateProfileAsync(
        Guid id, string? department, string? branch, CancellationToken ct = default)
    {
        var updates = new List<UpdateDefinition<UserDocument>>
        {
            Builders<UserDocument>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow)
        };

        if (!string.IsNullOrWhiteSpace(department))
            updates.Add(Builders<UserDocument>.Update.Set(u => u.Department, department.Trim()));

        if (!string.IsNullOrWhiteSpace(branch))
            updates.Add(Builders<UserDocument>.Update.Set(u => u.Branch, branch.Trim()));

        var result = await _users.UpdateOneAsync(
            u => u.Id == id,
            Builders<UserDocument>.Update.Combine(updates),
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    public async Task<bool> SetBranchAsync(Guid id, string branch, CancellationToken ct = default)
    {
        var result = await _users.UpdateOneAsync(
            u => u.Id == id,
            Builders<UserDocument>.Update
                .Set(u => u.Branch, branch.Trim())
                .Set(u => u.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    public async Task<bool> SetPasswordHashAsync(
        Guid id, string passwordHash, CancellationToken ct = default)
    {
        var result = await _users.UpdateOneAsync(
            u => u.Id == id,
            Builders<UserDocument>.Update
                .Set(u => u.PasswordHash, passwordHash)
                .Set(u => u.UpdatedAt, DateTime.UtcNow)
                // A password change clears any active lockout.
                .Set(u => u.Security, new LoginSecurity()),
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    /// <summary>
    /// Only flips a user that is not already deleted, so the operation is idempotent
    /// and the caller can tell "already deleted" from "deleted now".
    /// </summary>
    public async Task<bool> SoftDeleteAsync(
        Guid id, Guid deletedBy, string reason, CancellationToken ct = default)
    {
        var result = await _users.UpdateOneAsync(
            u => u.Id == id && !u.IsDeleted,
            Builders<UserDocument>.Update
                .Set(u => u.IsDeleted, true)
                .Set(u => u.IsActive, false)
                .Set(u => u.DeletedAt, DateTime.UtcNow)
                .Set(u => u.DeletedBy, deletedBy)
                .Set(u => u.DeletionReason, reason)
                .Set(u => u.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> AddRoleAsync(Guid id, string role, CancellationToken ct = default)
    {
        // $addToSet makes the role grant idempotent without a read first.
        var result = await _users.UpdateOneAsync(
            u => u.Id == id,
            Builders<UserDocument>.Update.AddToSet(u => u.Roles, role),
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    public async Task RegisterFailedLoginAsync(
        Guid id, int maxAttempts, TimeSpan lockout, CancellationToken ct = default)
    {
        // Increment first, then read the new value to decide on a lockout. $inc is
        // atomic, so concurrent attempts cannot both see the same count.
        var updated = await _users.FindOneAndUpdateAsync(
            Builders<UserDocument>.Filter.Eq(u => u.Id, id),
            Builders<UserDocument>.Update
                .Inc(u => u.Security.FailedAttempts, 1)
                .Set(u => u.Security.LastFailedAt, DateTime.UtcNow),
            new FindOneAndUpdateOptions<UserDocument>
            {
                ReturnDocument = ReturnDocument.After
            },
            ct);

        if (updated is not null && updated.Security.FailedAttempts >= maxAttempts)
        {
            await _users.UpdateOneAsync(
                u => u.Id == id,
                Builders<UserDocument>.Update
                    .Set(u => u.Security.LockedUntil, DateTime.UtcNow.Add(lockout))
                    .Set(u => u.Security.FailedAttempts, 0),
                cancellationToken: ct);
        }
    }

    public Task ClearLoginFailuresAsync(Guid id, CancellationToken ct = default) =>
        _users.UpdateOneAsync(
            u => u.Id == id,
            Builders<UserDocument>.Update.Set(u => u.Security, new LoginSecurity()),
            cancellationToken: ct);

    /// <summary>
    /// Counts in a single aggregation instead of fetching every user and counting in
    /// memory, which is what the dashboard used to do.
    /// </summary>
    public async Task<UserStatsDto> GetStatsAsync(string? excludeEmail, CancellationToken ct = default)
    {
        var f = Builders<UserDocument>.Filter;
        var baseFilter = string.IsNullOrWhiteSpace(excludeEmail)
            ? f.Empty
            : f.Ne(u => u.EmailNormalized, excludeEmail.Trim().ToLowerInvariant());

        var total = await _users.CountDocumentsAsync(baseFilter, cancellationToken: ct);
        var deleted = await _users.CountDocumentsAsync(
            f.And(baseFilter, f.Eq(u => u.IsDeleted, true)), cancellationToken: ct);

        return new UserStatsDto(total, total - deleted, deleted);
    }
}
