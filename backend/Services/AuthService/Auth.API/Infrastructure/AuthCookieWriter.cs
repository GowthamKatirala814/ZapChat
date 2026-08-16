using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ZapChat.Shared.Auth;

namespace Auth.API.Infrastructure;

public sealed class CookieOptionsConfig
{
    public const string SectionName = "AuthCookies";

    /// <summary>
    /// "Lax" when the SPA is served from the same origin as the API (the Vite dev
    /// proxy setup), "None" when it is genuinely cross-site. Lax is preferred: it
    /// gives CSRF protection that SameSite=None does not.
    /// </summary>
    public string SameSite { get; set; } = "Lax";

    /// <summary>Secure flag. Must be true whenever SameSite is None.</summary>
    public bool Secure { get; set; } = true;

    public string? Domain { get; set; }
}

/// <summary>
/// The single place auth cookies are written, read and cleared. Previously this
/// logic was inline in the controller with hardcoded SameSite=None.
/// </summary>
public sealed class AuthCookieWriter
{
    private readonly JwtOptions _jwt;
    private readonly CookieOptionsConfig _config;

    public AuthCookieWriter(IOptions<JwtOptions> jwt, IOptions<CookieOptionsConfig> config)
    {
        _jwt = jwt.Value;
        _config = config.Value;
    }

    private SameSiteMode SameSite => _config.SameSite.ToLowerInvariant() switch
    {
        "none" => SameSiteMode.None,
        "strict" => SameSiteMode.Strict,
        _ => SameSiteMode.Lax
    };

    public void Write(HttpResponse response, string accessToken, string refreshToken)
    {
        // SameSite=None is only legal alongside Secure. Force it rather than emitting
        // a cookie the browser will silently drop.
        var secure = _config.Secure || SameSite == SameSiteMode.None;

        response.Cookies.Append(_jwt.AccessCookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSite,
            Domain = _config.Domain,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            Path = "/"
        });

        response.Cookies.Append(_jwt.RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSite,
            Domain = _config.Domain,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            // Scoped to the auth routes: no other service ever needs to see it.
            Path = "/api/auth"
        });
    }

    public void Clear(HttpResponse response)
    {
        var secure = _config.Secure || SameSite == SameSiteMode.None;

        var expired = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSite,
            Domain = _config.Domain,
            Expires = DateTimeOffset.UnixEpoch
        };

        response.Cookies.Append(_jwt.AccessCookieName, string.Empty,
            new CookieOptions
            {
                HttpOnly = expired.HttpOnly, Secure = expired.Secure,
                SameSite = expired.SameSite, Domain = expired.Domain,
                Expires = expired.Expires, Path = "/"
            });

        response.Cookies.Append(_jwt.RefreshCookieName, string.Empty,
            new CookieOptions
            {
                HttpOnly = expired.HttpOnly, Secure = expired.Secure,
                SameSite = expired.SameSite, Domain = expired.Domain,
                Expires = expired.Expires, Path = "/api/auth"
            });
    }

    public string? ReadAccessToken(HttpRequest request) =>
        request.Cookies.TryGetValue(_jwt.AccessCookieName, out var value) &&
        !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    public string? ReadRefreshToken(HttpRequest request) =>
        request.Cookies.TryGetValue(_jwt.RefreshCookieName, out var value) &&
        !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}
