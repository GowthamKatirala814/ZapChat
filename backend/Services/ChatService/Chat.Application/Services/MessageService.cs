using System.Text.RegularExpressions;
using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Chat.Domain.Documents;
using Microsoft.Extensions.Logging;
using Shared.Moderation;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;
using ZapChat.Shared.Realtime;
using ZapChat.Shared.Results;

namespace Chat.Application.Services;

public sealed partial class MessageService : IMessageService
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DeleteWindow = TimeSpan.FromHours(24);
    private const int PreviewLength = 80;

    [GeneratedRegex(@"@([A-Za-z]{2,40})", RegexOptions.Compiled)]
    private static partial Regex MentionPattern();

    private readonly IMessageRepository _messages;
    private readonly IRoomRepository _rooms;
    private readonly IRoomMemberRepository _members;
    private readonly IModerationEventRepository _moderationEvents;
    private readonly IFileRepository _files;
    private readonly IRoomService _roomService;
    private readonly IModerationPipeline _moderation;
    private readonly IChatBroadcaster _broadcaster;
    private readonly INotificationSender _notifications;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IMessageRepository messages,
        IRoomRepository rooms,
        IRoomMemberRepository members,
        IModerationEventRepository moderationEvents,
        IFileRepository files,
        IRoomService roomService,
        IModerationPipeline moderation,
        IChatBroadcaster broadcaster,
        INotificationSender notifications,
        ICurrentUser currentUser,
        ILogger<MessageService> logger)
    {
        _messages = messages;
        _rooms = rooms;
        _members = members;
        _moderationEvents = moderationEvents;
        _files = files;
        _roomService = roomService;
        _moderation = moderation;
        _broadcaster = broadcaster;
        _notifications = notifications;
        _currentUser = currentUser;
        _logger = logger;
    }

    // ── Read ────────────────────────────────────────────────────────────────────

    public async Task<CursorPage<MessageDto>> GetHistoryAsync(
        Guid roomId, string? before, int limit, CancellationToken ct = default)
    {
        // Authorization first. The old endpoint had none, so any caller could read
        // any room's entire history, including HR Issues.
        await _roomService.RequireReadAccessAsync(roomId, ct);

        var page = await _messages.GetHistoryAsync(roomId, before, limit, ct);
        var me = _currentUser.UserId;

        return new CursorPage<MessageDto>
        {
            Items = page.Items.Select(m => ToDto(m, me)).ToList(),
            HasMore = page.HasMore,
            NextCursor = page.NextCursor
        };
    }

    public async Task<MessageDto> GetAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        await _roomService.RequireReadAccessAsync(message.RoomId, ct);

        return ToDto(message, _currentUser.UserId);
    }

    // ── Send ────────────────────────────────────────────────────────────────────

    public async Task<MessageDto> SendAsync(
        Guid roomId, SendMessageRequest request, CancellationToken ct = default)
    {
        var room = await _roomService.RequireReadAccessAsync(roomId, ct);
        var userId = _currentUser.RequireUserId();
        var anonymousName = _currentUser.AnonymousName;

        // ── Moderation gate: before any write, before any broadcast ──────────────
        // The HR channel fails closed: if the classifier is unreachable the message is
        // rejected rather than let through unverified.
        var verdict = await _moderation.EvaluateAsync(new ModerationRequest(
            request.Content, userId, anonymousName, room.Id, room.Name,
            FailClosed: room.Type == RoomType.Hr), ct);

        await _moderationEvents.InsertAsync(new ModerationEventDocument
        {
            UserId = userId,
            AnonymousName = anonymousName,
            RoomId = room.Id,
            RoomName = room.Name,
            Snippet = Truncate(request.Content, 200),
            Category = verdict.Category,
            Confidence = verdict.Confidence,
            WasAllowed = verdict.Allowed,
            Engine = verdict.Engine,
            MatchedRule = verdict.MatchedRule,
            Explanation = verdict.Reason
        }, ct);

        if (!verdict.Allowed)
        {
            _logger.LogInformation(
                "Blocked a message from {Author} in {Room}: {Category} via {Engine}.",
                anonymousName, room.Name, verdict.Category, verdict.Engine);

            // 422 with the category — the sender is told, nobody else is.
            throw new RejectedException(verdict.Reason, verdict.Category);
        }

        // ── Reply target ────────────────────────────────────────────────────────
        ReplyReference? replyTo = null;

        if (request.ReplyToMessageId is { } parentId)
        {
            var parent = await _messages.GetByIdAsync(parentId, ct);

            if (parent is null || parent.RoomId != roomId)
                throw new ValidationException("The message being replied to is not in this room.");

            replyTo = new ReplyReference
            {
                MessageId = parent.Id,
                // Snapshot, so editing the parent later does not rewrite this reply.
                Snippet = parent.IsVisible ? Truncate(parent.Content, 120) : string.Empty,
                AuthorName = parent.Author.AnonymousName
            };
        }

        // ── Attachments ─────────────────────────────────────────────────────────
        var attachments = new List<MessageAttachment>();

        if (request.AttachmentIds.Count > 0)
        {
            var files = await _files.GetManyAsync(request.AttachmentIds, ct);

            // A caller may only attach files they uploaded, and only once.
            foreach (var file in files)
            {
                if (file.OwnerUserId != userId)
                    throw new ForbiddenException("You can only attach files you uploaded.");

                if (file.MessageId is not null)
                    throw new ValidationException("That file is already attached to a message.");

                attachments.Add(new MessageAttachment
                {
                    Id = file.Id,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.SizeBytes,
                    Url = $"/api/files/{file.Id}"
                });
            }
        }

        var mentions = MentionPattern().Matches(request.Content)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var message = new MessageDocument
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Author = new MessageAuthor { UserId = userId, AnonymousName = anonymousName },
            Content = request.Content,
            SentAt = DateTime.UtcNow,
            ReplyTo = replyTo,
            Attachments = attachments,
            Mentions = mentions,
            Moderation = verdict.Engine is "Rules" or "Gemini"
                ? new ModerationStamp
                {
                    Engine = verdict.Engine,
                    Category = verdict.Category,
                    Confidence = verdict.Confidence,
                    MatchedRule = verdict.MatchedRule
                }
                : null
        };

        await _messages.InsertAsync(message, ct);

        if (attachments.Count > 0)
            await _files.BindToMessageAsync(request.AttachmentIds, roomId, message.Id, ct);

        var summary = new LastMessageSummary
        {
            MessageId = message.Id,
            Preview = Truncate(request.Content, PreviewLength),
            AuthorName = anonymousName,
            SentAt = message.SentAt
        };

        await _rooms.SetLastMessageAsync(roomId, summary, ct);

        // Sender is implicitly caught up.
        await _members.MarkReadAsync(roomId, userId, ct);

        // Keep the denormalized name current for this member.
        await _members.RefreshAnonymousNameAsync(userId, anonymousName, ct);

        // ── Fan-out ─────────────────────────────────────────────────────────────
        // One UpdateMany bumps every other member's unread count and returns the
        // updated rows. This is the path that used to depend on an unauthenticated
        // cross-service call, which 401'd and silently disabled unread badges,
        // @mentions, room notifications and read receipts.
        var others = await _members.IncrementUnreadExceptAsync(roomId, userId, ct);

        var dto = ToDto(message, userId);

        // The broadcast goes to a group, so it must be viewer-neutral: a single DTO
        // cannot carry a correct per-recipient IsMine. It is sent with no viewer, which
        // makes IsMine false for everyone. The sender gets the accurate version as the
        // return value of this call, and a second tab belonging to the sender can match
        // on AnonymousName, which is unique platform-wide.
        await _broadcaster.MessageReceivedAsync(
            roomId, ToDto(message, viewerUserId: null), others.Select(m => m.UserId).ToList());

        // Exact persisted count per member, not a client-side guess.
        foreach (var member in others)
        {
            await _broadcaster.RoomUpdatedAsync(member.UserId, new RoomUpdatedDto(
                roomId, room.Name, member.UnreadCount,
                new LastMessageDto(summary.MessageId, summary.Preview,
                    summary.AuthorName, summary.SentAt)));
        }

        // The sender's own sidebar: preview moves, unread stays cleared.
        await _broadcaster.RoomUpdatedAsync(userId, new RoomUpdatedDto(
            roomId, room.Name, 0,
            new LastMessageDto(summary.MessageId, summary.Preview,
                summary.AuthorName, summary.SentAt)));

        await NotifyAsync(room, message, others, ct);

        return dto;
    }

    /// <summary>
    /// Mentions and replies produce notifications. Mentions resolve against the
    /// denormalized member names, so this needs no call to Auth at all.
    /// </summary>
    private async Task NotifyAsync(
        RoomDocument room, MessageDocument message,
        IReadOnlyList<RoomMemberDocument> others, CancellationToken ct)
    {
        var notified = new HashSet<Guid>();

        foreach (var name in message.Mentions)
        {
            var member = others.FirstOrDefault(m =>
                string.Equals(m.AnonymousName, name, StringComparison.OrdinalIgnoreCase));

            if (member is null || member.IsMuted || !notified.Add(member.UserId)) continue;

            await _notifications.SendAsync(
                member.UserId,
                "You were mentioned",
                $"{message.Author.AnonymousName} mentioned you in {room.Name}",
                "Mention", message.Id, ct);
        }

        if (message.ReplyTo is not null)
        {
            var parent = await _messages.GetByIdAsync(message.ReplyTo.MessageId, ct);

            if (parent is not null
                && parent.Author.UserId != message.Author.UserId
                && notified.Add(parent.Author.UserId))
            {
                await _notifications.SendAsync(
                    parent.Author.UserId,
                    "New reply",
                    $"{message.Author.AnonymousName} replied to you in {room.Name}",
                    "Reply", message.Id, ct);
            }
        }
    }

    // ── Edit / delete ───────────────────────────────────────────────────────────

    public async Task<MessageDto> EditAsync(
        Guid messageId, EditMessageRequest request, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        var userId = _currentUser.RequireUserId();

        // Ownership by user id, not by comparing anonymous name strings.
        if (message.Author.UserId != userId)
            throw new ForbiddenException("You can only edit your own messages.");

        if (!message.IsVisible)
            throw new ValidationException("A deleted message cannot be edited.");

        if (DateTime.UtcNow - message.SentAt > EditWindow)
            throw new ValidationException("Messages can only be edited within 15 minutes of sending.");

        var room = await _rooms.GetByIdAsync(message.RoomId, ct)
                   ?? throw new NotFoundException("That room does not exist.");

        var verdict = await _moderation.EvaluateAsync(new ModerationRequest(
            request.Content, userId, _currentUser.AnonymousName, room.Id, room.Name,
            FailClosed: room.Type == RoomType.Hr), ct);

        if (!verdict.Allowed)
            throw new RejectedException(verdict.Reason, verdict.Category);

        if (!await _messages.EditAsync(messageId, userId, request.Content, ct))
            throw new ConflictException("That message could no longer be edited.");

        var updated = await _messages.GetByIdAsync(messageId, ct)!;

        // Viewer-neutral for the group broadcast; the caller gets their own view back.
        await _broadcaster.MessageEditedAsync(message.RoomId, ToDto(updated!, viewerUserId: null));

        return ToDto(updated!, userId);
    }

    public async Task DeleteAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        var userId = _currentUser.RequireUserId();

        if (message.Author.UserId != userId)
            throw new ForbiddenException("You can only delete your own messages.");

        if (!message.IsVisible)
            throw new ValidationException("That message is already deleted.");

        if (DateTime.UtcNow - message.SentAt > DeleteWindow)
            throw new ValidationException("Messages can only be deleted within 24 hours of sending.");

        await RemoveAsync(message, userId, DeletionKind.User, reason: null, ct);
    }

    /// <summary>
    /// Moderation removal. Unlike the old admin path — which only marked reports
    /// reviewed and never touched the message — this actually removes it and tells
    /// every client.
    /// </summary>
    public async Task ModerationDeleteAsync(
        Guid messageId, string reason, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        if (!message.IsVisible)
            throw new ConflictException("That message is already removed.");

        await RemoveAsync(message, _currentUser.UserId, DeletionKind.Moderation, reason, ct);

        _logger.LogWarning(
            "Message {MessageId} in room {RoomId} removed by moderation. Reason: {Reason}",
            messageId, message.RoomId, reason);
    }

    public async Task<long> ModerationDeleteAllByAuthorAsync(
        Guid authorUserId, string reason, CancellationToken ct = default)
    {
        var removed = await _messages.SoftDeleteAllByAuthorAsync(authorUserId, reason, ct);

        _logger.LogWarning(
            "Removed {Count} message(s) authored by {UserId}. Reason: {Reason}",
            removed, authorUserId, reason);

        return removed;
    }

    private async Task RemoveAsync(
        MessageDocument message, Guid? actor, DeletionKind kind, string? reason,
        CancellationToken ct)
    {
        if (!await _messages.SoftDeleteAsync(message.Id, actor, kind, reason, ct))
            throw new ConflictException("That message could no longer be deleted.");

        var at = DateTime.UtcNow;

        await _broadcaster.MessageDeletedAsync(message.RoomId, message.Id, kind, at);

        // If this was the room's newest message, recompute the sidebar preview so it
        // does not keep showing deleted content.
        var room = await _rooms.GetByIdAsync(message.RoomId, ct);

        if (room?.LastMessage?.MessageId == message.Id)
        {
            var newest = await _messages.GetNewestVisibleAsync(message.RoomId, ct);

            var replacement = newest is null
                ? null
                : new LastMessageSummary
                {
                    MessageId = newest.Id,
                    Preview = Truncate(newest.Content, PreviewLength),
                    AuthorName = newest.Author.AnonymousName,
                    SentAt = newest.SentAt
                };

            await _rooms.ClearLastMessageAsync(message.RoomId, replacement, ct);
        }

        // Remove any notification that pointed at this message.
        await _notifications.DeleteBySourceAsync(message.Id, ct);
    }

    // ── Reactions ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Server decides whether this is an add or a remove and returns the resulting
    /// state. The old hub always inserted while the client toggled locally, so the two
    /// diverged and the database accumulated duplicates.
    /// </summary>
    public async Task<MessageDto> ToggleReactionAsync(
        Guid messageId, string emoji, CancellationToken ct = default)
    {
        if (!ReactionCatalogue.IsAllowed(emoji))
            throw new ValidationException("That is not an available reaction.");

        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        await _roomService.RequireReadAccessAsync(message.RoomId, ct);

        var userId = _currentUser.RequireUserId();

        var updated = await _messages.ToggleReactionAsync(
            messageId, userId, _currentUser.AnonymousName, emoji, ct);

        if (updated is null)
            throw new ConflictException("That reaction could not be applied.");

        // Scoped to the room group, not every connected client. Sent viewer-neutral so
        // no recipient sees another user's "mine" flag; each client re-evaluates its own
        // state from the returned reaction list.
        await _broadcaster.ReactionsChangedAsync(
            message.RoomId, messageId, ToDto(updated, viewerUserId: null));

        return ToDto(updated, userId);
    }

    // ── Mapping ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Projects a stored message for one viewer.
    ///
    /// Deleted content is replaced with an empty string rather than being sent and
    /// hidden by the client, and the deleting user's id is never included — only
    /// whether it was the author or moderation.
    /// </summary>
    internal static MessageDto ToDto(MessageDocument m, Guid? viewerUserId)
    {
        var visible = m.IsVisible;

        return new MessageDto(
            m.Id,
            m.RoomId,
            m.Author.AnonymousName,
            IsMine: viewerUserId is not null && m.Author.UserId == viewerUserId,
            Content: visible ? m.Content : string.Empty,
            SentAt: m.SentAt,
            ReplyTo: m.ReplyTo is null
                ? null
                : new ReplyDto(m.ReplyTo.MessageId, m.ReplyTo.Snippet, m.ReplyTo.AuthorName),
            Reactions: visible
                ? m.Reactions
                    .Where(r => r.UserIds.Count > 0)
                    .Select(r => new ReactionDto(
                        r.Emoji, r.UserIds.Count,
                        viewerUserId is not null && r.UserIds.Contains(viewerUserId.Value),
                        r.Names))
                    .ToList()
                : [],
            Attachments: visible
                ? m.Attachments
                    .Select(a => new AttachmentDto(
                        a.Id, a.FileName, a.ContentType, a.SizeBytes, a.Url))
                    .ToList()
                : [],
            IsEdited: m.State.IsEdited,
            EditedAt: m.State.EditedAt,
            DeletedBy: m.State.Deletion.Kind,
            DeletedAt: m.State.Deletion.At);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
