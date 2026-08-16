using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Notification.Application;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Realtime;

namespace Notification.API;

/// <summary>
/// Delivers notifications to the connected user.
///
/// There are deliberately NO client-callable methods. The old hub exposed
/// SendNotification(userId, title, message), which persisted a notification for any
/// user id the caller supplied. Its comment claimed only other services called it, but
/// IHubContext cannot invoke hub methods, so the only possible caller was a browser.
/// Creation now happens over an admin-only HTTP endpoint.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger) => _logger = logger;

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("Notification hub connected: {UserId}", Context.UserIdentifier);
        return base.OnConnectedAsync();
    }
}

public sealed class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<NotificationBroadcaster> _logger;

    public NotificationBroadcaster(
        IHubContext<NotificationHub> hub, ILogger<NotificationBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task NotificationReceivedAsync(Guid userId, NotificationDto notification) =>
        Safe(() => _hub.Clients.User(userId.ToString())
            .SendAsync(HubEvents.NotificationReceived, notification));

    public Task NotificationDeletedAsync(Guid userId, Guid notificationId) =>
        Safe(() => _hub.Clients.User(userId.ToString())
            .SendAsync(HubEvents.NotificationDeleted, new { id = notificationId }));

    private async Task Safe(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A notification broadcast failed.");
        }
    }
}

/// <summary>The caller's own notifications, and nobody else's.</summary>
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) =>
        _notifications = notifications;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(
        [FromQuery] int limit = 50,
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
        => Ok(await _notifications.ListMineAsync(limit, unreadOnly, ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> UnreadCount(CancellationToken ct)
        => Ok(new UnreadCountDto(await _notifications.CountMyUnreadAsync(ct)));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(notificationId, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(ct);
        return NoContent();
    }

    [HttpDelete("{notificationId:guid}")]
    public async Task<IActionResult> Delete(Guid notificationId, CancellationToken ct)
    {
        await _notifications.DeleteAsync(notificationId, ct);
        return NoContent();
    }

    [HttpPost("push/subscribe")]
    public async Task<IActionResult> Subscribe(
        [FromBody] PushSubscriptionRequest request, CancellationToken ct)
    {
        await _notifications.SubscribeAsync(request, ct);
        return NoContent();
    }

    [HttpPost("push/unsubscribe")]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] PushSubscriptionRequest request, CancellationToken ct)
    {
        await _notifications.UnsubscribeAsync(request.Endpoint, ct);
        return NoContent();
    }
}

/// <summary>
/// Called by Chat and PrivateChat with a service token. Requires the Admin role, so a
/// browser cannot reach it.
/// </summary>
[ApiController]
[Route("api/notifications/internal")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class InternalNotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public InternalNotificationsController(INotificationService notifications) =>
        _notifications = notifications;

    [HttpPost]
    public async Task<ActionResult<NotificationDto>> Create(
        [FromBody] CreateNotificationRequest request, CancellationToken ct)
        => Ok(await _notifications.CreateAsync(request, ct));

    [HttpDelete("by-source/{sourceId:guid}")]
    public async Task<IActionResult> DeleteBySource(Guid sourceId, CancellationToken ct)
    {
        await _notifications.DeleteBySourceAsync(sourceId, ct);
        return NoContent();
    }
}

/// <summary>Notification analytics for the admin dashboard.</summary>
[ApiController]
[Route("api/notification-admin")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class NotificationAdminController : ControllerBase
{
    private readonly INotificationRepository _notifications;

    public NotificationAdminController(INotificationRepository notifications) =>
        _notifications = notifications;

    [HttpGet("analytics/summary")]
    public async Task<ActionResult<object>> Summary(CancellationToken ct)
        => Ok(new { totalNotifications = await _notifications.CountAsync(ct) });

    [HttpGet("analytics/per-day")]
    public async Task<ActionResult<object>> PerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var counts = (await _notifications.CountByDayAsync(days, ct))
            .ToDictionary(x => x.Day.Date, x => x.Count);

        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        return Ok(Enumerable.Range(0, Math.Clamp(days, 1, 365)).Select(offset =>
        {
            var day = since.AddDays(offset);
            return new { date = day.ToString("yyyy-MM-dd"), count = counts.GetValueOrDefault(day) };
        }));
    }
}
