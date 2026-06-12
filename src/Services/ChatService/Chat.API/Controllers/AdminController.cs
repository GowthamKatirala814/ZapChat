using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ChatDbContext _context;

    public AdminController(ChatDbContext context)
    {
        _context = context;
    }

    [HttpGet("rooms/summary")]
    public async Task<IActionResult> GetRoomsSummary()
    {
        // Count only rooms with valid names (exclude null, empty, whitespace-only)
        var totalRooms = await _context.ChatRooms
            .CountAsync(r => !string.IsNullOrWhiteSpace(r.Name));
        return Ok(new { totalRooms = totalRooms });
    }

    /// <summary>
    /// Returns all active rooms with valid names. Used by Admin Service for sync and frontend for room list.
    /// Filters out rooms with null, empty, or whitespace-only names.
    /// </summary>
    [HttpGet("rooms")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _context.ChatRooms
            .Where(r => !string.IsNullOrWhiteSpace(r.Name)) // Filter out invalid rooms
            .OrderBy(x => x.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.RoomType,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(rooms);
    }

    /// <summary>
    /// Creates a room. Called by Admin Service when admin creates a room.
    /// Validates room name is not empty and has minimum length.
    /// </summary>
    [HttpPost("rooms")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        // Validate room name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Room name cannot be empty" });
        }

        var trimmedName = request.Name.Trim();

        if (trimmedName.Length < 2)
        {
            return BadRequest(new { message = "Room name must be at least 2 characters" });
        }

        if (trimmedName.Length > 50)
        {
            return BadRequest(new { message = "Room name cannot exceed 50 characters" });
        }

        var existingRoom = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == trimmedName);

        if (existingRoom != null)
        {
            return Ok(new { id = existingRoom.Id, message = "Room already exists" });
        }

        var room = new ChatRoom
        {
            Id = request.Id,
            Name = trimmedName,
            RoomType = request.RoomType ?? "Public",
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();

        return Ok(new { id = room.Id, message = "Room created" });
    }

    /// <summary>
    /// Deletes a room. Called by Admin Service when admin deletes a room.
    /// </summary>
    [HttpDelete("rooms/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var room = await _context.ChatRooms.FindAsync(id);
        if (room == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        _context.ChatRooms.Remove(room);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Room deleted" });
    }

    public record CreateRoomRequest(Guid Id, string Name, string? RoomType);

    [HttpGet("messages/summary")]
    public async Task<IActionResult> GetMessagesSummary()
    {
        var totalMessages = await _context.Messages.CountAsync();
        return Ok(new { totalMessages = totalMessages });
    }

    [HttpGet("analytics/active-rooms")]
    public async Task<IActionResult> GetActiveRooms([FromQuery] int top = 10)
    {
        if (top < 1) top = 1;
        if (top > 100) top = 100;

        var data = await _context.Messages
            .Where(m => !m.IsRemoved)
            .GroupBy(m => m.ChatRoomId)
            .Select(g => new { roomId = g.Key, messageCount = g.Count() })
            .OrderByDescending(x => x.messageCount)
            .Take(top)
            .Join(_context.ChatRooms,
                msg => msg.roomId,
                room => room.Id,
                (msg, room) => new
                {
                    roomId = msg.roomId,
                    roomName = room.Name,
                    messageCount = msg.messageCount
                })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("analytics/active-users")]
    public async Task<IActionResult> GetActiveUsers([FromQuery] int top = 10)
    {
        if (top < 1) top = 1;
        if (top > 100) top = 100;

        var data = await _context.Messages
            .Where(m => !m.IsRemoved)
            .GroupBy(m => m.AnonymousName)
            .Select(g => new { anonymousName = g.Key, messageCount = g.Count() })
            .OrderByDescending(x => x.messageCount)
            .Take(top)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("analytics/daily-messages")]
    public async Task<IActionResult> GetDailyMessages([FromQuery] int days = 30)
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

    // ─── New Analytics Endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Chart 2 — Room Health Index.
    /// Accepts a list of reported message IDs from AdminService (which owns the Reports table).
    /// Computes per-room: messageCount, reportCount (by matching reported message IDs to room messages),
    /// reportRate, and health classification.
    /// POST body: { "reportedMessageIds": ["guid1", "guid2", ...] }
    /// </summary>
    [HttpPost("analytics/room-health")]
    public async Task<IActionResult> GetRoomHealth(
        [FromBody] RoomHealthRequest request,
        [FromQuery] int top = 10)
    {
        if (top < 1) top = 1;
        if (top > 50) top = 50;

        var reportedIds = request?.ReportedMessageIds ?? new List<Guid>();

        // Get message counts per room (non-removed messages only)
        var roomStats = await _context.Messages
            .Where(m => !m.IsRemoved)
            .GroupBy(m => m.ChatRoomId)
            .Select(g => new
            {
                roomId       = g.Key,
                messageCount = g.Count(),
                // Count how many of this room's messages are in the reported set
                reportCount  = g.Count(m => reportedIds.Contains(m.Id))
            })
            .OrderByDescending(x => x.messageCount)
            .Take(50) // fetch more than top before filtering
            .Join(_context.ChatRooms,
                msg  => msg.roomId,
                room => room.Id,
                (msg, room) => new
                {
                    roomName     = room.Name,
                    messageCount = msg.messageCount,
                    reportCount  = msg.reportCount,
                    reportRate   = msg.messageCount > 0
                        ? Math.Round((double)msg.reportCount / msg.messageCount * 100, 1)
                        : 0.0
                })
            .ToListAsync();

        var result = roomStats
            .Select(r => new
            {
                roomName     = r.roomName,
                messageCount = r.messageCount,
                reportCount  = r.reportCount,
                reportRate   = r.reportRate,
                health       = r.reportRate < 1.0 ? "Healthy"
                             : r.reportRate < 5.0 ? "Monitor"
                             :                       "Critical"
            })
            .OrderByDescending(r => r.reportRate)
            .Take(top);

        return Ok(result);
    }

    public record RoomHealthRequest(List<Guid> ReportedMessageIds);

    /// <summary>
    /// Chart 4 — Message Volume by Hour of Day.
    /// Queries all non-removed messages, groups by hour of SentAt, returns 0 for hours with no messages.
    /// </summary>
    [HttpGet("analytics/hourly-activity")]
    public async Task<IActionResult> GetHourlyActivity()
    {
        // EF Core cannot translate DateTime.Hour directly on SQL Server — load grouped data in-memory
        var hourlyCounts = await _context.Messages
            .Where(m => !m.IsRemoved)
            .Select(m => m.SentAt.Hour)
            .ToListAsync();

        var lookup = hourlyCounts
            .GroupBy(h => h)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = Enumerable.Range(0, 24).Select(h => new
        {
            hour         = h,
            messageCount = lookup.TryGetValue(h, out var c) ? c : 0
        });

        return Ok(result);
    }

    /// <summary>
    /// Chart 5 — Sentiment Distribution by Room.
    /// Applies keyword-based scoring per message, aggregates positive/neutral/negative
    /// percentages per room. Returns top rooms by message count.
    /// </summary>
    [HttpGet("analytics/room-sentiment")]
    public async Task<IActionResult> GetRoomSentiment([FromQuery] int top = 8)
    {
        if (top < 1) top = 1;
        if (top > 20) top = 20;

        // Positive keywords
        var positiveKeywords = new[]
        {
            "great", "good", "thanks", "thank", "awesome", "excellent", "helpful", "appreciate",
            "happy", "love", "perfect", "amazing", "wonderful", "fantastic", "nice", "brilliant",
            "well done", "congratulations", "solved", "fixed", "resolved", "improved", "better"
        };

        // Negative keywords
        var negativeKeywords = new[]
        {
            "issue", "problem", "error", "bug", "broken", "failed", "fail", "terrible", "awful",
            "frustrated", "frustrating", "angry", "bad", "worst", "horrible", "delayed", "delay",
            "stuck", "blocked", "wrong", "confused", "confusing", "unhappy", "disappointed",
            "concern", "worried", "stress", "stressed", "overloaded", "unfair", "complaint"
        };

        // Load top rooms by message count
        var topRoomIds = await _context.Messages
            .Where(m => !m.IsRemoved)
            .GroupBy(m => m.ChatRoomId)
            .OrderByDescending(g => g.Count())
            .Take(top)
            .Select(g => g.Key)
            .ToListAsync();

        if (!topRoomIds.Any())
            return Ok(Array.Empty<object>());

        // Load messages for those rooms (only content + roomId needed)
        var messages = await _context.Messages
            .Where(m => !m.IsRemoved && topRoomIds.Contains(m.ChatRoomId))
            .Select(m => new { m.ChatRoomId, m.Content })
            .ToListAsync();

        // Load room names
        var rooms = await _context.ChatRooms
            .Where(r => topRoomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        // Score messages per room
        var roomGroups = messages.GroupBy(m => m.ChatRoomId);

        var result = roomGroups
            .Where(g => rooms.ContainsKey(g.Key))
            .Select(g =>
            {
                int positiveCount = 0, negativeCount = 0, neutralCount = 0;

                foreach (var msg in g)
                {
                    var lower = (msg.Content ?? "").ToLowerInvariant();
                    bool hasPositive = positiveKeywords.Any(kw => lower.Contains(kw));
                    bool hasNegative = negativeKeywords.Any(kw => lower.Contains(kw));

                    if (hasPositive && !hasNegative)       positiveCount++;
                    else if (hasNegative && !hasPositive)  negativeCount++;
                    else                                    neutralCount++;
                }

                int total = positiveCount + negativeCount + neutralCount;
                if (total == 0) total = 1; // guard div-by-zero

                return new
                {
                    roomName = rooms[g.Key],
                    positive = (int)Math.Round((double)positiveCount / total * 100),
                    neutral  = (int)Math.Round((double)neutralCount  / total * 100),
                    negative = (int)Math.Round((double)negativeCount / total * 100)
                };
            })
            .OrderByDescending(r => r.positive + r.negative) // rooms with most opinions first
            .ToList();

        return Ok(result);
    }
}
