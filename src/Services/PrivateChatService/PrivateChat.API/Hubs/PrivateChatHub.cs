using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrivateChat.Application.DTOs;
using PrivateChat.Application.Interfaces;
using PrivateChat.Domain.Entities;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Hubs;

[Authorize]
public class PrivateChatHub : Hub
{
    private readonly PrivateChatDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PrivateChatHub> _logger;
    private readonly IContentModerationService _moderationService;

    public PrivateChatHub(
        PrivateChatDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<PrivateChatHub> logger,
        IContentModerationService moderationService)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _moderationService = moderationService;
    }

    /// <summary>
    /// Sends a private message.
    /// senderId is READ FROM JWT — never trusted from client.
    /// receiverId is the target user's GUID string.
    /// </summary>
    public async Task SendPrivateMessage(
        string conversationId,
        string receiverId,
        string message,
        string? parentMessageId = null)
    {
        // Always get the real sender identity from the JWT claim
        var senderId = Context.UserIdentifier;
        var senderName = Context.User?.Claims
            .FirstOrDefault(c => c.Type == "anonymousName")?.Value
            ?? "Anonymous";

        if (string.IsNullOrEmpty(senderId))
        {
            throw new HubException("Unauthorized: user identity not found.");
        }

        if (!Guid.TryParse(conversationId, out var convGuid))
        {
            throw new HubException("Invalid conversationId.");
        }

        // ── Content Moderation Gate ───────────────────────────────────────────
        var moderationResult = await _moderationService.ModerateAsync(new ModerationRequest
        {
            Content        = message,
            AnonymousName  = senderName,
            ConversationId = convGuid,
            UserId         = senderId
        });

        // ── USER BLOCK CHECK ────────────────────────────────────────────────
        var senderGuid = Guid.Parse(senderId);
        var receiverGuid = Guid.Parse(receiverId);

        var hasBlock = await _context.UserBlocks.AnyAsync(b =>
            (b.BlockerId == senderGuid && b.BlockedId == receiverGuid) ||
            (b.BlockerId == receiverGuid && b.BlockedId == senderGuid));

        if (hasBlock)
        {
            // Fail silently or notify sender
            await Clients.Caller.SendAsync("PrivateMessageBlocked", new
            {
                category = "UserBlock",
                reason = "You cannot send messages to this user."
            });
            return;
        }
        // ────────────────────────────────────────────────────────────────────

        if (!moderationResult.AllowMessage)
        {
            _logger.LogWarning(
                "[PrivateChatHub:Moderation] Message blocked. User={User} Conv={Conv} Category={Category} Confidence={Confidence:F2} RuleBased={IsRule}",
                senderName, convGuid,
                moderationResult.Category,
                moderationResult.Confidence,
                moderationResult.IsRuleBasedBlock);

            // Notify only the sender
            await Clients.Caller.SendAsync("PrivateMessageBlocked", new
            {
                category = moderationResult.Category,
                reason   = moderationResult.BlockedReason
            });
            return; // ← No save. No broadcast. Pipeline stops here.
        }
        // ─────────────────────────────────────────────────────────────────────

        var privateMessage = new PrivateMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = convGuid,
            SenderId = Guid.Parse(senderId),
            SenderName = senderName,
            Content = message,
            IsRead = false,
            SentAt = DateTime.UtcNow,
            ParentMessageId = string.IsNullOrEmpty(parentMessageId) ? null : Guid.Parse(parentMessageId)
        };

        int receiverUnreadCount = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Messages.Add(privateMessage);

            // Update denormalized columns on the conversation so list ordering
            // is always efficient (no N+1 subqueries) and survives refresh/logout.
            var conversation = await _context.Conversations.FindAsync(convGuid);
            if (conversation != null)
            {
                conversation.LastMessageAt = privateMessage.SentAt;
                conversation.LastMessagePreview = privateMessage.Content;

                if (conversation.User1Id == receiverGuid)
                {
                    conversation.User1UnreadCount++;
                    receiverUnreadCount = conversation.User1UnreadCount;
                }
                else if (conversation.User2Id == receiverGuid)
                {
                    conversation.User2UnreadCount++;
                    receiverUnreadCount = conversation.User2UnreadCount;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PrivateChatHub] Failed to save message transaction.");
            throw new HubException("Failed to send message.");
        }

        var payload = new
        {
            id = privateMessage.Id,
            conversationId = privateMessage.ConversationId,
            senderId = privateMessage.SenderId,
            senderName,
            content = message,
            sentAt = privateMessage.SentAt,
            isRead = false,
            parentMessageId = privateMessage.ParentMessageId
        };

        // Build the ConversationUpdated payload so both users can reorder their list instantly.
        // We calculate precise unread counts rather than frontend guessing.
        var receiverConvUpdatedPayload = new
        {
            conversationId = convGuid.ToString(),
            lastMessageAt = privateMessage.SentAt,
            lastMessageContent = message,
            lastMessageSenderName = senderName,
            unreadCount = receiverUnreadCount
        };

        // For sender, unread count does not change when they send a message
        var senderConvUpdatedPayload = new
        {
            conversationId = convGuid.ToString(),
            lastMessageAt = privateMessage.SentAt,
            lastMessageContent = message,
            lastMessageSenderName = senderName,
            unreadCount = -1 // frontend will ignore update if -1
        };

        // Push message to receiver (if online)
        await Clients.User(receiverId).SendAsync("ReceivePrivateMessage", payload);
        // Tell receiver's conversation list to move this chat to the top
        await Clients.User(receiverId).SendAsync("ConversationUpdated", receiverConvUpdatedPayload);

        try
        {
            var client = _httpClientFactory.CreateClient("NotificationService");
            await client.PostAsJsonAsync("api/notification", new
            {
                UserId = Guid.Parse(receiverId),
                Title = "New Private Message",
                Message = $"{senderName} sent you a message",
                SourceMessageId = privateMessage.Id,
                Type = "Message"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
        }

        // Echo message back to sender and also tell their list to reorder
        await Clients.User(senderId).SendAsync("ReceivePrivateMessage", payload);
        await Clients.User(senderId).SendAsync("ConversationUpdated", senderConvUpdatedPayload);
    }

    public async Task AddReaction(string messageId, string reaction)
    {
        var senderId = Context.UserIdentifier;
        var senderName = Context.User?.Claims
            .FirstOrDefault(c => c.Type == "anonymousName")?.Value
            ?? "Anonymous";

        if (string.IsNullOrEmpty(senderId)) return;

        if (!Guid.TryParse(messageId, out var msgGuid)) return;

        var message = await _context.Messages.FindAsync(msgGuid);
        if (message == null) return;

        var conversation = await _context.Conversations.FindAsync(message.ConversationId);
        if (conversation == null) return;

        var existingReaction = _context.MessageReactions
            .FirstOrDefault(r => r.PrivateMessageId == msgGuid && r.UserId == Guid.Parse(senderId) && r.Reaction == reaction);

        if (existingReaction != null)
        {
            _context.MessageReactions.Remove(existingReaction);
        }
        else
        {
            _context.MessageReactions.Add(new PrivateMessageReaction
            {
                Id = Guid.NewGuid(),
                PrivateMessageId = msgGuid,
                UserId = Guid.Parse(senderId),
                SenderName = senderName,
                Reaction = reaction
            });
        }

        await _context.SaveChangesAsync();

        var payload = new
        {
            messageId,
            senderName,
            reaction
        };

        // Broadcast to both users in the conversation
        await Clients.User(conversation.User1Id.ToString()).SendAsync("ReactionAdded", payload);
        await Clients.User(conversation.User2Id.ToString()).SendAsync("ReactionAdded", payload);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("[PrivateChatHub] Connected: {UserId}", Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[PrivateChatHub] Disconnected: {UserId}", Context.UserIdentifier);
        if (exception is not null)
            _logger.LogError(exception, "[PrivateChatHub] Disconnection error for {UserId}", Context.UserIdentifier);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task EditPrivateMessage(string messageId, string newContent)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        if (!Guid.TryParse(messageId, out var msgGuid)) return;

        var message = await _context.Messages
            .FirstOrDefaultAsync(x => x.Id == msgGuid);

        if (message == null) return;

        var conversation = await _context.Conversations.FindAsync(message.ConversationId);
        if (conversation == null) return;

        // Validation Rules
        if (message.SenderId.ToString() != senderId) return; // Only own messages
        if (message.IsDeleted || message.IsRemoved) return; // Cannot edit deleted/moderated
        
        // 15-minute window
        if ((DateTime.UtcNow - message.SentAt).TotalMinutes > 15) return;

        // Content Moderation Gate for Edits
        var senderName = Context.User?.Claims.FirstOrDefault(c => c.Type == "anonymousName")?.Value ?? "Anonymous";
        var moderationResult = await _moderationService.ModerateAsync(new ModerationRequest
        {
            Content       = newContent,
            AnonymousName = senderName,
            ConversationId = message.ConversationId,
            UserId        = senderId
        });

        if (!moderationResult.AllowMessage)
        {
            _logger.LogWarning(
                "[PrivateChatHub:Moderation] Edit blocked. User={User} Category={Category} Confidence={Confidence:F2} RuleBased={IsRule}",
                senderName,
                moderationResult.Category,
                moderationResult.Confidence,
                moderationResult.IsRuleBasedBlock);

            await Clients.Caller.SendAsync("PrivateMessageBlocked", new
            {
                category = moderationResult.Category,
                reason   = moderationResult.BlockedReason
            });
            return;
        }

        // Apply Edit
        message.Content = newContent;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var payload = new
        {
            messageId = message.Id,
            content = message.Content,
            editedAt = message.EditedAt,
            isEdited = true
        };

        // Broadcast to both users
        await Clients.User(conversation.User1Id.ToString()).SendAsync("MessageEdited", payload);
        await Clients.User(conversation.User2Id.ToString()).SendAsync("MessageEdited", payload);
    }
}