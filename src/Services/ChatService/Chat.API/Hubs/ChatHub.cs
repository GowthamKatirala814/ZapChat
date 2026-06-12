using Chat.API.Services;
using Chat.Application.Interfaces;
using Chat.Domain.Entities;
using Chat.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Chat.API.Hubs;

[Authorize]
public class ChatHub(
    ChatDbContext context,
    PresenceTracker presenceTracker,
    INotificationService notificationService) : Hub
{
    private readonly ChatDbContext _context = context;
    private readonly PresenceTracker _presenceTracker = presenceTracker;
    private readonly INotificationService _notificationService = notificationService;

    // Helper: read the anonymous name stored in the JWT "anonymousName" claim
    private string GetAnonymousName() =>
        Context.User?.Claims
            .FirstOrDefault(x => x.Type == "anonymousName")?.Value
        ?? Context.User?.Claims
            .FirstOrDefault(x => x.Type.Contains("nameidentifier"))?.Value
        ?? "Anonymous";

    public override async Task OnConnectedAsync()
    {
        var anonymousName = GetAnonymousName();

        await _presenceTracker.UserConnected(
            Context.ConnectionId,
            anonymousName);

        var users = await _presenceTracker.GetOnlineUsers();

        await Clients.All.SendAsync("OnlineUsersUpdated", users);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _presenceTracker.UserDisconnected(Context.ConnectionId);

        var users = await _presenceTracker.GetOnlineUsers();

        await Clients.All.SendAsync("OnlineUsersUpdated", users);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomName)
    {
        var room = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == roomName);

        if (room is null)
        {
            room = new ChatRoom
            {
                Id = Guid.NewGuid(),
                Name = roomName,
                RoomType = "General"
            };

            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        await Clients.Group(roomName)
            .SendAsync("UserJoined", $"{GetAnonymousName()} joined {roomName}");
    }

    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

        await Clients.Group(roomName)
            .SendAsync("UserLeft", $"{GetAnonymousName()} left {roomName}");
    }

    public async Task SendMessage(
        string roomName,
        string message,
        string? parentMessageId = null)
    {
        var anonymousName = GetAnonymousName();

        var userId = Context.User?.Claims
            .FirstOrDefault(x => x.Type.Contains("nameidentifier"))?.Value;

        var room = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == roomName);

        if (room is null) return;

        Guid? parentId = null;
        if (!string.IsNullOrEmpty(parentMessageId)
            && Guid.TryParse(parentMessageId, out var parsedParentId))
        {
            parentId = parsedParentId;
        }

        var chatMessage = new Message
        {
            Id = Guid.NewGuid(),
            ChatRoomId = room.Id,
            AnonymousName = anonymousName,
            Content = message,
            ParentMessageId = parentId,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // Notify asynchronously — don't crash the message if notification fails
        if (parentId.HasValue && !string.IsNullOrEmpty(userId))
        {
            try
            {
                await _notificationService.CreateNotification(
                    Guid.Parse(userId),
                    "New Reply",
                    $"{anonymousName} replied in #{roomName}");
            }
            catch
            {
                // Notification failure must never break message delivery
            }
        }

        await Clients.Group(roomName)
            .SendAsync("ReceiveMessage", new
            {
                id = chatMessage.Id,
                anonymousName,
                message,
                parentMessageId = parentId,
                sentAt = chatMessage.SentAt,
                userId
            });

        // Broadcast a global notification to other users
        await Clients.Others.SendAsync("GlobalNotification", new
        {
            id = Guid.NewGuid(),
            title = $"New message in {roomName}",
            message = $"{anonymousName}: {message}",
            roomName = roomName,
            isRead = false,
            createdAt = DateTime.UtcNow
        });
    }

    public async Task Typing(string roomName)
    {
        await Clients.OthersInGroup(roomName)
            .SendAsync("UserTyping", GetAnonymousName());
    }

    public async Task StopTyping(string roomName)
    {
        await Clients.OthersInGroup(roomName)
            .SendAsync("UserStoppedTyping", GetAnonymousName());
    }

    public async Task AddReaction(
        string messageId,
        string reaction)
    {
        if (!Guid.TryParse(messageId, out var msgGuid)) return;

        var message = await _context.Messages
            .FirstOrDefaultAsync(x => x.Id == msgGuid);

        if (message is null) return;

        var anonymousName = GetAnonymousName();

        var messageReaction = new MessageReaction
        {
            Id = Guid.NewGuid(),
            MessageId = msgGuid,
            AnonymousName = anonymousName,
            Reaction = reaction
        };

        _context.MessageReactions.Add(messageReaction);
        await _context.SaveChangesAsync();

        await Clients.All.SendAsync("ReactionAdded", new
        {
            messageId = msgGuid,
            anonymousName,
            reaction
        });
    }

    public async Task<List<string>> GetOnlineUsers()
    {
        return await _presenceTracker.GetOnlineUsers();
    }
}