using System.ComponentModel.DataAnnotations;
using Notification.Domain.Documents;

namespace Notification.Application;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    Guid? SourceId,
    DateTime CreatedAt);

public sealed record UnreadCountDto(long Unread);

/// <summary>Posted by Chat and PrivateChat with a service token. Never by a browser.</summary>
public sealed class CreateNotificationRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Message;

    public Guid? SourceId { get; set; }
}

public sealed class PushSubscriptionRequest
{
    [Required, MaxLength(1000)]
    public string Endpoint { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Auth { get; set; } = string.Empty;
}

public interface INotificationRepository
{
    Task InsertAsync(NotificationDocument document, CancellationToken ct = default);

    /// <summary>Always filtered by recipient — there is no "get by id" without the owner.</summary>
    Task<NotificationDocument?> GetOwnedAsync(
        Guid id, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDocument>> ListForUserAsync(
        Guid userId, int limit, bool unreadOnly, CancellationToken ct = default);

    Task<long> CountUnreadAsync(Guid userId, CancellationToken ct = default);

    Task<bool> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<long> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>Removes notifications whose source was deleted. Service-initiated.</summary>
    Task<IReadOnlyList<NotificationDocument>> DeleteBySourceAsync(
        Guid sourceId, CancellationToken ct = default);

    Task<long> CountAsync(CancellationToken ct = default);

    Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default);
}

public interface IPushSubscriptionRepository
{
    Task UpsertAsync(PushSubscriptionDocument document, CancellationToken ct = default);
    Task<bool> DeleteAsync(string endpoint, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<PushSubscriptionDocument>> ListForUserAsync(
        Guid userId, CancellationToken ct = default);
    Task RecordFailureAsync(Guid id, CancellationToken ct = default);
    Task RetireAsync(Guid id, CancellationToken ct = default);
}

public interface INotificationBroadcaster
{
    Task NotificationReceivedAsync(Guid userId, NotificationDto notification);
    Task NotificationDeletedAsync(Guid userId, Guid notificationId);
}

/// <summary>Web push delivery. A no-op implementation is used when VAPID keys are absent.</summary>
public interface IPushDispatcher
{
    bool IsEnabled { get; }
    Task DispatchAsync(Guid userId, NotificationDto notification, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> ListMineAsync(
        int limit, bool unreadOnly, CancellationToken ct = default);

    Task<long> CountMyUnreadAsync(CancellationToken ct = default);

    Task MarkReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
    Task DeleteAsync(Guid notificationId, CancellationToken ct = default);

    Task DeleteBySourceAsync(Guid sourceId, CancellationToken ct = default);

    Task SubscribeAsync(PushSubscriptionRequest request, CancellationToken ct = default);
    Task UnsubscribeAsync(string endpoint, CancellationToken ct = default);
}
