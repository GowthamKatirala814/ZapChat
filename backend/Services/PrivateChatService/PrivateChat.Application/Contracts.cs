using System.ComponentModel.DataAnnotations;
using PrivateChat.Domain.Documents;
using ZapChat.Shared.Results;

namespace PrivateChat.Application;

// ── DTOs ────────────────────────────────────────────────────────────────────────

/// <summary>
/// A conversation from one caller's point of view. The other participant is always
/// identified by anonymous name only.
/// </summary>
public sealed record ConversationDto(
    Guid Id,
    Guid OtherUserId,
    string OtherAnonymousName,
    int UnreadCount,
    LastMessageDto? LastMessage,
    bool IsBlockedByMe,
    bool HasBlockedMe);

public sealed record LastMessageDto(
    Guid MessageId, string Preview, string SenderName, bool SentByMe, DateTime SentAt);

public sealed record DirectMessageDto(
    Guid Id,
    Guid ConversationId,
    string SenderName,
    bool IsMine,
    string Content,
    DateTime SentAt,
    DateTime? ReadAt,
    ReplyDto? ReplyTo,
    IReadOnlyList<ReactionDto> Reactions,
    IReadOnlyList<AttachmentDto> Attachments,
    bool IsEdited,
    DateTime? EditedAt,
    DeletionKind DeletedBy,
    DateTime? DeletedAt);

public sealed record ReplyDto(Guid MessageId, string Snippet, string AuthorName);

public sealed record ReactionDto(string Emoji, int Count, bool Mine, IReadOnlyList<string> Names);

public sealed record AttachmentDto(
    Guid Id, string FileName, string ContentType, long SizeBytes, string Url);

public sealed record ConversationUpdatedDto(
    Guid ConversationId, int UnreadCount, LastMessageDto? LastMessage);

// ── Requests ────────────────────────────────────────────────────────────────────

public sealed class StartConversationRequest
{
    /// <summary>The other participant. The caller is always taken from the token.</summary>
    [Required]
    public Guid OtherUserId { get; set; }
}

public sealed class SendDirectMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    public Guid? ReplyToMessageId { get; set; }
}

public sealed class EditDirectMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed class ReactRequest
{
    [Required, StringLength(8, MinimumLength = 1)]
    public string Emoji { get; set; } = string.Empty;
}

// ── Abstractions ────────────────────────────────────────────────────────────────

public interface IConversationRepository
{
    Task<ConversationDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ConversationDocument?> GetByPairAsync(Guid a, Guid b, CancellationToken ct = default);

    Task<IReadOnlyList<ConversationDocument>> ListForUserAsync(
        Guid userId, CancellationToken ct = default);

    /// <summary>Upsert on the participant key so a concurrent start cannot duplicate.</summary>
    Task<ConversationDocument> GetOrCreateAsync(
        Guid a, string aName, Guid b, string bName, CancellationToken ct = default);

    Task SetLastMessageAsync(
        Guid conversationId, LastMessageSummary summary, Guid recipientUserId,
        CancellationToken ct = default);

    Task ReplaceLastMessageAsync(
        Guid conversationId, LastMessageSummary? replacement, CancellationToken ct = default);

    Task<bool> MarkReadAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

    Task RefreshAnonymousNameAsync(
        Guid userId, string anonymousName, CancellationToken ct = default);

    Task<long> CountAsync(CancellationToken ct = default);
}

public interface IDirectMessageRepository
{
    Task<DirectMessageDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task InsertAsync(DirectMessageDocument message, CancellationToken ct = default);

    Task<CursorPage<DirectMessageDocument>> GetHistoryAsync(
        Guid conversationId, string? before, int limit, CancellationToken ct = default);

