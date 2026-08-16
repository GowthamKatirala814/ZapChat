using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PrivateChat.Application;
using PrivateChat.Domain.Documents;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Realtime;
using ZapChat.Shared.Results;

namespace PrivateChat.API;

// ══════════════════════════════════════════════════════════════════════════════
//  Hub
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Realtime direct messaging. Thin by design: it delegates to
/// <see cref="IConversationService"/>, which performs the participant check on every
/// operation, so the hub and REST paths enforce identical rules.
/// </summary>
[Authorize]
public sealed class PrivateChatHub : Hub
{
    private readonly IConversationService _conversations;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PrivateChatHub> _logger;

    public PrivateChatHub(
        IConversationService conversations,
        ICurrentUser currentUser,
        ILogger<PrivateChatHub> logger)
    {
        _conversations = conversations;
        _currentUser = currentUser;
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("Private chat connected: {UserId}", Context.UserIdentifier);
        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Joins the conversation's group. Access is verified first, so a client cannot
    /// subscribe to a conversation it is not part of.
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        await _conversations.RequireParticipantAsync(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Conversation(conversationId));
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Conversation(conversationId));

    /// <summary>
    /// Sends a message. Only the conversation id is accepted — the recipient is derived
    /// from the stored conversation, so a message cannot be injected elsewhere.
    /// </summary>
    public Task<DirectMessageDto> SendMessage(
        Guid conversationId, SendDirectMessageRequest request) =>
        _conversations.SendAsync(conversationId, request);

    public Task<DirectMessageDto> EditMessage(Guid messageId, EditDirectMessageRequest request) =>
        _conversations.EditAsync(messageId, request);

    public Task DeleteMessage(Guid messageId) => _conversations.DeleteAsync(messageId);

    public Task<DirectMessageDto> ToggleReaction(Guid messageId, string emoji) =>
        _conversations.ToggleReactionAsync(messageId, emoji);

    public Task MarkRead(Guid conversationId) => _conversations.MarkReadAsync(conversationId);

    public async Task StartTyping(Guid conversationId)
    {
        await _conversations.RequireParticipantAsync(conversationId);

        await Clients.OthersInGroup(HubGroups.Conversation(conversationId))
            .SendAsync(HubEvents.UserTyping, new
            {
                conversationId, anonymousName = _currentUser.AnonymousName
            });
    }

    public async Task StopTyping(Guid conversationId)
    {
        await _conversations.RequireParticipantAsync(conversationId);

        await Clients.OthersInGroup(HubGroups.Conversation(conversationId))
            .SendAsync(HubEvents.UserStoppedTyping, new
            {
                conversationId, anonymousName = _currentUser.AnonymousName
            });
    }
}

/// <summary>
/// Pushes private-chat events. Targets individual users rather than a broadcast, so a
/// conversation's contents only reach its two participants.
/// </summary>
public sealed class PrivateChatBroadcaster : IPrivateChatBroadcaster
{
    private readonly IHubContext<PrivateChatHub> _hub;
    private readonly ILogger<PrivateChatBroadcaster> _logger;

    public PrivateChatBroadcaster(
        IHubContext<PrivateChatHub> hub, ILogger<PrivateChatBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    private IClientProxy Users(Guid[] userIds) =>
        _hub.Clients.Users(userIds.Select(u => u.ToString()).ToList());

    public Task MessageReceivedAsync(
        Guid conversationId, DirectMessageDto message, Guid[] recipients) =>
        Safe(() => Users(recipients).SendAsync(HubEvents.PrivateMessageReceived, message));

    public Task MessageEditedAsync(DirectMessageDto message, Guid[] recipients) =>
        Safe(() => Users(recipients).SendAsync(HubEvents.MessageEdited, message));

    public Task MessageDeletedAsync(
        Guid conversationId, Guid messageId, DeletionKind kind, DateTime at, Guid[] recipients) =>
        Safe(() => Users(recipients).SendAsync(HubEvents.MessageDeleted, new
        {
            conversationId, messageId, deletedBy = kind.ToString(), deletedAt = at
        }));

    public Task ReactionsChangedAsync(DirectMessageDto message, Guid[] recipients) =>
        Safe(() => Users(recipients).SendAsync(HubEvents.ReactionsChanged, new
        {
            conversationId = message.ConversationId,
            messageId = message.Id,
            reactions = message.Reactions
        }));

    public Task ConversationUpdatedAsync(Guid userId, ConversationUpdatedDto update) =>
        Safe(() => _hub.Clients.User(userId.ToString())
            .SendAsync(HubEvents.ConversationUpdated, update));

    /// <summary>Tells the sender their messages were read, so the tick updates.</summary>
    public Task MessagesReadAsync(
        Guid conversationId, IReadOnlyList<Guid> messageIds, Guid senderUserId) =>
        Safe(() => _hub.Clients.User(senderUserId.ToString())
            .SendAsync(HubEvents.PrivateMessageRead, new
            {
                conversationId, messageIds, readAt = DateTime.UtcNow
            }));

    private async Task Safe(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A private chat broadcast failed.");
        }
    }
}

