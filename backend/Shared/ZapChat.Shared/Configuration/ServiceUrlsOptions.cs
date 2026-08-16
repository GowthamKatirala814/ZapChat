namespace ZapChat.Shared.Configuration;

/// <summary>
/// Base addresses of the sibling services. Every cross-service call resolves its
/// URL from here — no hardcoded hosts or ports anywhere in service code.
/// </summary>
public sealed class ServiceUrlsOptions
{
    public const string SectionName = "ServiceUrls";

    public string AuthService { get; set; } = string.Empty;
    public string ChatService { get; set; } = string.Empty;
    public string PrivateChatService { get; set; } = string.Empty;
    public string PollService { get; set; } = string.Empty;
    public string NotificationService { get; set; } = string.Empty;
    public string AdminService { get; set; } = string.Empty;

    /// <summary>Public origin of the gateway. Used to build absolute file URLs.</summary>
    public string Gateway { get; set; } = string.Empty;

    /// <summary>
    /// Normalised base address for an HttpClient (always ends in '/'), or null when
    /// the URL is not configured.
    /// </summary>
    public static Uri? BaseAddress(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : new Uri(url.TrimEnd('/') + "/");

    public void Require(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"ServiceUrls:{name} is not configured. Add it to appsettings.json or set " +
                $"ZAPCHAT_SERVICEURLS__{name.ToUpperInvariant()}.");
        }
    }
}
