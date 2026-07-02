using Chat.API.Services;
using Chat.Application.DTOs;
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
    INotificationService notificationService,
    IContentModerationService moderationService,
    IHttpClientFactory httpClientFactory,
    ILogger<ChatHub> logger) : Hub
{
    private readonly ChatDbContext _context                        = context;
    private readonly PresenceTracker _presenceTracker              = presenceTracker;
    private readonly INotificationService _notificationService     = notificationService;
    private readonly IContentModerationService _moderationService  = moderationService;
    private readonly IHttpClientFactory _httpClientFactory         = httpClientFactory;
    private readonly ILogger<ChatHub> _logger                      = logger;

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

        // ── Content Moderation Gate ───────────────────────────────────────────
        // Runs BEFORE any DB write or SignalR broadcast.
        // Stage 1: fast local rules (no I/O). Stage 2: Gemini AI (only if rules pass).
        // FAIL-OPEN: Gemini unavailability logs a warning and allows the message through.
        var moderationResult = await _moderationService.ModerateAsync(new ModerationRequest
        {
            Content       = message,
            AnonymousName = anonymousName,
            RoomName      = roomName,
            RoomId        = room.Id,
            UserId        = userId
        });

        if (!moderationResult.AllowMessage)
        {
            _logger.LogWarning(
                "[ChatHub:Moderation] Message blocked. User={User} Room={Room} Category={Category} Confidence={Confidence:F2} RuleBased={IsRule}",
                anonymousName, roomName,
                moderationResult.Category,
                moderationResult.Confidence,
                moderationResult.IsRuleBasedBlock);

            // Only the SENDER receives this event — no other room members are informed.
            await Clients.Caller.SendAsync("MessageBlocked", new
            {
                category = moderationResult.Category,
                reason   = moderationResult.BlockedReason
            });
            return; // ← No save. No broadcast. Pipeline stops here.
        }
        // ─────────────────────────────────────────────────────────────────────

        var chatMessage = new Message
        {
            Id              = Guid.NewGuid(),
            ChatRoomId      = room.Id,
            AnonymousName   = anonymousName,
            Content         = message,
            ParentMessageId = parentId,
            SentAt          = DateTime.UtcNow
        };

        _context.Messages.Add(chatMessage);

        room.LastMessageAt = chatMessage.SentAt;
        room.LastMessagePreview = message;

        // Update read states for all active members of this room
        var memberDtos = new List<RoomMemberDto>();
        var existingReadStates = new List<ChatRoomReadState>();
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                var adminClient = _httpClientFactory.CreateClient("AdminService");
                var response = await adminClient.GetFromJsonAsync<List<RoomMemberDto>>($"/api/admin/rooms/{room.Id}/members");
                if (response != null)
                {
                    memberDtos = response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChatHub] Failed to fetch room members for {RoomName}", roomName);
            }

            existingReadStates = await _context.ChatRoomReadStates
                .Where(x => x.ChatRoomId == room.Id)
                .ToListAsync();

            foreach (var member in memberDtos)
            {
                var mIdStr = member.UserId.ToString();
                if (mIdStr == userId) continue;

                var rs = existingReadStates.FirstOrDefault(x => x.UserId == mIdStr);
                if (rs != null)
                {
                    rs.UnreadCount++;
                }
                else
                {
                    rs = new ChatRoomReadState
                    {
                        Id = Guid.NewGuid(),
                        ChatRoomId = room.Id,
                        UserId = mIdStr,
                        UnreadCount = 1,
                        LastReadAt = chatMessage.SentAt.AddMilliseconds(-1) // Ensure it is strictly BEFORE the message sent time
                    };
                    _context.ChatRoomReadStates.Add(rs);
                    existingReadStates.Add(rs); // Add to the tracking list so we can loop over it for RoomUpdated
                }
            }
        }

        await _context.SaveChangesAsync();

        // Emit a per-user RoomUpdated event so every connected client's sidebar
        // badge reflects the exact persisted unread count — authoritative, no guessing.
        if (!string.IsNullOrEmpty(userId))
        {
            // Sender always gets unreadCount=0 for this room (they just sent it)
            // But we send -1 to match the private chat convention (ignore update)
            await Clients.User(userId).SendAsync("RoomUpdated", new
            {
                roomName  = roomName,
                unreadCount = -1
            });

            // Every other member gets their real persisted count
            var allReadStates = await _context.ChatRoomReadStates
                .Where(x => x.ChatRoomId == room.Id && x.UserId != userId)
                .ToListAsync();

            foreach (var rs in allReadStates)
            {
                await Clients.User(rs.UserId).SendAsync("RoomUpdated", new
                {
                    roomName    = roomName,
                    unreadCount = rs.UnreadCount
                });
            }
        }

        // Notify asynchronously — don't crash the message if notification fails
        if (parentId.HasValue && !string.IsNullOrEmpty(userId))
        {
            try
            {
                await _notificationService.CreateNotification(
                    Guid.Parse(userId),
                    "New Reply",
                    $"{anonymousName} replied in #{roomName}",
                    "Reply");
            }
            catch
            {
                // Notification failure must never break message delivery
            }
        }

        // Process @mentions
        if (memberDtos.Any() && !string.IsNullOrEmpty(userId))
        {
            var mentionMatches = System.Text.RegularExpressions.Regex.Matches(message, @"@(\w+)");
            var mentionedNames = mentionMatches.Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            
            foreach (var mentionedName in mentionedNames)
            {
                var matchedMember = memberDtos.FirstOrDefault(m => string.Equals(m.AnonymousName, mentionedName, StringComparison.OrdinalIgnoreCase));
                if (matchedMember != null && matchedMember.UserId.ToString() != userId)
                {
                    try
                    {
                        await _notificationService.CreateNotification(
                            matchedMember.UserId,
                            "You were mentioned",
                            $"{anonymousName} mentioned you in #{roomName}",
                            "Mention");
                    }
                    catch
                    {
                        // Ignore notification failure
                    }
                }
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

        // Broadcast a global notification to other members ONLY
        if (memberDtos.Any())
        {
            var targetUserIds = memberDtos
                .Select(m => m.UserId.ToString())
                .Where(idStr => idStr != userId)
                .ToList();

            if (targetUserIds.Any())
            {
                await Clients.Users(targetUserIds).SendAsync("GlobalNotification", new
                {
                    id        = Guid.NewGuid(),
                    title     = $"New message in {roomName}",
                    message   = $"{anonymousName}: {message}",
                    roomName  = roomName,
                    isRead    = false,
                    createdAt = chatMessage.SentAt,
                    senderId  = userId
                });
            }
        }
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
            Id            = Guid.NewGuid(),
            MessageId     = msgGuid,
            AnonymousName = anonymousName,
            Reaction      = reaction
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

    public async Task EditMessage(string messageId, string newContent)
    {
        var anonymousName = GetAnonymousName();
        if (string.IsNullOrEmpty(anonymousName)) return;

        if (!Guid.TryParse(messageId, out var msgGuid)) return;

        var message = await _context.Messages
            .Include(m => m.ChatRoom)
            .FirstOrDefaultAsync(x => x.Id == msgGuid);

        if (message == null) return;

        // Validation Rules
        if (message.AnonymousName != anonymousName) return; // Only own messages
        if (message.IsDeleted || message.IsRemoved) return; // Cannot edit deleted/moderated
        
        // 15-minute window
        if ((DateTime.UtcNow - message.SentAt).TotalMinutes > 15) return;

        // Content Moderation Gate for Edits
        var userId = Context.UserIdentifier;
        var moderationResult = await _moderationService.ModerateAsync(new ModerationRequest
        {
            Content       = newContent,
            AnonymousName = anonymousName,
            RoomName      = message.ChatRoom.Name,
            RoomId        = message.ChatRoomId,
            UserId        = userId
        });

        if (!moderationResult.AllowMessage)
        {
            _logger.LogWarning(
                "[ChatHub:Moderation] Edit blocked. User={User} Room={Room} Category={Category} Confidence={Confidence:F2} RuleBased={IsRule}",
                anonymousName, message.ChatRoom.Name,
                moderationResult.Category,
                moderationResult.Confidence,
                moderationResult.IsRuleBasedBlock);

            await Clients.Caller.SendAsync("MessageBlocked", new
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

        await Clients.Group(message.ChatRoom.Name)
            .SendAsync("MessageEdited", new
            {
                messageId = message.Id,
                content = message.Content,
                editedAt = message.EditedAt,
                isEdited = true
            });
    }
}