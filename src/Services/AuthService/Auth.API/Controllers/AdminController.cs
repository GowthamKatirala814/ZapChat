using Auth.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminController(AuthDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("users/summary")]
    public async Task<IActionResult> GetUsersSummary()
    {
        var totalUsers = await _context.Users.CountAsync();
        return Ok(new
        {
            totalUsers = totalUsers,
            activeUsers = 0,
            blockedUsers = 0
        });
    }

    [HttpGet("analytics/user-growth")]
    public async Task<IActionResult> GetUserGrowth([FromQuery] int days = 30)
    {
        if (days < 1) days = 1;
        if (days > 365) days = 365;

        var since = DateTime.UtcNow.AddDays(-days).Date;

        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        var query = _context.Users.Where(u => u.CreatedAt >= since);

        if (!string.IsNullOrWhiteSpace(adminEmail))
            query = query.Where(u => u.Email != adminEmail);

        var counts = await query
            .GroupBy(u => u.CreatedAt.Date)
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
