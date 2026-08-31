using Microsoft.Extensions.Logging;
using PrivateChat.Domain.Documents;
using Shared.Moderation;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;
using ZapChat.Shared.Realtime;
using ZapChat.Shared.Results;

namespace PrivateChat.Application;

public sealed class ConversationService : IConversationService
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DeleteWindow = TimeSpan.FromHours(24);
    private const int PreviewLength = 80;


    private readonly IConversationRepository _conversations;
    private readonly IDirectMessageRepository _messages;
    private readonly IUserBlockRepository _blocks;
    private readonly IModerationEventRepository _moderationEvents;
    private readonly IModerationPipeline _moderation;
    private readonly IPrivateChatBroadcaster _broadcaster;
    private readonly INotificationSender _notifications;
    private readonly IUserDirectory _directory;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversations,
        IDirectMessageRepository messages,
        IUserBlockRepository blocks,
        IModerationEventRepository moderationEvents,
        IModerationPipeline moderation,
        IPrivateChatBroadcaster broadcaster,
        INotificationSender notifications,
        IUserDirectory directory,
        ICurrentUser currentUser,
        ILogger<ConversationService> logger)
    {
        _conversations = conversations;
        _messages = messages;
        _blocks = blocks;
        _moderationEvents = moderationEvents;
        _moderation = moderation;
        _broadcaster = broadcaster;
        _notifications = notifications;
        _directory = directory;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// The authorization gate for everything in this service.
    ///
    /// Returns 404 rather than 403 for a conversation the caller is not part of: the
    /// existence of a conversation between two other people is itself private, so a
    /// 403 would confirm it. The old service had no check of any kind — passing any
    /// conversation id returned its full message history, unauthenticated.
    /// </summary>
    public async Task<ConversationDocument> RequireParticipantAsync(
        Guid conversationId, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        var conversation = await _conversations.GetByIdAsync(conversationId, ct);

        if (conversation is null || !conversation.Includes(userId))
        {
            _logger.LogWarning(
                "User {UserId} attempted to access conversation {ConversationId} they are not part of.",
                userId, conversationId);

            throw new NotFoundException("That conversation does not exist.");
        }

        return conversation;
    }

    // ── Conversations ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ConversationDto>> ListAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        var conversations = await _conversations.ListForUserAsync(userId, ct);

        var blockedByMe = (await _blocks.ListBlockedByAsync(userId, ct)).ToHashSet();
        var blockedMe = (await _blocks.ListBlockersOfAsync(userId, ct)).ToHashSet();

        return conversations
            .Select(c => ToDto(c, userId, blockedByMe, blockedMe))
            .ToList();
    }

    public async Task<ConversationDto> GetAsync(Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await RequireParticipantAsync(conversationId, ct);
        var userId = _currentUser.RequireUserId();

        var blockedByMe = (await _blocks.ListBlockedByAsync(userId, ct)).ToHashSet();
        var blockedMe = (await _blocks.ListBlockersOfAsync(userId, ct)).ToHashSet();

        return ToDto(conversation, userId, blockedByMe, blockedMe);
    }

    /// <summary>
    /// Starts (or returns) the conversation between the caller and one other user.
    /// The caller is always taken from the token — the old endpoint accepted both
    /// user ids as query parameters, so anyone could mint or fetch a conversation
    /// between any two people.
    /// </summary>
    public async Task<ConversationDto> StartAsync(Guid otherUserId, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        if (otherUserId == userId)
            throw new ValidationException("You cannot start a conversation with yourself.");

        if (await _blocks.AnyBetweenAsync(userId, otherUserId, ct))
            throw new ForbiddenException("You cannot start a conversation with this user.");

        var otherName = await _directory.GetAnonymousNameAsync(otherUserId, ct)
                        ?? throw new NotFoundException("That user does not exist.");

        var conversation = await _conversations.GetOrCreateAsync(
            userId, _currentUser.AnonymousName, otherUserId, otherName, ct);

        // Keep the caller's denormalized name current.
        await _conversations.RefreshAnonymousNameAsync(userId, _currentUser.AnonymousName, ct);

        return ToDto(conversation, userId, [], []);
    }

    public async Task<CursorPage<DirectMessageDto>> GetHistoryAsync(
        Guid conversationId, string? before, int limit, CancellationToken ct = default)
    {
        await RequireParticipantAsync(conversationId, ct);

        var page = await _messages.GetHistoryAsync(conversationId, before, limit, ct);
        var me = _currentUser.UserId;

        return new CursorPage<DirectMessageDto>
        {
            Items = page.Items.Select(m => ToDto(m, me)).ToList(),
            HasMore = page.HasMore,
            NextCursor = page.NextCursor
        };
    }

    public async Task MarkReadAsync(Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await RequireParticipantAsync(conversationId, ct);
        var userId = _currentUser.RequireUserId();

        await _conversations.MarkReadAsync(conversationId, userId, ct);

        var readIds = await _messages.MarkConversationReadAsync(conversationId, userId, ct);

        if (readIds.Count > 0)
        {
            var other = conversation.Other(userId);
            if (other is not null)
                await _broadcaster.MessagesReadAsync(conversationId, readIds, other.UserId);
        }
    }

    // ── Messaging ───────────────────────────────────────────────────────────────

    public async Task<DirectMessageDto> SendAsync(
        Guid conversationId, SendDirectMessageRequest request, CancellationToken ct = default)
    {
        var conversation = await RequireParticipantAsync(conversationId, ct);
        var userId = _currentUser.RequireUserId();

        // The recipient is derived from the conversation document, never taken from the
        // client. The old hub accepted receiverId as a parameter and never checked it
        // against the conversation, so a message could be addressed anywhere.
        var recipient = conversation.Other(userId)
                        ?? throw new ConflictException("That conversation has no other participant.");

        if (await _blocks.AnyBetweenAsync(userId, recipient.UserId, ct))
            throw new ForbiddenException("You cannot send messages to this user.");

        var verdict = await _moderation.EvaluateAsync(new ModerationRequest(
            request.Content, userId, _currentUser.AnonymousName,
            conversationId, "direct message"), ct);

        await _moderationEvents.InsertAsync(new ModerationEventDocument
        {
            UserId = userId,
            AnonymousName = _currentUser.AnonymousName,
            ConversationId = conversationId,
            Snippet = Truncate(request.Content, 200),
            Category = verdict.Category,
            Confidence = verdict.Confidence,
            WasAllowed = verdict.Allowed,
            Engine = verdict.Engine,
            MatchedRule = verdict.MatchedRule,
            Explanation = verdict.Reason
        }, ct);

        if (!verdict.Allowed)
            throw new RejectedException(verdict.Reason, verdict.Category);

        ReplyReference? replyTo = null;

        if (request.ReplyToMessageId is { } parentId)
        {
            var parent = await _messages.GetByIdAsync(parentId, ct);

            if (parent is null || parent.ConversationId != conversationId)
                throw new ValidationException("The message being replied to is not in this conversation.");

            replyTo = new ReplyReference
            {
                MessageId = parent.Id,
                Snippet = parent.IsVisible ? Truncate(parent.Content, 120) : string.Empty,
                AuthorName = parent.Sender.AnonymousName
            };
        }

        var message = new DirectMessageDocument
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Sender = new MessageSender
            {
                UserId = userId,
                AnonymousName = _currentUser.AnonymousName
            },
            Content = request.Content,
            SentAt = DateTime.UtcNow,
            ReplyTo = replyTo,
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

        var summary = new LastMessageSummary
        {
            MessageId = message.Id,
            Preview = Truncate(request.Content, PreviewLength),
            SenderId = userId,
            SenderName = _currentUser.AnonymousName,
            SentAt = message.SentAt
        };

        // One atomic update: preview, message count, and only the recipient's unread.
        await _conversations.SetLastMessageAsync(conversationId, summary, recipient.UserId, ct);

        var updated = await _conversations.GetByIdAsync(conversationId, ct);
        var recipientUnread = updated?.ParticipantFor(recipient.UserId)?.UnreadCount ?? 0;

        var senderDto = ToDto(message, userId);
        var recipientDto = ToDto(message, recipient.UserId);

        await _broadcaster.MessageReceivedAsync(conversationId, recipientDto, [recipient.UserId]);
        await _broadcaster.MessageReceivedAsync(conversationId, senderDto, [userId]);

        await _broadcaster.ConversationUpdatedAsync(recipient.UserId, new ConversationUpdatedDto(
            conversationId, recipientUnread,
            new LastMessageDto(summary.MessageId, summary.Preview, summary.SenderName,
                SentByMe: false, summary.SentAt)));

        await _broadcaster.ConversationUpdatedAsync(userId, new ConversationUpdatedDto(
            conversationId, 0,
            new LastMessageDto(summary.MessageId, summary.Preview, summary.SenderName,
                SentByMe: true, summary.SentAt)));

        await _notifications.SendAsync(
            recipient.UserId,
            "New private message",
            $"{_currentUser.AnonymousName} sent you a message",
            "Message", message.Id, ct);

        return senderDto;
    }

    public async Task<DirectMessageDto> EditAsync(
        Guid messageId, EditDirectMessageRequest request, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        var conversation = await RequireParticipantAsync(message.ConversationId, ct);
        var userId = _currentUser.RequireUserId();

        if (message.Sender.UserId != userId)
            throw new ForbiddenException("You can only edit your own messages.");

        if (!message.IsVisible)
            throw new ValidationException("A deleted message cannot be edited.");

        if (DateTime.UtcNow - message.SentAt > EditWindow)
            throw new ValidationException("Messages can only be edited within 15 minutes of sending.");

        var verdict = await _moderation.EvaluateAsync(new ModerationRequest(
            request.Content, userId, _currentUser.AnonymousName,
            message.ConversationId, "direct message"), ct);

        if (!verdict.Allowed)
            throw new RejectedException(verdict.Reason, verdict.Category);

        if (!await _messages.EditAsync(messageId, userId, request.Content, ct))
            throw new ConflictException("That message could no longer be edited.");

        var updated = await _messages.GetByIdAsync(messageId, ct)!;

        // One DTO per participant, exactly as SendAsync does. A single payload sent to
        // both would carry the editor's IsMine=true to the other participant, and their
        // client would then offer edit and delete on a message they did not write.
        foreach (var participant in conversation.Participants)
        {
            await _broadcaster.MessageEditedAsync(
                ToDto(updated!, participant.UserId), [participant.UserId]);
        }

        return ToDto(updated!, userId);
    }

    public async Task DeleteAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        var conversation = await RequireParticipantAsync(message.ConversationId, ct);
        var userId = _currentUser.RequireUserId();

        if (message.Sender.UserId != userId)
            throw new ForbiddenException("You can only delete your own messages.");

        if (!message.IsVisible)
            throw new ValidationException("That message is already deleted.");

        if (DateTime.UtcNow - message.SentAt > DeleteWindow)
            throw new ValidationException("Messages can only be deleted within 24 hours of sending.");

        await RemoveAsync(message, conversation, userId, DeletionKind.User, null, ct);
    }

    public async Task ModerationDeleteAsync(
        Guid messageId, string reason, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        var conversation = await _conversations.GetByIdAsync(message.ConversationId, ct)
                           ?? throw new NotFoundException("That conversation does not exist.");

        if (!message.IsVisible)
            throw new ConflictException("That message is already removed.");

        await RemoveAsync(message, conversation, _currentUser.UserId, DeletionKind.Moderation, reason, ct);

        _logger.LogWarning(
            "Private message {MessageId} removed by moderation. Reason: {Reason}", messageId, reason);
    }

    public async Task<long> ModerationDeleteAllBySenderAsync(
        Guid senderUserId, string reason, CancellationToken ct = default)
    {
        var removed = await _messages.SoftDeleteAllBySenderAsync(senderUserId, reason, ct);

        _logger.LogWarning(
            "Removed {Count} private message(s) sent by {UserId}. Reason: {Reason}",
            removed, senderUserId, reason);

        return removed;
    }

    private async Task RemoveAsync(
        DirectMessageDocument message, ConversationDocument conversation,
        Guid? actor, DeletionKind kind, string? reason, CancellationToken ct)
    {
        if (!await _messages.SoftDeleteAsync(message.Id, actor, kind, reason, ct))
            throw new ConflictException("That message could no longer be deleted.");

        var recipients = conversation.Participants.Select(p => p.UserId).ToArray();
        var at = DateTime.UtcNow;

        await _broadcaster.MessageDeletedAsync(
            conversation.Id, message.Id, kind, at, recipients);

        // Recompute the sidebar preview when the newest message was the one removed.
        if (conversation.LastMessage?.MessageId == message.Id)
        {
            var newest = await _messages.GetNewestVisibleAsync(conversation.Id, ct);

            var replacement = newest is null
                ? null
                : new LastMessageSummary
                {
                    MessageId = newest.Id,
                    Preview = Truncate(newest.Content, PreviewLength),
                    SenderId = newest.Sender.UserId,
                    SenderName = newest.Sender.AnonymousName,
                    SentAt = newest.SentAt
                };

            await _conversations.ReplaceLastMessageAsync(conversation.Id, replacement, ct);

            foreach (var participant in conversation.Participants)
            {
                await _broadcaster.ConversationUpdatedAsync(
                    participant.UserId,
                    new ConversationUpdatedDto(
                        conversation.Id,
                        participant.UnreadCount,
                        replacement is null
                            ? null
                            : new LastMessageDto(
                                replacement.MessageId, replacement.Preview, replacement.SenderName,
                                replacement.SenderId == participant.UserId, replacement.SentAt)));
            }
        }

        await _notifications.DeleteBySourceAsync(message.Id, ct);
    }

    public async Task<DirectMessageDto> ToggleReactionAsync(
        Guid messageId, string emoji, CancellationToken ct = default)
    {
        if (!ReactionCatalogue.IsAllowed(emoji))
            throw new ValidationException("That is not an available reaction.");

        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        var conversation = await RequireParticipantAsync(message.ConversationId, ct);
        var userId = _currentUser.RequireUserId();

        var updated = await _messages.ToggleReactionAsync(
            messageId, userId, _currentUser.AnonymousName, emoji, ct);

        if (updated is null)
            throw new ConflictException("That reaction could not be applied.");

        var recipients = conversation.Participants.Select(p => p.UserId).ToArray();
        await _broadcaster.ReactionsChangedAsync(ToDto(updated, userId), recipients);

        return ToDto(updated, userId);
    }

    // ── Blocking ────────────────────────────────────────────────────────────────
    // The blocker is always the caller. The old endpoints took blockerId from the
    // query string with no auth, so anyone could block on anyone else's behalf.

    public async Task BlockAsync(Guid otherUserId, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        if (userId == otherUserId)
            throw new ValidationException("You cannot block yourself.");

        await _blocks.BlockAsync(userId, otherUserId, ct);
        _logger.LogInformation("User {UserId} blocked {OtherUserId}.", userId, otherUserId);
    }

    public async Task UnblockAsync(Guid otherUserId, CancellationToken ct = default)
    {
        await _blocks.UnblockAsync(_currentUser.RequireUserId(), otherUserId, ct);
    }

    public Task<IReadOnlyList<Guid>> ListBlockedAsync(CancellationToken ct = default) =>
        _blocks.ListBlockedByAsync(_currentUser.RequireUserId(), ct);

    // ── Mapping ─────────────────────────────────────────────────────────────────

    private static ConversationDto ToDto(
        ConversationDocument c, Guid viewerId,
        HashSet<Guid> blockedByMe, HashSet<Guid> blockedMe)
    {
        var me = c.ParticipantFor(viewerId);
        var other = c.Other(viewerId);

        return new ConversationDto(
            c.Id,
            other?.UserId ?? Guid.Empty,
            other?.AnonymousName ?? "Unknown",
            me?.UnreadCount ?? 0,
            c.LastMessage is null
                ? null
                : new LastMessageDto(
                    c.LastMessage.MessageId,
                    c.LastMessage.Preview,
                    c.LastMessage.SenderName,
                    c.LastMessage.SenderId == viewerId,
                    c.LastMessage.SentAt),
            IsBlockedByMe: other is not null && blockedByMe.Contains(other.UserId),
            HasBlockedMe: other is not null && blockedMe.Contains(other.UserId));
    }

    internal static DirectMessageDto ToDto(DirectMessageDocument m, Guid? viewerId)
    {
        var visible = m.IsVisible;

        return new DirectMessageDto(
            m.Id,
            m.ConversationId,
            m.Sender.AnonymousName,
            IsMine: viewerId is not null && m.Sender.UserId == viewerId,
            Content: visible ? m.Content : string.Empty,
            SentAt: m.SentAt,
            ReadAt: m.ReadAt,
            ReplyTo: m.ReplyTo is null
                ? null
                : new ReplyDto(m.ReplyTo.MessageId, m.ReplyTo.Snippet, m.ReplyTo.AuthorName),
            Reactions: visible
                ? m.Reactions
                    .Where(r => r.UserIds.Count > 0)
                    .Select(r => new ReactionDto(
                        r.Emoji, r.UserIds.Count,
                        viewerId is not null && r.UserIds.Contains(viewerId.Value),
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
