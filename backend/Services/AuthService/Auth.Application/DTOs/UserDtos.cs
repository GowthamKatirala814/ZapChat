namespace Auth.Application.DTOs;

/// <summary>
/// What one user is allowed to learn about another.
///
/// This type deliberately has no Email and no FullName. The old endpoints returned
/// both alongside AnonymousName in the same object, which made the platform's
/// anonymity trivially reversible by any authenticated caller. Anything that used
/// to reach for those fields now cannot.
/// </summary>
public sealed record PublicUserDto(
    Guid Id,
    string AnonymousName,
    string Department,
    string Branch,
    DateTime CreatedAt,
    bool IsDeleted);

/// <summary>
/// The caller's own profile. Real identity appears here and nowhere else, because
/// this is only ever returned for the authenticated caller's own id.
/// </summary>
public sealed record MyProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string Department,
    string Branch,
    string AnonymousName,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

/// <summary>
/// Admin view. Still excludes Email and FullName: an administrator moderates
/// behaviour, and does not need to break anonymity to do it. The one place the real
/// address is used — hashing it to block re-registration — happens server-side and
/// never crosses the wire.
/// </summary>
public sealed record AdminUserDto(
    Guid Id,
    string AnonymousName,
    string Department,
    string Branch,
    DateTime CreatedAt,
    bool IsActive,
    bool IsDeleted,
    DateTime? DeletedAt,
    Guid? DeletedBy,
    string? DeletionReason,
    IReadOnlyList<string> Roles,
    bool IsLockedOut);

/// <summary>Result of a successful sign-in. Carries no token — those are cookies.</summary>
public sealed record AuthResultDto(
    Guid UserId,
    string AnonymousName,
    string Email,
    string Role);

public sealed record UserStatsDto(
    long Total,
    long Active,
    long Deleted);

public sealed class UserQueryParameters
{
    private int _pageSize = 25;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { < 1 => 1, > 200 => 200, _ => value };
    }

    /// <summary>Matches anonymous name, department or branch. Never email or real name.</summary>
    public string? Search { get; set; }

    /// <summary>"All" | "Active" | "Deleted"</summary>
    public string? Status { get; set; }

    public string? Department { get; set; }
    public string? Branch { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
}
