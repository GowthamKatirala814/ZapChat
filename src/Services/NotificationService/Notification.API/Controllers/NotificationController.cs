using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notification.API.DTOs;
using Notification.Domain.Entities;
using Notification.Infrastructure.Persistence.DbContexts;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly NotificationDbContext _context;

    public NotificationController(
        NotificationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification(
        CreateNotificationRequest request)
    {
        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            IsRead = false
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        return Ok(notification);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetNotifications(
        Guid userId)
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
    public async Task<IActionResult> MarkAsRead(
        Guid id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok();
    }
}