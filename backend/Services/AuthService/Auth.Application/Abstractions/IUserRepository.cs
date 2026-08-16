using Auth.Application.DTOs;
using Auth.Domain.Documents;
using ZapChat.Shared.Results;

namespace Auth.Application.Abstractions;

/// <summary>
/// The only way application code reaches the users collection. Deliberately narrow:
/// each method matches a real access pattern rather than exposing a queryable.
/// </summary>
public interface IUserRepository
{
    Task<UserDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDocument?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserDocument?> GetByAnonymousNameAsync(string anonymousName, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> AnonymousNameExistsAsync(string name, CancellationToken ct = default);

    /// <summary>Filters the candidate names that are already taken, in one round trip.</summary>
    Task<HashSet<string>> FindTakenAnonymousNamesAsync(
        IReadOnlyCollection<string> candidates, CancellationToken ct = default);

    Task InsertAsync(UserDocument user, CancellationToken ct = default);

    Task<IReadOnlyList<UserDocument>> ListAsync(
        bool excludeDeleted, CancellationToken ct = default);

    Task<IReadOnlyList<UserDocument>> GetManyByIdAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<PagedResult<UserDocument>> SearchAsync(
        UserQueryParameters query, CancellationToken ct = default);

    Task<bool> UpdateProfileAsync(
        Guid id, string? department, string? branch, CancellationToken ct = default);

    /// <summary>Admin-managed branch change. Separate from the self-service profile update.</summary>
    Task<bool> SetBranchAsync(Guid id, string branch, CancellationToken ct = default);

    Task<bool> SetPasswordHashAsync(Guid id, string passwordHash, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(
        Guid id, Guid deletedBy, string reason, CancellationToken ct = default);

    Task<bool> AddRoleAsync(Guid id, string role, CancellationToken ct = default);

    /// <summary>Records a failed sign-in and applies a lockout once the limit is hit.</summary>
    Task RegisterFailedLoginAsync(
        Guid id, int maxAttempts, TimeSpan lockout, CancellationToken ct = default);

    Task ClearLoginFailuresAsync(Guid id, CancellationToken ct = default);

    Task<UserStatsDto> GetStatsAsync(string? excludeEmail, CancellationToken ct = default);
}
