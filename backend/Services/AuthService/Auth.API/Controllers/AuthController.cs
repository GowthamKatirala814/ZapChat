using Auth.API.Infrastructure;
using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Auth.API.Controllers;

/// <summary>
/// Sign-in, session lifecycle, and the caller's own profile.
///
/// Every action is authenticated by default (the host applies a deny-by-default
/// fallback policy); the anonymous ones opt out explicitly.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _auth;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly AuthCookieWriter _cookies;

    public AuthController(
        IAuthenticationService auth,
        IUserRepository users,
        ICurrentUser currentUser,
        AuthCookieWriter cookies)
    {
        _auth = auth;
        _users = users;
        _currentUser = currentUser;
        _cookies = cookies;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var (result, access, refresh) = await _auth.LoginAsync(request, ct);
        _cookies.Write(Response, access, refresh);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> Refresh(CancellationToken ct)
    {
        var presented = _cookies.ReadRefreshToken(Request);

        try
        {
            var (result, access, refresh) = await _auth.RefreshAsync(presented ?? string.Empty, ct);
            _cookies.Write(Response, access, refresh);
            return Ok(result);
        }
        catch (UnauthorizedException)
        {
            // Clear the cookies so the browser stops retrying with a dead token.
            _cookies.Clear(Response);
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _auth.LogoutAsync(_cookies.ReadRefreshToken(Request), ct);
        _cookies.Clear(Response);
        return Ok(new { message = "Signed out." });
    }

    /// <summary>
    /// Hands the caller their own access token so the SignalR client can pass it in
    /// the query string, which is the one place a WebSocket cannot send a header.
    ///
    /// This used to be [AllowAnonymous] and returned the raw JWT to anyone who asked,
    /// which made HttpOnly cosmetic. It now requires an authenticated caller, and
    /// every service also accepts the cookie directly — so a client that speaks only
    /// HTTP never needs to call this at all.
    /// </summary>
    [HttpGet("token")]
    [Produces("text/plain")]
    public IActionResult GetHubToken()
    {
        var token = _cookies.ReadAccessToken(Request)
                    ?? Request.Headers.Authorization.ToString()
                        .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedException("No access token is present on this request.");

        return Content(token, "text/plain");
    }

    [HttpGet("me")]
    public async Task<ActionResult<MyProfileDto>> Me(CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(_currentUser.RequireUserId(), ct)
                   ?? throw new NotFoundException("Your account no longer exists.");

        return Ok(new MyProfileDto(
            user.Id, user.Email, user.FullName, user.Department, user.Branch,
            user.Anonymous.Name, user.CreatedAt, user.Roles));
    }

    /// <summary>
    /// Self-service profile edit. Only department — branch gates branch-room access,
    /// so it is admin-managed. Previously a user could set their own branch and would
    /// have been able to grant themselves access to another office's channel.
    /// </summary>
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        if (string.IsNullOrWhiteSpace(request.Department))
            throw new ValidationException("Provide a department to update.");

        if (!await _users.UpdateProfileAsync(userId, request.Department, branch: null, ct))
            throw new NotFoundException("Your account no longer exists.");

        var user = await _users.GetByIdAsync(userId, ct)!;
        return Ok(new { department = user!.Department, branch = user.Branch });
    }

    /// <summary>
    /// The directory other users see: anonymous names only. Used by the sidebar to
    /// offer someone to start a DM with.
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<PublicUserDto>>> ListUsers(
        [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var users = await _users.ListAsync(excludeDeleted: !includeDeleted, ct);

        return Ok(users
            .Select(u => new PublicUserDto(
                u.Id, u.Anonymous.Name, u.Department, u.Branch, u.CreatedAt, u.IsDeleted))
            .ToList());
    }

    /// <summary>
    /// A single user's public identity. Callers can only ever learn the anonymous
    /// name — the old version of this route returned the real email address to
    /// unauthenticated callers.
    /// </summary>
    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<PublicUserDto>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
                   ?? throw new NotFoundException("No such user.");

        return Ok(new PublicUserDto(
            user.Id, user.Anonymous.Name, user.Department, user.Branch,
            user.CreatedAt, user.IsDeleted));
    }
}
