using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.API.DTOs;
using Notification.API.Hubs;
using Notification.Domain.Entities;
using Notification.Infrastructure.Persistence.DbContexts;

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
            IsRead = false,
            CreatedAt = DateTime.UtcNow
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
                createdAt = notification.CreatedAt
            });

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
}