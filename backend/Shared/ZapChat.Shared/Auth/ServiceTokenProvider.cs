using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ZapChat.Shared.Auth;

/// <summary>
/// Mints short-lived tokens that one service uses to call another.
///
/// This is the fix for the single most damaging defect in the old codebase:
/// ChatHub, RoomManagementService and ModerationBackgroundService all called
/// protected endpoints on other services with a token-less HttpClient, got 401,
/// swallowed it, and silently disabled unread counts, @mentions, read receipts
/// and auto-moderation.
///
/// The token is signed with the same key the services already trust and carries
/// a distinguishing "svc" claim plus the roles the operation needs, so a service
/// call is auditable and is never mistaken for a human user.
/// </summary>
public interface IServiceTokenProvider
{
    string CreateToken(string callerName, params string[] roles);
}

public sealed class ServiceTokenProvider : IServiceTokenProvider
{
    public const string ServiceClaim = "svc";

    private readonly JwtOptions _jwt;

    public ServiceTokenProvider(IOptions<JwtOptions> jwt) => _jwt = jwt.Value;

    public string CreateToken(string callerName, params string[] roles)
    {
        var claims = new List<Claim>
        {
            // A stable synthetic subject so downstream audit logs are not empty.
            new(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
            new(ServiceClaim, callerName),
            new(ZapChatClaims.AnonymousName, $"system:{callerName}")
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            // Deliberately short: these are used for one call and never stored.
            expires: DateTime.UtcNow.AddMinutes(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
