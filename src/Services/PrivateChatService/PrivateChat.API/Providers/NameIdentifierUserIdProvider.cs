using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PrivateChat.API.Providers;

/// <summary>
/// Maps SignalR connections to user IDs using the
/// NameIdentifier claim from the JWT token.
/// This is what makes Clients.User(userId) work.
/// </summary>
public class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(
        HubConnectionContext connection)
    {
        return connection.User?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;
    }
}
