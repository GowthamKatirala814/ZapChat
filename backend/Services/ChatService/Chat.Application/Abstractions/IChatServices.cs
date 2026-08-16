using Chat.Application.DTOs;
using Chat.Domain.Documents;
using ZapChat.Shared.Results;

namespace Chat.Application.Abstractions;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetVisibleRoomsAsync(CancellationToken ct = default);

    Task<RoomDto> GetRoomAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>
    /// Joins the caller. Enforces the room's access rule — a branch room requires a
    /// matching branch claim.
    /// </summary>
    Task<RoomDto> JoinAsync(Guid roomId, CancellationToken ct = default);

    Task LeaveAsync(Guid roomId, CancellationToken ct = default);

    Task MarkReadAsync(Guid roomId, CancellationToken ct = default);

    Task<IReadOnlyList<RoomMemberDto>> GetMembersAsync(Guid roomId, CancellationToken ct = default);

    Task<IReadOnlyList<ReadReceiptDto>> GetReadReceiptsAsync(
        Guid messageId, CancellationToken ct = default);

    /// <summary>Throws if the caller may not read the room. Used by hub and REST alike.</summary>
    Task<RoomDocument> RequireReadAccessAsync(Guid roomId, CancellationToken ct = default);

    // ── Administration ──────────────────────────────────────────────────────────
    Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<RoomDto> UpdateAsync(Guid roomId, UpdateRoomRequest request, CancellationToken ct = default);
    Task ArchiveAsync(Guid roomId, CancellationToken ct = default);
    Task RestoreAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>Adds a user to every system room. Called by Auth after registration.</summary>
    Task JoinDefaultRoomsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Creates the rooms that must always exist. Runs at startup.</summary>
    Task EnsureSystemRoomsAsync(CancellationToken ct = default);
}

public interface IMessageService
{
    Task<CursorPage<MessageDto>> GetHistoryAsync(
        Guid roomId, string? before, int limit, CancellationToken ct = default);

    Task<MessageDto> SendAsync(
        Guid roomId, SendMessageRequest request, CancellationToken ct = default);

    Task<MessageDto> EditAsync(
        Guid messageId, EditMessageRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>Admin / automated removal. Distinguished from a user's own deletion.</summary>
    Task ModerationDeleteAsync(Guid messageId, string reason, CancellationToken ct = default);

    Task<long> ModerationDeleteAllByAuthorAsync(
        Guid authorUserId, string reason, CancellationToken ct = default);

    Task<MessageDto> ToggleReactionAsync(
        Guid messageId, string emoji, CancellationToken ct = default);

    Task<MessageDto> GetAsync(Guid messageId, CancellationToken ct = default);
}

/// <summary>
/// Server -> client realtime events. Declared here so the application layer can
/// publish without referencing SignalR; implemented in the API project.
/// </summary>
public interface IChatBroadcaster
{
    Task MessageReceivedAsync(Guid roomId, MessageDto message, IReadOnlyList<Guid> recipientUserIds);
    Task MessageEditedAsync(Guid roomId, MessageDto message);
    Task MessageDeletedAsync(Guid roomId, Guid messageId, DeletionKind kind, DateTime at);
    Task ReactionsChangedAsync(Guid roomId, Guid messageId, MessageDto message);
    Task RoomUpdatedAsync(Guid userId, RoomUpdatedDto update);
    Task RoomReadAsync(Guid roomId, string anonymousName, DateTime at);
    Task RoomPresenceChangedAsync(Guid roomId, IReadOnlyList<RoomMemberDto> online);
}

/// <summary>Fire-and-forget notification dispatch to the notification service.</summary>
public interface INotificationSender
{
    Task SendAsync(
        Guid userId, string title, string message, string type,
        Guid? sourceId = null, CancellationToken ct = default);

    Task DeleteBySourceAsync(Guid sourceId, CancellationToken ct = default);
}
