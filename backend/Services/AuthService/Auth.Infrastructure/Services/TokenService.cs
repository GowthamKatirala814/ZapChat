using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.Abstractions;
using Auth.Domain.Documents;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZapChat.Shared.Auth;

namespace Auth.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwt;

    public TokenService(IOptions<JwtOptions> jwt) => _jwt = jwt.Value;

    public string CreateAccessToken(UserDocument user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // The anonymous name travels in the token so Chat and PrivateChat never
            // need a lookup to render a message author.
            new(ZapChatClaims.AnonymousName, user.Anonymous.Name),
            new(ZapChatClaims.Department, user.Department),

            // Branch is a claim because it gates branch-room access. It is set from
            // the stored value, which only an admin can change.
            new(ZapChatClaims.Branch, user.Branch)
        };

        // The email claim is deliberately NOT included. Every service that used to
        // read it only needed the user id or the anonymous name, and putting a real
        // address in a token that the browser can decode leaked identity.

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string raw, RefreshTokenDocument document) CreateRefreshToken(
        Guid userId, Guid? familyId = null)
    {
        var raw = CreateOpaqueToken();

        var document = new RefreshTokenDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            // Only the hash is stored, so a database read never yields a usable token.
            TokenHash = Hash(raw),
            FamilyId = familyId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        };

        return (raw, document);
    }

    public string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string CreateOpaqueToken()
    {
        // 256 bits, URL-safe so it can travel in a query string or JSON body.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// Uses RandomNumberGenerator, not Random.Shared. The old OTP generator used a
    /// non-cryptographic PRNG for both registration and password-reset codes.
    /// </summary>
    public string CreateNumericCode(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString(new string('0', digits));
    }
}
