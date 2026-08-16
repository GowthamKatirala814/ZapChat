using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Realtime;

namespace Chat.API.Hubs;

/// <summary>
/// Realtime room chat.
///
/// The hub is deliberately thin: it authenticates, delegates to the application
/// service, and manages group membership. Every business rule — access control,
/// moderation, persistence, unread fan-out, notifications — lives in
/// <see cref="IMessageService"/> and <see cref="IRoomService"/>, so the REST and
/// realtime paths cannot diverge. The old hub carried 220 lines of that logic
/// inline, with a second copy of parts of it in the controllers.
///
/// Groups are keyed by room id rather than room name, so renaming a room no longer
/// orphans every connection subscribed to it.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IRoomService _rooms;
    private readonly IMessageService _messages;
    private readonly IPresenceRepository _presence;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IRoomService rooms,
        IMessageService messages,
        IPresenceRepository presence,
        ICurrentUser currentUser,
        ILogger<ChatHub> logger)
    {
        _rooms = rooms;
        _messages = messages;
        _presence = presence;
        _currentUser = currentUser;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.UserId;

        if (userId is null)
        {
            // Should be unreachable given [Authorize], but never register presence
            // for an unidentified connection.
            Context.Abort();
            return;
        }

        await _presence.ConnectAsync(
            Context.ConnectionId, userId.Value, _currentUser.AnonymousName);

        _logger.LogDebug(
            "Connection {ConnectionId} opened for {AnonymousName}.",
            Context.ConnectionId, _currentUser.AnonymousName);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Read the rooms first so presence can be re-broadcast after removal.
        var rooms = await _presence.GetRoomsForConnectionAsync(Context.ConnectionId);

        await _presence.DisconnectAsync(Context.ConnectionId);

        foreach (var roomId in rooms)
            await PublishPresenceAsync(roomId);

        if (exception is not null)
        {
            _logger.LogWarning(exception,
                "Connection {ConnectionId} dropped with an error.", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes to a room. Access is re-checked here — a client cannot join a group
    /// it has no right to read. The old hub created any room a client named.
    /// </summary>
    public async Task<RoomDto> JoinRoom(Guid roomId)
    {
        var room = await _rooms.JoinAsync(roomId);

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Room(roomId));
        await _presence.JoinRoomAsync(Context.ConnectionId, roomId);
        await PublishPresenceAsync(roomId);

        await Clients.OthersInGroup(HubGroups.Room(roomId))
            .SendAsync(HubEvents.UserJoined, new { roomId, anonymousName = _currentUser.AnonymousName });

        return room;
    }

    public async Task LeaveRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Room(roomId));
        await _presence.LeaveRoomAsync(Context.ConnectionId, roomId);
        await PublishPresenceAsync(roomId);

        await Clients.OthersInGroup(HubGroups.Room(roomId))
            .SendAsync(HubEvents.UserLeft, new { roomId, anonymousName = _currentUser.AnonymousName });
    }

    /// <summary>
    /// Sends a message. Returns the stored message to the caller so the sender's UI
    /// can reconcile immediately; every other member receives it via the broadcast the
    /// service performs.
    /// </summary>
    public Task<MessageDto> SendMessage(Guid roomId, SendMessageRequest request) =>
        _messages.SendAsync(roomId, request);

    public Task<MessageDto> EditMessage(Guid messageId, EditMessageRequest request) =>
        _messages.EditAsync(messageId, request);

    public Task DeleteMessage(Guid messageId) => _messages.DeleteAsync(messageId);

    public Task<MessageDto> ToggleReaction(Guid messageId, string emoji) =>
        _messages.ToggleReactionAsync(messageId, emoji);

    public async Task MarkRead(Guid roomId)
    {
        await _rooms.MarkReadAsync(roomId);

        // Tell the room so read ticks update live. Carries the anonymous name and the
        // timestamp — the old event sent {roomName,userId,lastReadAt} while the client
        // read data.messageId, so nothing ever matched.
        await Clients.OthersInGroup(HubGroups.Room(roomId))
            .SendAsync(HubEvents.RoomRead, new
            {
                roomId,
                anonymousName = _currentUser.AnonymousName,
                readAt = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Typing indicator. Carries the room id so a client in several rooms can tell
    /// which one the event belongs to — the old event sent only a bare name.
    /// </summary>
    public async Task StartTyping(Guid roomId)
    {
        await _presence.HeartbeatAsync(Context.ConnectionId);

        await Clients.OthersInGroup(HubGroups.Room(roomId))
            .SendAsync(HubEvents.UserTyping, new
            {
                roomId, anonymousName = _currentUser.AnonymousName
            });
    }

    public Task StopTyping(Guid roomId) =>
        Clients.OthersInGroup(HubGroups.Room(roomId))
            .SendAsync(HubEvents.UserStoppedTyping, new
            {
                roomId, anonymousName = _currentUser.AnonymousName
            });

    /// <summary>Keeps this connection's presence TTL alive.</summary>
    public Task Heartbeat() => _presence.HeartbeatAsync(Context.ConnectionId);

    public Task<IReadOnlyList<RoomMemberDto>> GetRoomPresence(Guid roomId) =>
        _rooms.GetMembersAsync(roomId);

    /// <summary>
    /// Publishes the room's online list. Scoped to the room group — presence used to
    /// go to Clients.All as one flat platform-wide list of names.
    /// </summary>
    private async Task PublishPresenceAsync(Guid roomId)
    {
        try
        {
            var members = await _rooms.GetMembersAsync(roomId);

            await Clients.Group(HubGroups.Room(roomId))
                .SendAsync(HubEvents.RoomPresenceChanged, new { roomId, members });
        }
        catch (Exception ex)
        {
            // Presence is cosmetic; never fail a connect or disconnect over it.
            _logger.LogWarning(ex, "Could not publish presence for room {RoomId}.", roomId);
        }
    }
}
