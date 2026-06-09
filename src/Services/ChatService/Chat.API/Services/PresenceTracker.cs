using System.Collections.Concurrent;

namespace Chat.API.Services;

public class PresenceTracker
{
    private static readonly ConcurrentDictionary<string, string>
        OnlineUsers = new();

    // Store anonymousName keyed by connectionId
    public Task UserConnected(
        string connectionId,
        string anonymousName)
    {
        OnlineUsers[connectionId] = anonymousName;
        return Task.CompletedTask;
    }

    public Task UserDisconnected(
        string connectionId)
    {
        OnlineUsers.TryRemove(connectionId, out _);

        return Task.CompletedTask;
    }

    public Task<List<string>> GetOnlineUsers()
    {
        var users = OnlineUsers.Values
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return Task.FromResult(users);
    }
}