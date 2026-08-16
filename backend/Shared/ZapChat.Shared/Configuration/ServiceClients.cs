namespace ZapChat.Shared.Configuration;

/// <summary>
/// Named HttpClient keys. Referenced by constant so a typo in CreateClient("...")
/// cannot silently produce a client with no base address — which is how the old
/// Chat -> Notification call failed.
/// </summary>
public static class ServiceClients
{
    public const string Auth = "auth-service";
    public const string Chat = "chat-service";
    public const string PrivateChat = "privatechat-service";
    public const string Poll = "poll-service";
    public const string Notification = "notification-service";
    public const string Admin = "admin-service";
}
