using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure.Persistence.DbContexts;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly NotificationDbContext _context;

    public AdminController(NotificationDbContext context)
    {
        _context = context;
    }

    [HttpGet("notifications/summary")]
    public async Task<IActionResult> GetNotificationsSummary()
    {
        var totalNotifications = await _context.Notifications.CountAsync();
        return Ok(new { totalNotifications = totalNotifications });
    }

    [HttpGet("analytics/daily-notifications")]
    public async Task<IActionResult> GetDailyNotifications([FromQuery] int days = 30)
    {
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        var since = DateTime.UtcNow.AddDays(-days).Date;

        var counts = await _context.Notifications
            .Where(n => n.CreatedAt >= since)
            .GroupBy(n => n.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .ToListAsync();

        var lookup = counts.ToDictionary(x => x.date, x => x.count);

        var series = Enumerable.Range(0, days).Select(offset =>
        {
            var date = since.AddDays(offset);
            return new
            {
                date = date.ToString("yyyy-MM-dd"),
                count = lookup.TryGetValue(date, out var c) ? c : 0
            };
        });

        return Ok(series);
    }
}
