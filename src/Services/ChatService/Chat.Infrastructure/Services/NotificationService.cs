using Chat.Application.Interfaces;
using System.Net.Http.Json;

namespace Chat.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;

    public NotificationService(
        HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CreateNotification(
        Guid userId,
        string title,
        string message,
        string type = "Message")
    {
        await _httpClient.PostAsJsonAsync(
            "api/notification",
            new
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type
            });
    }
}