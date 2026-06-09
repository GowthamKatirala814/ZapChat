using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatDbContext _context;

    public ChatController(ChatDbContext context)
    {
        _context = context;
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _context.ChatRooms
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom(
        ChatRoom room)
    {
        var existingRoom = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == room.Name);

        if (existingRoom is not null)
        {
            return BadRequest("Room already exists.");
        }

        room.Id = Guid.NewGuid();

        _context.ChatRooms.Add(room);

        await _context.SaveChangesAsync();

        return Ok(room);
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(
        [FromQuery] string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            return BadRequest("roomName is required.");

        var room = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == roomName);

        if (room is null)
        {
            // Room doesn't exist yet — return empty list, not 404
            return Ok(Array.Empty<object>());
        }

        var messages = await _context.Messages
            .Include(x => x.Reactions)
            .Where(x => x.ChatRoomId == room.Id)
            .OrderBy(x => x.SentAt)
            .Select(x => new
            {
                x.Id,
                x.AnonymousName,
                message = x.Content,
                x.SentAt,
                x.ParentMessageId,
                x.AttachmentUrl,
                x.FileName,
                reactions = x.Reactions.Select(r => new { r.AnonymousName, r.Reaction }).ToList()
            })
            .ToListAsync();

        return Ok(messages);
    }
}