// ══════════════════════════════════════════════════════════════════════════════
//  Controllers
// ══════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversations;

    public ConversationsController(IConversationService conversations) =>
        _conversations = conversations;

    /// <summary>The caller's own conversations. Never anyone else's.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> List(CancellationToken ct)
        => Ok(await _conversations.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Start(
        [FromBody] StartConversationRequest request, CancellationToken ct)
        => Ok(await _conversations.StartAsync(request.OtherUserId, ct));

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ConversationDto>> Get(Guid conversationId, CancellationToken ct)
        => Ok(await _conversations.GetAsync(conversationId, ct));

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<CursorPage<DirectMessageDto>>> Messages(
        Guid conversationId,
        [FromQuery] string? before,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
        => Ok(await _conversations.GetHistoryAsync(conversationId, before, limit, ct));

    [HttpPost("{conversationId:guid}/messages")]
    public async Task<ActionResult<DirectMessageDto>> Send(
        Guid conversationId, [FromBody] SendDirectMessageRequest request, CancellationToken ct)
        => Ok(await _conversations.SendAsync(conversationId, request, ct));

    [HttpPost("{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId, CancellationToken ct)
    {
        await _conversations.MarkReadAsync(conversationId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/direct-messages")]
public sealed class DirectMessagesController : ControllerBase
{
    private readonly IConversationService _conversations;

    public DirectMessagesController(IConversationService conversations) =>
        _conversations = conversations;

    [HttpPut("{messageId:guid}")]
    public async Task<ActionResult<DirectMessageDto>> Edit(
        Guid messageId, [FromBody] EditDirectMessageRequest request, CancellationToken ct)
        => Ok(await _conversations.EditAsync(messageId, request, ct));

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid messageId, CancellationToken ct)
    {
        await _conversations.DeleteAsync(messageId, ct);
        return NoContent();
    }

    [HttpPost("{messageId:guid}/reactions")]
    public async Task<ActionResult<DirectMessageDto>> React(
        Guid messageId, [FromBody] ReactRequest request, CancellationToken ct)
        => Ok(await _conversations.ToggleReactionAsync(messageId, request.Emoji, ct));
}

/// <summary>Blocking. The blocker is always the authenticated caller.</summary>
[ApiController]
[Route("api/blocks")]
public sealed class BlocksController : ControllerBase
{
    private readonly IConversationService _conversations;

    public BlocksController(IConversationService conversations) => _conversations = conversations;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Guid>>> List(CancellationToken ct)
        => Ok(await _conversations.ListBlockedAsync(ct));

    [HttpPost("{otherUserId:guid}")]
    public async Task<IActionResult> Block(Guid otherUserId, CancellationToken ct)
    {
        await _conversations.BlockAsync(otherUserId, ct);
        return NoContent();
    }

    [HttpDelete("{otherUserId:guid}")]
    public async Task<IActionResult> Unblock(Guid otherUserId, CancellationToken ct)
    {
        await _conversations.UnblockAsync(otherUserId, ct);
        return NoContent();
    }
}

/// <summary>
/// Moderation and analytics on private chat. Routed under /api/privatechat-admin, not
/// /api/admin, so it does not collide with the admin service's route prefix.
/// </summary>
[ApiController]
[Route("api/privatechat-admin")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class PrivateChatAdminController : ControllerBase
{
    private readonly IConversationService _conversations;
    private readonly IConversationRepository _conversationRepository;
    private readonly IDirectMessageRepository _messageRepository;
    private readonly IModerationEventRepository _moderationEvents;

    public PrivateChatAdminController(
        IConversationService conversations,
        IConversationRepository conversationRepository,
        IDirectMessageRepository messageRepository,
        IModerationEventRepository moderationEvents)
    {
        _conversations = conversations;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _moderationEvents = moderationEvents;
    }

    public sealed class RemoveMessageRequest
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Removes a private message. Note that admins can remove a message they cannot
    /// read: the moderation path deliberately does not grant read access to private
    /// conversations, only the ability to act on a reported message id.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> RemoveMessage(
        Guid messageId, [FromBody] RemoveMessageRequest request, CancellationToken ct)
    {
        await _conversations.ModerationDeleteAsync(messageId, request.Reason, ct);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/remove-messages")]
    public async Task<ActionResult<object>> RemoveAllBySender(
        Guid userId, [FromBody] RemoveMessageRequest request, CancellationToken ct)
        => Ok(new
        {
            removed = await _conversations.ModerationDeleteAllBySenderAsync(userId, request.Reason, ct)
        });

    [HttpGet("analytics/summary")]
    public async Task<ActionResult<object>> Summary(CancellationToken ct) => Ok(new
    {
        totalConversations = await _conversationRepository.CountAsync(ct),
        totalMessages = await _messageRepository.CountAsync(ct)
    });

    [HttpGet("analytics/messages-per-day")]
    public async Task<ActionResult<object>> PerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var counts = (await _messageRepository.CountByDayAsync(days, ct))
            .ToDictionary(x => x.Day.Date, x => x.Count);

        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        return Ok(Enumerable.Range(0, Math.Clamp(days, 1, 365)).Select(offset =>
        {
            var day = since.AddDays(offset);
            return new { date = day.ToString("yyyy-MM-dd"), count = counts.GetValueOrDefault(day) };
        }));
    }

    [HttpGet("moderation/stats")]
    public async Task<ActionResult<PrivateModerationStats>> ModerationStats(CancellationToken ct)
        => Ok(await _moderationEvents.GetStatsAsync(ct));
}

// ══════════════════════════════════════════════════════════════════════════════
//  Infrastructure adapters
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Resolves anonymous names from Auth using a service token.</summary>
public sealed class UserDirectory : IUserDirectory
{
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<UserDirectory> _logger;

    public UserDirectory(IHttpClientFactory httpClients, ILogger<UserDirectory> logger)
    {
        _httpClients = httpClients;
        _logger = logger;
    }

    private sealed record PublicUser(Guid Id, string AnonymousName);

    public async Task<string?> GetAnonymousNameAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Auth);

            if (client.BaseAddress is null)
            {
                _logger.LogWarning("ServiceUrls:AuthService is not configured.");
                return null;
            }

            var response = await client.PostAsJsonAsync(
                "api/auth/internal/resolve", new[] { userId }, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Auth returned {Status} resolving user {UserId}.",
                    (int)response.StatusCode, userId);
                return null;
            }

            var users = await response.Content.ReadFromJsonAsync<List<PublicUser>>(ct);
            return users?.FirstOrDefault()?.AnonymousName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve user {UserId}.", userId);
            return null;
        }
    }
}

public sealed class NotificationSender : INotificationSender
{
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(IHttpClientFactory httpClients, ILogger<NotificationSender> logger)
    {
        _httpClients = httpClients;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid userId, string title, string message, string type,
        Guid? sourceId = null, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Notification);

            if (client.BaseAddress is null)
            {
                _logger.LogWarning(
                    "ServiceUrls:NotificationService is not configured; dropped a notification for {UserId}.",
                    userId);
                return;
            }

            var response = await client.PostAsJsonAsync("api/notifications/internal", new
            {
                userId, title, message, type, sourceId
            }, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Notification service returned {Status} for {UserId}.",
                    (int)response.StatusCode, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not deliver a notification to {UserId}.", userId);
        }
    }

    public async Task DeleteBySourceAsync(Guid sourceId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Notification);
            if (client.BaseAddress is null) return;

            await client.DeleteAsync($"api/notifications/internal/by-source/{sourceId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not remove notifications for source {SourceId}.", sourceId);
        }
    }
}
