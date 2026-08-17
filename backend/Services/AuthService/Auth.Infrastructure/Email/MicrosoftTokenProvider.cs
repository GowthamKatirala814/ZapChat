using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Email;

/// <summary>
/// Acquires Microsoft Entra access tokens with the OAuth2 client-credentials grant.
///
/// Written against the token endpoint directly rather than pulling in MSAL or
/// Azure.Identity: the grant is one form POST, and the alternative is a large dependency
/// tree for a single call. What that costs us is caching, which is handled here — and
/// caching matters, because Entra throttles token requests and the naive version asks
/// for a fresh token on every email.
/// </summary>
public interface IMicrosoftTokenProvider
{
    /// <summary>A bearer token for the given scope, from cache when still valid.</summary>
    Task<string> GetTokenAsync(string scope, CancellationToken ct = default);
}

public sealed class MicrosoftTokenProvider : IMicrosoftTokenProvider, IDisposable
{
    /// <summary>Graph application permissions, e.g. Mail.Send.</summary>
    public const string GraphScope = "https://graph.microsoft.com/.default";

    /// <summary>Exchange Online, for SMTP XOAUTH2 submission.</summary>
    public const string OutlookScope = "https://outlook.office365.com/.default";

    private readonly GraphOptions _options;
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<MicrosoftTokenProvider> _logger;

    // One entry per scope. The lock is per-provider rather than per-scope because two
    // scopes are the most this will ever hold and contention is not a concern.
    private readonly Dictionary<string, CachedToken> _cache = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MicrosoftTokenProvider(
        IOptions<EmailOptions> options,
        IHttpClientFactory httpClients,
        ILogger<MicrosoftTokenProvider> logger)
    {
        _options = options.Value.Graph;
        _httpClients = httpClients;
        _logger = logger;
    }

    private sealed record CachedToken(string Value, DateTimeOffset ExpiresAt);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }

    private sealed class TokenError
    {
        [JsonPropertyName("error")] public string Error { get; set; } = string.Empty;
        [JsonPropertyName("error_description")] public string Description { get; set; } = string.Empty;
    }

    public async Task<string> GetTokenAsync(string scope, CancellationToken ct = default)
    {
        if (TryGetCached(scope, out var cached)) return cached;

        await _gate.WaitAsync(ct);
        try
        {
            // Re-check inside the lock: several concurrent sends would otherwise each
            // request a token after queuing on an acquisition that already succeeded.
            if (TryGetCached(scope, out cached)) return cached;

            var token = await RequestAsync(scope, ct);

            _cache[scope] = token;
            return token.Value;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryGetCached(string scope, out string token)
    {
        token = string.Empty;

        if (!_cache.TryGetValue(scope, out var entry)) return false;
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow) return false;

        token = entry.Value;
        return true;
    }

    private async Task<CachedToken> RequestAsync(string scope, CancellationToken ct)
    {
        var endpoint = $"{_options.Authority.TrimEnd('/')}/{_options.TenantId}/oauth2/v2.0/token";
        var client = _httpClients.CreateClient("entra-token");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = scope,
                ["grant_type"] = "client_credentials",
            }),
        };

        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new EmailDeliveryException(
                EmailFailureKind.Transient, "entra", endpoint, "-",
                "Could not reach the Microsoft identity platform to get a token.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, ct);

            // AADSTS errors are configuration or consent problems almost without
            // exception, and retrying them only delays a failure the operator must fix.
            // The description is safe to log: it names the app and the missing grant,
            // and never contains the secret that was sent.
            _logger.LogError(
                "Entra token request failed for scope {Scope}: {Status} {Error} — {Description}",
                scope, (int)response.StatusCode, error.Error, error.Description);

            throw new EmailDeliveryException(
                EmailFailureKind.Authentication, "entra", endpoint, "-",
                $"Microsoft rejected the application credentials ({error.Error}).");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
                      ?? throw new EmailDeliveryException(
                          EmailFailureKind.Authentication, "entra", endpoint, "-",
                          "The token endpoint returned no token.");

        // Renew a minute early so a token cannot expire between the check and the send.
        var expiry = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn) - 60);

        _logger.LogDebug(
            "Acquired an Entra token for scope {Scope}, valid for {Seconds}s.",
            scope, payload.ExpiresIn);

        return new CachedToken(payload.AccessToken, expiry);
    }

    private static async Task<TokenError> ReadErrorAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<TokenError>(ct)
                   ?? new TokenError { Error = response.StatusCode.ToString() };
        }
        catch
        {
            return new TokenError { Error = response.StatusCode.ToString() };
        }
    }

    public void Dispose() => _gate.Dispose();
}
