using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poll.Infrastructure.Persistence.DbContexts;

namespace Poll.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly PollDbContext _context;

    public AdminController(PollDbContext context)
    {
        _context = context;
    }

    [HttpGet("polls/summary")]
    public async Task<IActionResult> GetPollsSummary()
    {
        var totalPolls = await _context.Polls.CountAsync();
        return Ok(new { totalPolls = totalPolls });
    }

    [HttpGet("analytics/daily-polls")]
    public async Task<IActionResult> GetDailyPolls([FromQuery] int days = 30)
    {
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        var since = DateTime.UtcNow.AddDays(-days).Date;

        var counts = await _context.Polls
            .Where(p => p.CreatedAt >= since)
            .GroupBy(p => p.CreatedAt.Date)
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

    [HttpGet("analytics/most-voted-polls")]
    public async Task<IActionResult> GetMostVotedPolls([FromQuery] int top = 10)
    {
        if (top < 1) top = 1;
        if (top > 100) top = 100;

        var data = await _context.Polls
            .Include(p => p.Options)
            .OrderByDescending(p => p.Options.Sum(o => o.VoteCount))
            .Take(top)
            .Select(p => new
            {
                pollId = p.Id,
                question = p.Question,
                totalVotes = p.Options.Sum(o => o.VoteCount),
                createdAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(data);
    }
}
