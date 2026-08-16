using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;
using ZapChat.Shared.Results;

namespace Auth.API.Controllers;

/// <summary>
/// Admin-only user administration, plus the endpoints other services call.
///
/// Everything here requires the Admin role. The soft-delete route in particular used
/// to be [Authorize] only and took the acting admin's id from the request body, which
/// let any signed-in user delete any account and forge the audit trail.
/// </summary>
[ApiController]
[Route("api/auth/admin/users")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class UserAdminController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UserAdminController> _logger;

    public UserAdminController(
        IUserRepository users, ICurrentUser currentUser, ILogger<UserAdminController> logger)
    {
        _users = users;
        _currentUser = currentUser;
        _logger = logger;
    }

    private static AdminUserDto ToDto(Domain.Documents.UserDocument u) => new(
        u.Id, u.Anonymous.Name, u.Department, u.Branch, u.CreatedAt,
        u.IsActive, u.IsDeleted, u.DeletedAt, u.DeletedBy, u.DeletionReason,
        u.Roles, u.Security.IsLockedOut);

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> Search(
        [FromQuery] UserQueryParameters query, CancellationToken ct)
    {
        var page = await _users.SearchAsync(query, ct);

        return Ok(new PagedResult<AdminUserDto>
        {
            Items = page.Items.Select(ToDto).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> Get(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
                   ?? throw new NotFoundException("No such user.");
        return Ok(ToDto(user));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsDto>> Stats(
        [FromQuery] string? excludeEmail, CancellationToken ct)
        => Ok(await _users.GetStatsAsync(excludeEmail, ct));

    /// <summary>
    /// Soft-deletes an account. The acting admin comes from the token, never the body.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        Guid id, [FromBody] SoftDeleteUserRequest request, CancellationToken ct)
    {
        var adminId = _currentUser.RequireUserId();

        if (adminId == id)
            throw new ValidationException("You cannot delete your own account.");

        var target = await _users.GetByIdAsync(id, ct)
                     ?? throw new NotFoundException("No such user.");

        if (target.Roles.Contains(ZapChatRoles.Admin))
            throw new ForbiddenException("Administrator accounts cannot be deleted through this endpoint.");

        if (!await _users.SoftDeleteAsync(id, adminId, request.Reason, ct))
            throw new ConflictException("That account is already deleted.");

        _logger.LogWarning(
            "Admin {AdminId} soft-deleted user {UserId}. Reason: {Reason}",
            adminId, id, request.Reason);

        return NoContent();
    }

    /// <summary>Branch is admin-managed because it gates branch-room access.</summary>
    [HttpPut("{id:guid}/branch")]
    public async Task<IActionResult> SetBranch(
        Guid id, [FromBody] SetBranchRequest request, CancellationToken ct)
    {
        if (!await _users.SetBranchAsync(id, request.Branch, ct))
            throw new NotFoundException("No such user.");

        _logger.LogInformation(
            "Admin {AdminId} set branch of user {UserId} to {Branch}.",
            _currentUser.UserId, id, request.Branch);

        return NoContent();
    }
}

/// <summary>
/// Service-to-service lookups. Requires the Admin role, which service tokens carry —
/// so these are reachable by sibling services and by administrators, and by nobody else.
/// </summary>
[ApiController]
[Route("api/auth/internal")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class InternalUsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public InternalUsersController(IUserRepository users) => _users = users;

    /// <summary>Resolves ids to anonymous names for message authorship and room member lists.</summary>
    [HttpPost("resolve")]
    public async Task<ActionResult<IReadOnlyList<PublicUserDto>>> Resolve(
        [FromBody] Guid[] ids, CancellationToken ct)
    {
        var users = await _users.GetManyByIdAsync(ids ?? [], ct);

        return Ok(users.Select(u => new PublicUserDto(
            u.Id, u.Anonymous.Name, u.Department, u.Branch, u.CreatedAt, u.IsDeleted)).ToList());
    }

    /// <summary>
    /// Resolves the anonymous name shown on a message back to a user id, so a report
    /// can be attributed. Matches current and previous names.
    /// </summary>
    [HttpGet("by-anonymous-name/{name}")]
    public async Task<ActionResult<PublicUserDto>> ByAnonymousName(string name, CancellationToken ct)
    {
        var user = await _users.GetByAnonymousNameAsync(name, ct)
                   ?? throw new NotFoundException("No user matches that anonymous name.");

        return Ok(new PublicUserDto(
            user.Id, user.Anonymous.Name, user.Department, user.Branch,
            user.CreatedAt, user.IsDeleted));
    }

    /// <summary>
    /// Returns the SHA-256 hash of a user's email, never the address itself. This is
    /// all the admin service needs to block a banned account from re-registering; the
    /// old flow tried to fetch the raw email and failed with a 401 anyway.
    /// </summary>
    [HttpGet("{id:guid}/email-hash")]
    public async Task<ActionResult<object>> EmailHash(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
                   ?? throw new NotFoundException("No such user.");

        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(user.EmailNormalized))).ToLowerInvariant();

        return Ok(new { userId = user.Id, emailHash = hash });
    }

    /// <summary>Soft-deletes a user on behalf of automated moderation.</summary>
    [HttpPost("{id:guid}/soft-delete")]
    public async Task<IActionResult> SoftDelete(
        Guid id, [FromBody] SoftDeleteUserRequest request, CancellationToken ct)
    {
        var target = await _users.GetByIdAsync(id, ct)
                     ?? throw new NotFoundException("No such user.");

        if (target.Roles.Contains(ZapChatRoles.Admin))
            throw new ForbiddenException("Administrator accounts cannot be auto-moderated.");

        if (!await _users.SoftDeleteAsync(id, Guid.Empty, request.Reason, ct))
            throw new ConflictException("That account is already deleted.");

        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsDto>> Stats(
        [FromQuery] string? excludeEmail, CancellationToken ct)
        => Ok(await _users.GetStatsAsync(excludeEmail, ct));

    /// <summary>Every active user id — used to seed room membership.</summary>
    [HttpGet("active-ids")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> ActiveIds(CancellationToken ct)
    {
        var users = await _users.ListAsync(excludeDeleted: true, ct);
        return Ok(users.Select(u => u.Id).ToList());
    }
}
