using System.ComponentModel.DataAnnotations;

namespace ZapChat.Shared.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC signing key. Must be supplied by configuration or environment —
    /// there is no default, and startup fails without it.
    /// </summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:Secret must be at least 32 characters.")]
    public string Secret { get; set; } = string.Empty;

    [Required] public string Issuer { get; set; } = "ZapChat";
    [Required] public string Audience { get; set; } = "ZapChatUsers";

    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 15;
    [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Cookie name carrying the access token.</summary>
    public string AccessCookieName { get; set; } = "access_token";

    /// <summary>Cookie name carrying the refresh token.</summary>
    public string RefreshCookieName { get; set; } = "refresh_token";
}
