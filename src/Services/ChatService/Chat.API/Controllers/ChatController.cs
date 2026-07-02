using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.SignalR;
using Chat.API.Hubs;
using System.Net.Http.Json;

namespace Chat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatController(
        ChatDbContext context,
        IHubContext<ChatHub> hubContext,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _hubContext = hubContext;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms([FromQuery] string? userId = null)
    {
        var query = _context.ChatRooms.AsQueryable();

        var rooms = await query
            .OrderByDescending(x => x.LastMessageAt)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.RoomType,
                x.CreatedAt,
                x.LastMessageAt,
                x.LastMessagePreview,
                UnreadCount = string.IsNullOrEmpty(userId) ? 0 : 
                    _context.ChatRoomReadStates
                        .Where(rs => rs.ChatRoomId == x.Id && rs.UserId == userId)
                        .Select(rs => rs.UnreadCount)
                        .FirstOrDefault()
            })
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
            .Include(x => x.ParentMessage)
            .Where(x => x.ChatRoomId == room.Id)
            .OrderBy(x => x.SentAt)
            .Select(x => new
            {
                x.Id,
                x.AnonymousName,
                message = (x.IsRemoved || x.IsDeleted) ? "" : x.Content,
                x.SentAt,
                x.ParentMessageId,
                ParentMessageSnippet = x.ParentMessage != null ? (x.ParentMessage.IsRemoved || x.ParentMessage.IsDeleted ? "" : x.ParentMessage.Content) : null,
                ParentMessageSenderName = x.ParentMessage != null ? x.ParentMessage.AnonymousName : null,
                x.AttachmentUrl,
                x.FileName,
                x.IsDeleted,
                x.DeletedAt,
                x.IsEdited,
                x.EditedAt,
                deletedBy = x.DeletedBy ?? (x.IsRemoved ? "Moderation" : (x.IsDeleted ? "User" : null)),
                reactions = x.Reactions.Select(r => new { r.AnonymousName, r.Reaction }).ToList()
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPut("room/{roomName}/read")]
    public async Task<IActionResult> MarkRoomAsRead(string roomName, [FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        var room = await _context.ChatRooms.FirstOrDefaultAsync(x => x.Name == roomName);
        if (room == null) return NotFound();

        var readState = await _context.ChatRoomReadStates
            .FirstOrDefaultAsync(x => x.ChatRoomId == room.Id && x.UserId == userId);

        if (readState != null)
        {
            readState.UnreadCount = 0;
            readState.LastReadAt = DateTime.UtcNow;
        }
        else
        {
            _context.ChatRoomReadStates.Add(new ChatRoomReadState
            {
                Id = Guid.NewGuid(),
                ChatRoomId = room.Id,
                UserId = userId,
                UnreadCount = 0,
                LastReadAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // Emit RoomMessageRead to the room to update seen ticks live
        await _hubContext.Clients.Group(roomName)
            .SendAsync("RoomMessageRead", new { roomName, userId, lastReadAt = DateTime.UtcNow });

        return Ok();
    }

    [HttpGet("messages/{messageId}/seen-by")]
    public async Task<IActionResult> GetMessageSeenBy(Guid messageId)
    {
        var message = await _context.Messages
            .Include(m => m.ChatRoom)
            .FirstOrDefaultAsync(x => x.Id == messageId);

        if (message == null) return NotFound();

        var adminClient = _httpClientFactory.CreateClient("AdminService");
        List<Chat.Application.DTOs.RoomMemberDto> memberDtos = new();
        try
        {
            var response = await adminClient.GetFromJsonAsync<List<Chat.Application.DTOs.RoomMemberDto>>($"/api/admin/rooms/{message.ChatRoomId}/members");
            if (response != null)
            {
                memberDtos = response;
            }
        }
        catch
        {
            // Fallback or ignore
        }

        var readStates = await _context.ChatRoomReadStates
            .Where(x => x.ChatRoomId == message.ChatRoomId)
            .ToListAsync();

        var seenByUserIds = new List<string>();

        foreach (var member in memberDtos)
        {
            var mIdStr = member.UserId.ToString();

            var rs = readStates.FirstOrDefault(x => x.UserId == mIdStr);
            if (rs != null && rs.LastReadAt >= message.SentAt)
            {
                seenByUserIds.Add(mIdStr);
            }
        }

        return Ok(seenByUserIds);
    }

}