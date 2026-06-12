using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly PrivateChatDbContext _context;

    public AdminController(PrivateChatDbContext context)
    {
        _context = context;
    }

    [HttpGet("conversations/summary")]
    public async Task<IActionResult> GetConversationsSummary()
    {
        var totalPrivateConversations = await _context.Conversations.CountAsync();
        return Ok(new { totalPrivateConversations = totalPrivateConversations });
    }

    [HttpGet("analytics/private-chat-volume")]
    public async Task<IActionResult> GetPrivateChatVolume([FromQuery] int days = 30)
    {
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        var since = DateTime.UtcNow.AddDays(-days).Date;

        var counts = await _context.Messages
            .Where(m => m.SentAt >= since)
            .GroupBy(m => m.SentAt.Date)
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
