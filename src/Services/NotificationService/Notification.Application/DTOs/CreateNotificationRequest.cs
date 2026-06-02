namespace Notification.API.DTOs;

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}