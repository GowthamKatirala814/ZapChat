namespace Notification.API.DTOs;

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>If set, links this notification to a specific private message.</summary>
    public Guid? SourceMessageId { get; set; }

    public string Type { get; set; } = "Message";
}