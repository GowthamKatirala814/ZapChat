namespace Admin.Infrastructure.Configuration;

/// <summary>
/// Strongly typed configuration section for downstream service base URLs.
/// Bound from appsettings.json under the "ServiceUrls" key.
/// URL values can be changed without any code changes.
/// </summary>
public class ServiceUrlsOptions
{
    /// <summary>
    /// The configuration section key used for binding.
    /// </summary>
    public const string SectionName = "ServiceUrls";

    /// <summary>
    /// Base URL of the Auth Service.
    /// Example: http://localhost:5204
    /// Used by: DashboardService, UserManagementService
    /// </summary>
    public string AuthService { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the Chat Service.
    /// Example: http://localhost:5139
    /// Used by: DashboardService, ModerationBackgroundService
    /// </summary>
    public string ChatService { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the PrivateChat Service.
    /// Example: http://localhost:5172
    /// Used by: DashboardService, ModerationBackgroundService
    /// </summary>
    public string PrivateChatService { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the Poll Service.
    /// Example: http://localhost:5205
    /// Used by: DashboardService
    /// </summary>
    public string PollService { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the Notification Service.
    /// Example: http://localhost:5206
    /// Used by: DashboardService
    /// </summary>
    public string NotificationService { get; set; } = string.Empty;
}
