using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PrivateChat.Domain.Entities;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Hubs;

[Authorize]
public class PrivateChatHub : Hub
{
    private readonly PrivateChatDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public PrivateChatHub(
        PrivateChatDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
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

        _context.Messages.Add(privateMessage);
        await _context.SaveChangesAsync();

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

        // Push to receiver (if online)
        await Clients.User(receiverId).SendAsync("ReceivePrivateMessage", payload);

        // Notify receiver asynchronously
        try
        {
            var client = _httpClientFactory.CreateClient();
            await client.PostAsJsonAsync("http://localhost:5262/api/notification", new
            {
                UserId = Guid.Parse(receiverId),
                Title = "New Private Message",
                Message = $"{senderName} sent you a message"
            });
        }
        catch
        {
            // Ignore notification failure
        }

        // Push to sender — same event name so one handler covers both sides
        // This replaces the old "MessageSent" pattern that caused duplicates
        await Clients.User(senderId).SendAsync("ReceivePrivateMessage", payload);
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
        Console.WriteLine($"[PrivateChatHub] Connected: {Context.UserIdentifier}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[PrivateChatHub] Disconnected: {Context.UserIdentifier}");
        await base.OnDisconnectedAsync(exception);
    }
}