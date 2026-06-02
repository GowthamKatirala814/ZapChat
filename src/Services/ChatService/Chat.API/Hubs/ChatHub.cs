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

    private readonly PresenceTracker _presenceTracker =
        presenceTracker;

    private readonly INotificationService _notificationService =
        notificationService;

    public override async Task OnConnectedAsync()
    {
        var email =
            Context.User?.Claims
                .FirstOrDefault(x =>
                    x.Type.Contains("email"))
                ?.Value
            ?? "Anonymous";

        await _presenceTracker.UserConnected(
            Context.ConnectionId,
            email);

        var users =
            await _presenceTracker.GetOnlineUsers();

        await Clients.All.SendAsync(
            "OnlineUsersUpdated",
            users);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        await _presenceTracker.UserDisconnected(
            Context.ConnectionId);

        var users =
            await _presenceTracker.GetOnlineUsers();

        await Clients.All.SendAsync(
            "OnlineUsersUpdated",
            users);

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

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            roomName);

        await Clients.Group(roomName)
            .SendAsync(
                "UserJoined",
                $"A user joined {roomName}");
    }

    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            roomName);

        await Clients.Group(roomName)
            .SendAsync(
                "UserLeft",
                $"A user left {roomName}");
    }

    public async Task SendMessage(
        string roomName,
        string anonymousName,
        string message,
        Guid? parentMessageId = null)
    {
        var userEmail =
            Context.User?.Claims
                .FirstOrDefault(x =>
                    x.Type.Contains("email"))
                ?.Value;

        var userId =
            Context.User?.Claims
                .FirstOrDefault(x =>
                    x.Type.Contains("nameidentifier"))
                ?.Value;

        var room = await _context.ChatRooms
            .FirstOrDefaultAsync(x => x.Name == roomName);

        if (room is null)
            return;

        var chatMessage = new Message
        {
            Id = Guid.NewGuid(),
            ChatRoomId = room.Id,
            AnonymousName = anonymousName,
            Content = message,
            ParentMessageId = parentMessageId,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(chatMessage);

        await _context.SaveChangesAsync();

        if (parentMessageId.HasValue)
        {
            await _notificationService.CreateNotification(
                Guid.NewGuid(),
                "New Reply",
                $"{anonymousName} replied in room {roomName}");
        }

        await Clients.Group(roomName)
            .SendAsync(
                "ReceiveMessage",
                new
                {
                    Id = chatMessage.Id,
                    AnonymousName = anonymousName,
                    Message = message,
                    ParentMessageId = parentMessageId,
                    SentAt = chatMessage.SentAt,
                    UserEmail = userEmail,
                    UserId = userId
                });
    }

    public async Task Typing(
        string roomName,
        string anonymousName)
    {
        await Clients.OthersInGroup(roomName)
            .SendAsync(
                "UserTyping",
                anonymousName);
    }

    public async Task StopTyping(
        string roomName,
        string anonymousName)
    {
        await Clients.OthersInGroup(roomName)
            .SendAsync(
                "UserStoppedTyping",
                anonymousName);
    }

    public async Task AddReaction(
        Guid messageId,
        string anonymousName,
        string reaction)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(x => x.Id == messageId);

        if (message is null)
            return;

        var messageReaction = new MessageReaction
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            AnonymousName = anonymousName,
            Reaction = reaction
        };

        _context.MessageReactions.Add(
            messageReaction);

        await _context.SaveChangesAsync();

        await Clients.All.SendAsync(
            "ReactionAdded",
            new
            {
                MessageId = messageId,
                AnonymousName = anonymousName,
                Reaction = reaction
            });
    }

    public async Task<List<string>> GetOnlineUsers()
    {
        return await _presenceTracker
            .GetOnlineUsers();
    }
}