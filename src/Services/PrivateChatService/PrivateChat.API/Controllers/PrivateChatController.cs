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

    public PrivateChatController(
        PrivateChatDbContext context)
    {
        _context = context;
    }
    [HttpPost("conversation")]
    public async Task<IActionResult> CreateConversation(
    [FromQuery] Guid user1Id,
    [FromQuery] Guid user2Id)
    {
        var conversation =
            await _context.Conversations
                .FirstOrDefaultAsync(x =>
                    (x.User1Id == user1Id &&
                     x.User2Id == user2Id)
                    ||
                    (x.User1Id == user2Id &&
                     x.User2Id == user1Id));

        if (conversation != null)
            return Ok(conversation);

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            User1Id = user1Id,
            User2Id = user2Id
        };

        _context.Conversations.Add(
            conversation);

        await _context.SaveChangesAsync();

        return Ok(conversation);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(
        SendMessageRequest request)
    {
        var message = new PrivateMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Content = request.Content,
            IsRead = false
        };

        _context.Messages.Add(message);

        await _context.SaveChangesAsync();

        return Ok(message);
    }

    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(
        Guid conversationId)
    {
        var messages =
            await _context.Messages
                .Where(x =>
                    x.ConversationId ==
                    conversationId)
                .OrderBy(x => x.SentAt)
                .ToListAsync();

        return Ok(messages);
    }

    [HttpPut("read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(
        Guid messageId)
    {
        var message =
            await _context.Messages
                .FirstOrDefaultAsync(
                    x => x.Id == messageId);

        if (message == null)
            return NotFound();

        message.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok();
    }
}