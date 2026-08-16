using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Auth.Domain.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Auth.Infrastructure.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private const int MaxFailedLogins = 8;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository users,
        IRefreshTokenRepository tokens,
        IPasswordHasher hasher,
        ITokenService tokenService,
        IConfiguration config,
        ILogger<AuthenticationService> logger)
    {
        _users = users;
        _tokens = tokens;
        _hasher = hasher;
        _tokenService = tokenService;
        _config = config;
        _logger = logger;
    }

    public async Task<(AuthResultDto result, string accessToken, string refreshToken)> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        // One message for "no such user", "wrong password" and "deleted account", so
        // the endpoint cannot be used to enumerate which addresses are registered.
        const string genericFailure = "Invalid email or password.";

        if (user is null)
        {
            // Spend comparable time so a missing account is not detectably faster.
            _hasher.VerifyPassword(request.Password, DummyHash);
            throw new UnauthorizedException(genericFailure);
        }

        if (user.Security.IsLockedOut)
        {
            _logger.LogWarning("Login refused for locked-out user {UserId}.", user.Id);
            throw new UnauthorizedException(
                "Too many failed attempts. Try again in a few minutes.");
        }

        if (!_hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            await _users.RegisterFailedLoginAsync(user.Id, MaxFailedLogins, LockoutDuration, ct);
            throw new UnauthorizedException(genericFailure);
        }

        if (!user.CanSignIn)
        {
            _logger.LogInformation("Login refused for disabled/deleted user {UserId}.", user.Id);
            throw new UnauthorizedException(genericFailure);
        }

        await _users.ClearLoginFailuresAsync(user.Id, ct);

        // Bootstrap the admin role from configuration, once, idempotently.
        var adminEmail = _config["AdminSettings:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            string.Equals(user.Email, adminEmail, StringComparison.OrdinalIgnoreCase) &&
            !user.Roles.Contains(ZapChatRoles.Admin))
        {
            await _users.AddRoleAsync(user.Id, ZapChatRoles.Admin, ct);
            user.Roles.Add(ZapChatRoles.Admin);
            _logger.LogInformation("Granted Admin role to configured admin account {UserId}.", user.Id);
        }

        return await IssueAsync(user, familyId: null, ct);
    }

    public async Task<(AuthResultDto result, string accessToken, string refreshToken)> RefreshAsync(
        string presentedRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presentedRefreshToken))
            throw new UnauthorizedException("No refresh token was presented.");

        var hash = _tokenService.Hash(presentedRefreshToken);
        var stored = await _tokens.GetByHashAsync(hash, ct);

        if (stored is null)
            throw new UnauthorizedException("The refresh token is invalid.");

        // Reuse detection. A token that has already been rotated away should never be
        // presented again — if it is, it was captured. Revoking the whole family logs
        // out the attacker and the legitimate session, which is the safe outcome.
        if (stored.RevokedAt is not null)
        {
            var revoked = await _tokens.RevokeFamilyAsync(
                stored.FamilyId, "Reuse of an already-rotated token detected.", ct);

            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}. Revoked {Count} token(s) in family {FamilyId}.",
                stored.UserId, revoked, stored.FamilyId);

            throw new UnauthorizedException("This session has been terminated. Please sign in again.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedException("The refresh token has expired.");

        var user = await _users.GetByIdAsync(stored.UserId, ct);

        if (user is null || !user.CanSignIn)
        {
            await _tokens.RevokeFamilyAsync(stored.FamilyId, "Account is no longer active.", ct);
            throw new UnauthorizedException("This account is no longer active.");
        }

        // Rotate: mark the presented token used, then mint a successor in the same family.
        await _tokens.RevokeAsync(stored.Id, "Rotated.", ct);

        return await IssueAsync(user, stored.FamilyId, ct);
    }

    public async Task LogoutAsync(string? presentedRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presentedRefreshToken)) return;

        var stored = await _tokens.GetByHashAsync(_tokenService.Hash(presentedRefreshToken), ct);
        if (stored is null) return;

        // Revoke the family, not just this token, so every device in the chain is out.
        await _tokens.RevokeFamilyAsync(stored.FamilyId, "Signed out.", ct);
        _logger.LogInformation("User {UserId} signed out; family {FamilyId} revoked.",
            stored.UserId, stored.FamilyId);
    }

    private async Task<(AuthResultDto, string, string)> IssueAsync(
        UserDocument user, Guid? familyId, CancellationToken ct)
    {
        var accessToken = _tokenService.CreateAccessToken(user);
        var (rawRefresh, document) = _tokenService.CreateRefreshToken(user.Id, familyId);
        await _tokens.InsertAsync(document, ct);

        var result = new AuthResultDto(
            user.Id,
            user.Anonymous.Name,
            user.Email,
            user.Roles.Contains(ZapChatRoles.Admin) ? "admin" : "user");

        return (result, accessToken, rawRefresh);
    }

    /// <summary>
    /// A real BCrypt hash of a fixed string. Verified against when the account does
    /// not exist so the response time does not reveal that.
    /// </summary>
    private const string DummyHash =
        "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
}
