namespace Chat.Application.Interfaces;

public interface INotificationService
{
    Task CreateNotification(
        Guid userId,
        string title,
        string message,
        string type = "Message");
}