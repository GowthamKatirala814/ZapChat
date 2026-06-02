namespace Chat.API.Services;

public class PresenceTracker
{
    private static readonly Dictionary<string, string>
        OnlineUsers = new();

    public Task UserConnected(
        string connectionId,
        string userEmail)
    {
        OnlineUsers[connectionId] = userEmail;

        return Task.CompletedTask;
    }

    public Task UserDisconnected(
        string connectionId)
    {
        if (OnlineUsers.ContainsKey(connectionId))
        {
            OnlineUsers.Remove(connectionId);
        }

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