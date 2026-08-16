using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ZapChat.Shared.Errors;

namespace ZapChat.Shared.Auth;

/// <summary>
/// The authenticated caller, derived from the validated JWT only.
///
/// This type exists so that no endpoint ever needs to accept a userId parameter.
/// Every place that previously read an id from a query string or request body
/// (poll votes, reports, blocks, read receipts, soft deletes) now reads it here.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Null when unauthenticated.</summary>
    Guid? UserId { get; }

    string AnonymousName { get; }
    string? Branch { get; }
    string? Department { get; }
    bool IsAdmin { get; }

    /// <summary>The caller's id, or a 401 if unauthenticated. Use this in handlers.</summary>
    Guid RequireUserId();
}

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string AnonymousName =>
        Principal?.FindFirst(ZapChatClaims.AnonymousName)?.Value ?? "Anonymous";

    public string? Branch => Principal?.FindFirst(ZapChatClaims.Branch)?.Value;

    public string? Department => Principal?.FindFirst(ZapChatClaims.Department)?.Value;

    public bool IsAdmin => Principal?.IsInRole(ZapChatRoles.Admin) == true;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedException("The request is not authenticated.");
}
