using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivateChat.Application.DTOs;
using PrivateChat.Domain.Entities;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrivateChatController : ControllerBase
{
    private readonly PrivateChatDbContext _context;

    public PrivateChatController(PrivateChatDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a conversation between two users or returns existing one.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    [HttpPost("conversation")]
    public async Task<IActionResult> CreateConversation(
        [FromQuery] Guid user1Id,
        [FromQuery] Guid user2Id)
    {
        // Normalise order so (A,B) and (B,A) always find the same conversation
        var lo = user1Id < user2Id ? user1Id : user2Id;
        var hi = user1Id < user2Id ? user2Id : user1Id;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(x =>
                x.User1Id == lo && x.User2Id == hi);

        if (conversation != null)
            return Ok(conversation);

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            User1Id = lo,
            User2Id = hi
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return Ok(conversation);
    }

    /// <summary>
    /// Returns all messages for a conversation, ordered oldest-first.
    /// </summary>
    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(Guid conversationId)
    {
        var messages = await _context.Messages
            .Include(x => x.Reactions)
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.SentAt)
            .Select(x => new
            {
                x.Id,
                x.ConversationId,
                x.SenderId,
                x.SenderName,
                x.Content,
                x.SentAt,
                x.IsRead,
                x.ParentMessageId,
                x.AttachmentUrl,
                x.FileName,
                reactions = x.Reactions.Select(r => new { r.SenderName, r.Reaction }).ToList()
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPut("read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(Guid messageId)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(x => x.Id == messageId);

        if (message == null)
            return NotFound();

        message.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok();
    }
}