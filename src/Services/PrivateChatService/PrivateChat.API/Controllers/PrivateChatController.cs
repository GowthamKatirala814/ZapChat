using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PrivateChat.API.Hubs;
using PrivateChat.Application.DTOs;
using PrivateChat.Domain.Entities;
using PrivateChat.Domain;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrivateChatController : ControllerBase
{
    private readonly PrivateChatDbContext _context;
    private readonly IHubContext<PrivateChatHub> _hubContext;

    public PrivateChatController(
        PrivateChatDbContext context,
        IHubContext<PrivateChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
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
            .Include(x => x.ParentMessage)
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.SentAt)
            .Select(x => new
            {
                x.Id,
                x.ConversationId,
                x.SenderId,
                x.SenderName,
                Content = (x.IsRemoved || x.IsDeleted) ? "" : x.Content,
                x.SentAt,
                x.IsRead,
                x.ParentMessageId,
                ParentMessageSnippet = x.ParentMessage != null ? (x.ParentMessage.IsRemoved || x.ParentMessage.IsDeleted ? "" : x.ParentMessage.Content) : null,
                ParentMessageSenderName = x.ParentMessage != null ? x.ParentMessage.SenderName : null,
                x.AttachmentUrl,
                x.FileName,
                x.IsDeleted,
                x.DeletedAt,
                x.IsEdited,
                x.EditedAt,
                deletedBy = x.DeletedBy ?? (x.IsRemoved ? "Moderation" : (x.IsDeleted ? "User" : null)),
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

        // Emit MessageRead back to the sender
        await _hubContext.Clients.User(message.SenderId.ToString())
            .SendAsync("MessageRead", new { messageId = message.Id });

        return Ok();
    }

    /// <summary>
    /// Returns all conversations for a user with last message, unread count, and other user info.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetUserConversations([FromQuery] Guid userId)
    {
        var blockedIds = await _context.UserBlocks
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync();

        var blockedSet = new HashSet<Guid>(blockedIds);

        var conversations = await _context.Conversations
            .Where(x => x.User1Id == userId || x.User2Id == userId)
            .Select(x => new
            {
                x.Id,
                User1Id = x.User1Id,
                User2Id = x.User2Id,
                OtherUserId = x.User1Id == userId ? x.User2Id : x.User1Id,
                LastMessageAt = x.LastMessageAt,
                // Direct mapped from denormalized columns
                LastMessage = x.LastMessageAt.HasValue ? new
                {
                    Id = Guid.Empty, // Frontend doesn't strictly need real Msg ID for preview
                    Content = x.LastMessagePreview,
                    SentAt = x.LastMessageAt,
                    SenderId = Guid.Empty,
                    SenderName = "",
                    IsRead = true
                } : null,
                UnreadCount = x.User1Id == userId ? x.User1UnreadCount : x.User2UnreadCount
            })
            // Fully efficient index seek
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync();

        // Filter out blocked users
        conversations = conversations.Where(c => !blockedSet.Contains(c.OtherUserId)).ToList();

        return Ok(conversations);
    }

    [HttpPut("conversation/{otherUserId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(Guid otherUserId, [FromQuery] Guid userId)
    {
        var lo = userId < otherUserId ? userId : otherUserId;
        var hi = userId < otherUserId ? otherUserId : userId;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(x => x.User1Id == lo && x.User2Id == hi);

        if (conversation == null)
            return NotFound();

        if (conversation.User1Id == userId)
            conversation.User1UnreadCount = 0;
        else
            conversation.User2UnreadCount = 0;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("blocks")]
    public async Task<IActionResult> BlockUser([FromQuery] Guid blockerId, [FromQuery] Guid blockedId)
    {
        if (blockerId == blockedId) return BadRequest("Cannot block yourself.");

        var existing = await _context.UserBlocks.FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
        if (existing == null)
        {
            _context.UserBlocks.Add(new UserBlock { BlockerId = blockerId, BlockedId = blockedId });
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpDelete("blocks")]
    public async Task<IActionResult> UnblockUser([FromQuery] Guid blockerId, [FromQuery] Guid blockedId)
    {
        var existing = await _context.UserBlocks.FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
        if (existing != null)
        {
            _context.UserBlocks.Remove(existing);
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlockedUsers([FromQuery] Guid userId)
    {
        var blocks = await _context.UserBlocks
            .Where(b => b.BlockerId == userId)
            .Select(b => b.BlockedId)
            .ToListAsync();
        return Ok(blocks);
    }
}