using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Chat.Domain.Documents;
using Microsoft.AspNetCore.SignalR;
using ZapChat.Shared.Realtime;

namespace Chat.API.Hubs;

/// <summary>
/// The only place SignalR is used to push room events. Implements the application
/// layer's <see cref="IChatBroadcaster"/> so business logic can publish without
/// referencing SignalR, and so both the REST controllers and the hub produce
/// identical events for the same action.
/// </summary>
public sealed class ChatBroadcaster : IChatBroadcaster
{
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<ChatBroadcaster> _logger;

    public ChatBroadcaster(IHubContext<ChatHub> hub, ILogger<ChatBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    private IClientProxy Room(Guid roomId) => _hub.Clients.Group(HubGroups.Room(roomId));

    public Task MessageReceivedAsync(
        Guid roomId, MessageDto message, IReadOnlyList<Guid> recipientUserIds) =>
        Safe(() => Room(roomId).SendAsync(HubEvents.MessageReceived, message),
            nameof(MessageReceivedAsync));

    public Task MessageEditedAsync(Guid roomId, MessageDto message) =>
        Safe(() => Room(roomId).SendAsync(HubEvents.MessageEdited, message),
            nameof(MessageEditedAsync));

    public Task MessageDeletedAsync(Guid roomId, Guid messageId, DeletionKind kind, DateTime at) =>
        Safe(() => Room(roomId).SendAsync(HubEvents.MessageDeleted, new
            {
                roomId,
                messageId,
                // "User" or "Moderation" — the client renders a different placeholder
                // for each, and the acting admin's identity is never disclosed.
                deletedBy = kind.ToString(),
                deletedAt = at
            }),
            nameof(MessageDeletedAsync));

    /// <summary>
    /// Carries the whole message so the client renders the server's reaction state
    /// rather than toggling its own copy.
    /// </summary>
    public Task ReactionsChangedAsync(Guid roomId, Guid messageId, MessageDto message) =>
        Safe(() => Room(roomId).SendAsync(HubEvents.ReactionsChanged, new
            {
                roomId, messageId, reactions = message.Reactions
            }),
            nameof(ReactionsChangedAsync));

    /// <summary>
    /// Per-user sidebar update. Uses Clients.User, which resolves through the default
    /// SignalR user-id provider (the NameIdentifier claim).
    /// </summary>
    public Task RoomUpdatedAsync(Guid userId, RoomUpdatedDto update) =>
        Safe(() => _hub.Clients.User(userId.ToString())
                .SendAsync(HubEvents.RoomUpdated, update),
            nameof(RoomUpdatedAsync));

    public Task RoomReadAsync(Guid roomId, string anonymousName, DateTime at) =>
        Safe(() => Room(roomId).SendAsync(HubEvents.RoomRead, new
            {
                roomId, anonymousName, readAt = at
            }),
            nameof(RoomReadAsync));

    public Task RoomPresenceChangedAsync(Guid roomId, IReadOnlyList<RoomMemberDto> online) =>
        Safe(() => Room(roomId).SendAsync(HubEvents.RoomPresenceChanged, new
            {
                roomId, members = online
            }),
            nameof(RoomPresenceChangedAsync));

    /// <summary>
    /// A broadcast failure must never fail the operation that caused it — the message
    /// is already persisted. Logged rather than swallowed silently.
    /// </summary>
    private async Task Safe(Func<Task> send, string operation)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR broadcast {Operation} failed.", operation);
        }
    }
}
