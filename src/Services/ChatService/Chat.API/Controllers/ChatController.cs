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

    [HttpGet("messages/{roomName}")]
    public async Task<IActionResult> GetMessages(
        string roomName)
    {
        var room = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == roomName);

        if (room is null)
        {
            return NotFound("Room not found.");
        }

        var messages = await _context.Messages
            .Where(x => x.ChatRoomId == room.Id)
            .OrderBy(x => x.SentAt)
            .ToListAsync();

        return Ok(messages);
    }
}