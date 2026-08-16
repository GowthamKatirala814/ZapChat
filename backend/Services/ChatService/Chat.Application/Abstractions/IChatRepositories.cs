using Chat.Domain.Documents;
using ZapChat.Shared.Results;

namespace Chat.Application.Abstractions;

public interface IRoomRepository
{
    Task<RoomDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoomDocument?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<RoomDocument>> ListAsync(
        bool includeArchived, CancellationToken ct = default);

    Task<IReadOnlyList<RoomDocument>> GetManyAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task InsertAsync(RoomDocument room, CancellationToken ct = default);

    Task<bool> UpdateAsync(
        Guid id, string name, string description, CancellationToken ct = default);

    Task<bool> ArchiveAsync(Guid id, Guid archivedBy, CancellationToken ct = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>Atomically records the newest message and bumps the message count.</summary>
    Task SetLastMessageAsync(
        Guid roomId, LastMessageSummary summary, CancellationToken ct = default);

    /// <summary>Recomputes the preview after the newest message is deleted.</summary>
    Task ClearLastMessageAsync(
        Guid roomId, LastMessageSummary? replacement, CancellationToken ct = default);

    Task AdjustMemberCountAsync(Guid roomId, int delta, CancellationToken ct = default);

    Task<long> CountAsync(bool includeArchived, CancellationToken ct = default);
}

public interface IRoomMemberRepository
{
    Task<RoomMemberDocument?> GetAsync(Guid roomId, Guid userId, CancellationToken ct = default);

    Task<bool> IsActiveMemberAsync(Guid roomId, Guid userId, CancellationToken ct = default);

    /// <summary>Adds a member, or reactivates an existing row. Returns true if newly added.</summary>
    Task<bool> JoinAsync(
        Guid roomId, Guid userId, string anonymousName, CancellationToken ct = default);

    Task<bool> LeaveAsync(Guid roomId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<RoomMemberDocument>> ListForRoomAsync(
        Guid roomId, CancellationToken ct = default);

    Task<IReadOnlyList<RoomMemberDocument>> ListForUserAsync(
        Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Bumps every active member's unread count except the sender, in one command.
    /// Returns the updated members so the hub can push exact per-user counts.
    /// </summary>
    Task<IReadOnlyList<RoomMemberDocument>> IncrementUnreadExceptAsync(
        Guid roomId, Guid senderUserId, CancellationToken ct = default);

    Task<bool> MarkReadAsync(Guid roomId, Guid userId, CancellationToken ct = default);

    /// <summary>Keeps the denormalized name current when it changes.</summary>
    Task RefreshAnonymousNameAsync(
        Guid userId, string anonymousName, CancellationToken ct = default);

    Task<long> DeactivateAllForUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IMessageRepository
{
    Task<MessageDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task InsertAsync(MessageDocument message, CancellationToken ct = default);

    /// <summary>
    /// Newest-first page. <paramref name="before"/> is an opaque cursor from a prior
    /// page. Cursor rather than offset so messages arriving mid-scroll cannot cause
    /// duplicates or gaps.
    /// </summary>
    Task<CursorPage<MessageDocument>> GetHistoryAsync(
        Guid roomId, string? before, int limit, CancellationToken ct = default);

    Task<bool> EditAsync(
        Guid id, Guid authorUserId, string content, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(
        Guid id, Guid? actorUserId, DeletionKind kind, string? reason,
        CancellationToken ct = default);

    /// <summary>Moderation removal of everything an author posted.</summary>
    Task<long> SoftDeleteAllByAuthorAsync(
        Guid authorUserId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Adds or removes one user's reaction and returns the message as it now stands.
    /// A single round trip so the client and server cannot disagree on the result.
    /// </summary>
    Task<MessageDocument?> ToggleReactionAsync(
        Guid messageId, Guid userId, string anonymousName, string emoji,
        CancellationToken ct = default);

    Task<MessageDocument?> GetNewestVisibleAsync(Guid roomId, CancellationToken ct = default);

    Task AttachFilesAsync(
        Guid messageId, IReadOnlyList<MessageAttachment> attachments,
        CancellationToken ct = default);

    // ── Analytics (aggregations, replacing cross-service HTTP calls) ─────────────

    Task<long> CountAsync(CancellationToken ct = default);

    Task<IReadOnlyList<(Guid RoomId, int Count)>> CountByRoomAsync(
        int top, CancellationToken ct = default);

    Task<IReadOnlyList<(string AnonymousName, int Count)>> CountByAuthorAsync(
        int top, CancellationToken ct = default);

    Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default);

    Task<IReadOnlyList<(int Hour, int Count)>> CountByHourAsync(CancellationToken ct = default);
}

public interface IModerationEventRepository
{
    Task InsertAsync(ModerationEventDocument document, CancellationToken ct = default);

    Task<ModerationStatsDto> GetStatsAsync(CancellationToken ct = default);
}

public sealed record ModerationStatsDto(
    long Total,
    long Allowed,
    long Blocked,
    long GeminiRequests,
    long RuleRequests,
    Dictionary<string, int> BlockedByCategory,
    Dictionary<string, int> TopMatchedRules);

public interface IFileRepository
{
    Task InsertAsync(FileDocument document, CancellationToken ct = default);
    Task<FileDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<FileDocument>> GetManyAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task BindToMessageAsync(
        IReadOnlyCollection<Guid> fileIds, Guid roomId, Guid messageId,
        CancellationToken ct = default);
}

public interface IPresenceRepository
{
    Task ConnectAsync(
        string connectionId, Guid userId, string anonymousName, CancellationToken ct = default);

    Task DisconnectAsync(string connectionId, CancellationToken ct = default);

    Task JoinRoomAsync(string connectionId, Guid roomId, CancellationToken ct = default);
    Task LeaveRoomAsync(string connectionId, Guid roomId, CancellationToken ct = default);

    Task HeartbeatAsync(string connectionId, CancellationToken ct = default);

    /// <summary>Distinct user ids currently present in a room.</summary>
    Task<IReadOnlyList<Guid>> GetOnlineUserIdsAsync(Guid roomId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetAllOnlineUserIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Rooms a connection had joined. Read before removing the connection so presence
    /// can be re-published to the rooms it was in.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetRoomsForConnectionAsync(
        string connectionId, CancellationToken ct = default);
}
