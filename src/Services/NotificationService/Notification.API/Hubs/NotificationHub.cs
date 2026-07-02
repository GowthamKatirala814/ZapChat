using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Notification.Infrastructure.Persistence.DbContexts;
using Notification.Domain.Entities;

namespace Notification.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(NotificationDbContext context, ILogger<NotificationHub> logger)
    {
        _context = context;
        _logger = logger;
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
        _logger.LogInformation("[NotificationHub] User connected: {UserId}", Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[NotificationHub] User disconnected: {UserId}", Context.UserIdentifier);
        if (exception is not null)
            _logger.LogError(exception, "[NotificationHub] Disconnection error for user {UserId}", Context.UserIdentifier);
        await base.OnDisconnectedAsync(exception);
    }
}
