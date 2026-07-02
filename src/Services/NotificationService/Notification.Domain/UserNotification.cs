namespace Notification.Domain.Entities;

public class UserNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = "Message"; // "Message", "Mention", "Reply"

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    /// <summary>
    /// When created from a private chat message, stores the originating
    /// message ID so we can delete this notification when the message is deleted.
    /// </summary>
    public Guid? SourceMessageId { get; set; }
}