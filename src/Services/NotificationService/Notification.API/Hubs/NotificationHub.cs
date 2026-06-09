using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Notification.Infrastructure.Persistence.DbContexts;
using Notification.Domain.Entities;

namespace Notification.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly NotificationDbContext _context;

    public NotificationHub(NotificationDbContext context)
    {
        _context = context;
    }

    // Called by other services via IHubContext — persists + pushes to specific user
    public async Task SendNotification(
        string userId,
        string title,
        string message)
    {
        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(userId),
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Push to the specific user's connections only
        await Clients.User(userId)
            .SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt
            });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine(
            $"[NotificationHub] User connected: {userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine(
            $"[NotificationHub] User disconnected: {userId}");
        await base.OnDisconnectedAsync(exception);
    }
}
