using System.ComponentModel.DataAnnotations;
using Chat.Domain.Documents;

namespace Chat.Application.DTOs;

/// <summary>A room as the sidebar sees it, including this caller's unread count.</summary>
public sealed record RoomDto(
    Guid Id,
    string Name,
    RoomType Type,
    string? Branch,
    string Description,
    int MemberCount,
    int MessageCount,
    bool IsArchived,
    DateTime CreatedAt,
    LastMessageDto? LastMessage,
    int UnreadCount,
    bool IsMember);

public sealed record LastMessageDto(
    Guid MessageId, string Preview, string AuthorName, DateTime SentAt);

/// <summary>
/// A message as sent to clients.
///
/// The author's real user id is deliberately absent. <see cref="IsMine"/> is computed
/// server-side for the requesting caller, which is all the UI needs to decide whether
/// to offer edit and delete — so ownership never requires disclosing who wrote what.
/// </summary>
public sealed record MessageDto(
    Guid Id,
    Guid RoomId,
    string AnonymousName,
    bool IsMine,
    string Content,
    DateTime SentAt,
    ReplyDto? ReplyTo,
    IReadOnlyList<ReactionDto> Reactions,
    IReadOnlyList<AttachmentDto> Attachments,
    bool IsEdited,
    DateTime? EditedAt,
    DeletionKind DeletedBy,
    DateTime? DeletedAt);

public sealed record ReplyDto(Guid MessageId, string Snippet, string AuthorName);

/// <summary>
/// A reaction group. <see cref="Mine"/> lets the client render the pressed state
/// without knowing which users reacted.
/// </summary>
public sealed record ReactionDto(string Emoji, int Count, bool Mine, IReadOnlyList<string> Names);

public sealed record AttachmentDto(
    Guid Id, string FileName, string ContentType, long SizeBytes, string Url);

public sealed record RoomMemberDto(Guid UserId, string AnonymousName, bool IsOnline);

/// <summary>Who has read up to when — the data the old seen-by endpoint always returned empty.</summary>
public sealed record ReadReceiptDto(string AnonymousName, DateTime LastReadAt);

// ── Requests ────────────────────────────────────────────────────────────────────

public sealed class CreateRoomRequest
{
    [Required, StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public RoomType Type { get; set; } = RoomType.General;

    /// <summary>Required when Type is Branch.</summary>
    [MaxLength(120)]
    public string? Branch { get; set; }
}

public sealed class UpdateRoomRequest
{
    [Required, StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Sending a message. There is no author field — identity comes from the token.
/// </summary>
public sealed class SendMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    public Guid? ReplyToMessageId { get; set; }

    /// <summary>Ids returned by the upload endpoint.</summary>
    public List<Guid> AttachmentIds { get; set; } = [];
}

public sealed class EditMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public sealed class ReactRequest
{
    [Required, StringLength(8, MinimumLength = 1)]
    public string Emoji { get; set; } = string.Empty;
}

/// <summary>Result of a moderation gate rejection, surfaced to the sender only.</summary>
public sealed record MessageBlockedDto(string Category, string Reason);

/// <summary>Per-user sidebar update pushed after a message lands.</summary>
public sealed record RoomUpdatedDto(
    Guid RoomId,
    string RoomName,
    int UnreadCount,
    LastMessageDto? LastMessage);
