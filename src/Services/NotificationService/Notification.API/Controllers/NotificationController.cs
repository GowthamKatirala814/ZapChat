using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.API.DTOs;
using Notification.API.Hubs;
using Notification.Domain.Entities;
using Notification.Infrastructure.Persistence.DbContexts;
using WebPush;
using System.Text.Json;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly NotificationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationController(
        NotificationDbContext context,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Called by other microservices (Chat, PrivateChat) via HTTP
    [HttpPost]
    public async Task<IActionResult> CreateNotification(
        [FromBody] CreateNotificationRequest request)
    {
        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            SourceMessageId = request.SourceMessageId
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Push real-time to the target user
        await _hubContext.Clients
            .User(request.UserId.ToString())
            .SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                sourceMessageId = notification.SourceMessageId
            });

        // Send Web Push notification
        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == request.UserId)
            .ToListAsync();

        if (subscriptions.Any())
        {
            // You would normally store these securely in configuration
            var vapidPublicKey = "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuB-3qOXGIV-kfO8wUo-iYcb9M";
            var vapidPrivateKey = "xRj_C4-b9E7M0e0T4vH7rZ6MvD4PzJ8P3-5c2D3N4P8"; // DEMO ONLY
            
            var webPushClient = new WebPushClient();
            
            foreach (var sub in subscriptions)
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                var vapidDetails = new VapidDetails("mailto:example@yourdomain.org", vapidPublicKey, vapidPrivateKey);
                var payload = JsonSerializer.Serialize(new
                {
                    title = notification.Title,
                    body = notification.Message,
                    url = "/" // Can be customized based on type
                });

                try
                {
                    await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                }
                catch (WebPushException exception)
                {
                    // If subscription is invalid/expired, remove it
                    if (exception.StatusCode == System.Net.HttpStatusCode.NotFound || 
                        exception.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        _context.PushSubscriptions.Remove(sub);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    // Ignore other errors
                }
            }
        }

        return Ok(notification);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetNotifications(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationResponse
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPut("read/{id}")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("read-all/{userId}")]
    public async Task<IActionResult> MarkAllAsRead(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        unread.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);

        if (notification == null)
            return NotFound();

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Deletes all notifications linked to a specific source message (e.g. when that message is deleted).
    /// Pushes a real-time "NotificationDeleted" event to the affected user so the UI removes the badge immediately.
    /// </summary>
    [HttpDelete("by-message/{messageId}")]
    public async Task<IActionResult> DeleteBySourceMessage(Guid messageId)
    {
        var notifications = await _context.Notifications
            .Where(x => x.SourceMessageId == messageId)
            .ToListAsync();

        if (notifications.Count == 0)
            return Ok(); // Nothing to delete — idempotent

        foreach (var n in notifications)
        {
            // Push real-time removal to the recipient
            await _hubContext.Clients
                .User(n.UserId.ToString())
                .SendAsync("NotificationDeleted", new { id = n.Id });
        }

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();

        return Ok();
    }

    public class PushSubscriptionRequest
    {
        public Guid UserId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

        if (existing == null)
        {
            _context.PushSubscriptions.Add(new Notification.Domain.Entities.PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        else if (existing.UserId != request.UserId)
        {
            existing.UserId = request.UserId;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionRequest request)
    {
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

        if (existing != null)
        {
            _context.PushSubscriptions.Remove(existing);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}