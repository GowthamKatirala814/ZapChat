using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using ZapChat.Shared.Auth;

namespace ZapChat.Shared.Http;

/// <summary>
/// Attaches credentials to outgoing service-to-service requests.
///
/// By default it mints a SERVICE token rather than forwarding the caller's.
///
/// Forwarding the caller's token was the obvious first choice and is wrong here: the
/// internal endpoints these clients target (resolve a user, remove a message, disable
/// an account) require the Admin role, while the caller is an ordinary user. Forwarding
/// produced a 403 that looked exactly like the 401s the old codebase suffered from.
///
/// The calling service has already authorized the user's action before it makes the
/// call; the downstream endpoint is a narrow internal operation, not a re-evaluation of
/// the user's permissions. The minted token carries a "svc" claim naming the caller, so
/// the call is still attributable.
///
/// Set <see cref="ServiceAuthOptions.ForwardUserToken"/> when a downstream endpoint
/// genuinely must apply the end user's own permissions.
/// </summary>
public sealed class ServiceAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;
    private readonly IServiceTokenProvider _tokens;
    private readonly ServiceAuthOptions _options;

    public ServiceAuthHandler(
        IHttpContextAccessor accessor,
        IServiceTokenProvider tokens,
        ServiceAuthOptions options)
    {
        _accessor = accessor;
        _tokens = tokens;
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            if (_options.ForwardUserToken && TryForwardInbound(request))
            {
                // Caller's token forwarded — downstream applies the user's permissions.
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    _tokens.CreateToken(_options.CallerName, _options.ServiceRoles));
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private bool TryForwardInbound(HttpRequestMessage request)
    {
        var inbound = _accessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(inbound) ||
            !AuthenticationHeaderValue.TryParse(inbound, out var parsed))
        {
            return false;
        }

        request.Headers.Authorization = parsed;
        return true;
    }
}

public sealed class ServiceAuthOptions
{
    /// <summary>Identifies the calling service in the minted token and downstream logs.</summary>
    public required string CallerName { get; init; }

    /// <summary>
    /// Roles the minted service token carries. Admin by default, because the internal
    /// endpoints are gated on it.
    /// </summary>
    public string[] ServiceRoles { get; init; } = [ZapChatRoles.Admin];

    /// <summary>
    /// Forward the end user's token instead of minting one. Off by default — see the
    /// class remarks.
    /// </summary>
    public bool ForwardUserToken { get; init; }
}
