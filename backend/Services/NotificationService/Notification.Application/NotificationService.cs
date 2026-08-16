using Microsoft.Extensions.Logging;
using Notification.Domain.Documents;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Notification.Application;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly INotificationBroadcaster _broadcaster;
    private readonly IPushDispatcher _push;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notifications,
        IPushSubscriptionRepository subscriptions,
        INotificationBroadcaster broadcaster,
        IPushDispatcher push,
        ICurrentUser currentUser,
        ILogger<NotificationService> logger)
    {
        _notifications = notifications;
        _subscriptions = subscriptions;
        _broadcaster = broadcaster;
        _push = push;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Creates a notification for a recipient. Only reachable with the Admin role, which
    /// service tokens carry — a browser cannot forge notifications for other users, as
    /// the old anonymous POST /api/notification allowed.
    /// </summary>
    public async Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request, CancellationToken ct = default)
    {
        var document = new NotificationDocument
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            SourceId = request.SourceId,
            CreatedAt = DateTime.UtcNow
        };

        await _notifications.InsertAsync(document, ct);

        var dto = ToDto(document);

        // In-app first: it is the reliable channel. Push is best-effort.
        await _broadcaster.NotificationReceivedAsync(request.UserId, dto);

        if (_push.IsEnabled)
            await _push.DispatchAsync(request.UserId, dto, ct);

        return dto;
    }

    public async Task<IReadOnlyList<NotificationDto>> ListMineAsync(
        int limit, bool unreadOnly, CancellationToken ct = default)
    {
        // Scoped to the caller. The old route took the user id from the URL with no
        // authorization, so anyone could read anyone's notification history — which
        // disclosed who was messaging whom.
        var userId = _currentUser.RequireUserId();

        var documents = await _notifications.ListForUserAsync(userId, limit, unreadOnly, ct);

        return documents.Select(ToDto).ToList();
    }

    public Task<long> CountMyUnreadAsync(CancellationToken ct = default) =>
        _notifications.CountUnreadAsync(_currentUser.RequireUserId(), ct);

    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!await _notifications.MarkReadAsync(notificationId, _currentUser.RequireUserId(), ct))
            throw new NotFoundException("That notification does not exist.");
    }

    public Task MarkAllReadAsync(CancellationToken ct = default) =>
        _notifications.MarkAllReadAsync(_currentUser.RequireUserId(), ct);

    public async Task DeleteAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!await _notifications.DeleteAsync(notificationId, _currentUser.RequireUserId(), ct))
            throw new NotFoundException("That notification does not exist.");
    }

    /// <summary>
    /// Withdraws notifications whose source was deleted, and tells each recipient so the
    /// badge clears immediately. Service-initiated.
    /// </summary>
    public async Task DeleteBySourceAsync(Guid sourceId, CancellationToken ct = default)
    {
        var affected = await _notifications.DeleteBySourceAsync(sourceId, ct);

        foreach (var notification in affected)
        {
            await _broadcaster.NotificationDeletedAsync(notification.UserId, notification.Id);
        }

        if (affected.Count > 0)
        {
            _logger.LogInformation(
                "Withdrew {Count} notification(s) for deleted source {SourceId}.",
                affected.Count, sourceId);
        }
    }

    public async Task SubscribeAsync(
        PushSubscriptionRequest request, CancellationToken ct = default)
    {
        if (!_push.IsEnabled)
        {
            // Told plainly rather than accepted and silently ignored.
            throw new DependencyUnavailableException(
                "Push notifications are not configured on this server.");
        }

        await _subscriptions.UpsertAsync(new PushSubscriptionDocument
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.RequireUserId(),
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth
        }, ct);
    }

    public Task UnsubscribeAsync(string endpoint, CancellationToken ct = default) =>
        _subscriptions.DeleteAsync(endpoint, _currentUser.RequireUserId(), ct);

    private static NotificationDto ToDto(NotificationDocument n) => new(
        n.Id, n.Title, n.Message, n.Type, n.IsRead, n.SourceId, n.CreatedAt);
}