    Task<bool> EditAsync(
        Guid id, Guid senderUserId, string content, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(
        Guid id, Guid? actorUserId, DeletionKind kind, string? reason,
        CancellationToken ct = default);

    Task<long> SoftDeleteAllBySenderAsync(
        Guid senderUserId, string reason, CancellationToken ct = default);

    Task<DirectMessageDocument?> ToggleReactionAsync(
        Guid messageId, Guid userId, string anonymousName, string emoji,
        CancellationToken ct = default);

    /// <summary>Marks every unread inbound message read. Returns the ids that changed.</summary>
    Task<IReadOnlyList<Guid>> MarkConversationReadAsync(
        Guid conversationId, Guid readerUserId, CancellationToken ct = default);

    Task<DirectMessageDocument?> GetNewestVisibleAsync(
        Guid conversationId, CancellationToken ct = default);

    Task<long> CountAsync(CancellationToken ct = default);

    Task<IReadOnlyList<(DateTime Day, int Count)>> CountByDayAsync(
        int days, CancellationToken ct = default);
}

public interface IUserBlockRepository
{
    Task<bool> BlockAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default);
    Task<bool> UnblockAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default);

    /// <summary>True when either party has blocked the other.</summary>
    Task<bool> AnyBetweenAsync(Guid a, Guid b, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListBlockedByAsync(Guid blockerId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListBlockersOfAsync(Guid blockedId, CancellationToken ct = default);
}

public interface IModerationEventRepository
{
    Task InsertAsync(ModerationEventDocument document, CancellationToken ct = default);
    Task<PrivateModerationStats> GetStatsAsync(CancellationToken ct = default);
}

public sealed record PrivateModerationStats(
    long Total, long Allowed, long Blocked, Dictionary<string, int> BlockedByCategory);

/// <summary>Realtime events. Implemented in the API project over SignalR.</summary>
public interface IPrivateChatBroadcaster
{
    Task MessageReceivedAsync(Guid conversationId, DirectMessageDto message, Guid[] recipients);
    Task MessageEditedAsync(DirectMessageDto message, Guid[] recipients);
    Task MessageDeletedAsync(
        Guid conversationId, Guid messageId, DeletionKind kind, DateTime at, Guid[] recipients);
    Task ReactionsChangedAsync(DirectMessageDto message, Guid[] recipients);
    Task ConversationUpdatedAsync(Guid userId, ConversationUpdatedDto update);
    Task MessagesReadAsync(Guid conversationId, IReadOnlyList<Guid> messageIds, Guid senderUserId);
}

public interface INotificationSender
{
    Task SendAsync(
        Guid userId, string title, string message, string type,
        Guid? sourceId = null, CancellationToken ct = default);

    Task DeleteBySourceAsync(Guid sourceId, CancellationToken ct = default);
}

/// <summary>Resolves user ids to anonymous names via Auth, for conversation creation.</summary>
public interface IUserDirectory
{
    Task<string?> GetAnonymousNameAsync(Guid userId, CancellationToken ct = default);
}

public interface IConversationService
{
    Task<IReadOnlyList<ConversationDto>> ListAsync(CancellationToken ct = default);

    Task<ConversationDto> StartAsync(Guid otherUserId, CancellationToken ct = default);

    Task<ConversationDto> GetAsync(Guid conversationId, CancellationToken ct = default);

    Task<CursorPage<DirectMessageDto>> GetHistoryAsync(
        Guid conversationId, string? before, int limit, CancellationToken ct = default);

    Task MarkReadAsync(Guid conversationId, CancellationToken ct = default);

    Task<DirectMessageDto> SendAsync(
        Guid conversationId, SendDirectMessageRequest request, CancellationToken ct = default);

    Task<DirectMessageDto> EditAsync(
        Guid messageId, EditDirectMessageRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid messageId, CancellationToken ct = default);

    Task ModerationDeleteAsync(Guid messageId, string reason, CancellationToken ct = default);

    Task<long> ModerationDeleteAllBySenderAsync(
        Guid senderUserId, string reason, CancellationToken ct = default);

    Task<DirectMessageDto> ToggleReactionAsync(
        Guid messageId, string emoji, CancellationToken ct = default);

    Task BlockAsync(Guid otherUserId, CancellationToken ct = default);
    Task UnblockAsync(Guid otherUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListBlockedAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads a conversation and asserts the caller is a participant. The old service
    /// had no equivalent — any caller could read any conversation by id.
    /// </summary>
    Task<ConversationDocument> RequireParticipantAsync(
        Guid conversationId, CancellationToken ct = default);
}
