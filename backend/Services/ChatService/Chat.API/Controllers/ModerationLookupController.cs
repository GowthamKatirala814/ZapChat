using Chat.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Chat.API.Controllers;

/// <summary>
/// Message lookup for the admin service, used when a report is filed so the report can
/// snapshot what was reported and attribute it to an author.
///
/// Admin-only, so it is reachable by a service token and by administrators, and by
/// nobody else. This replaces the old GET /api/messages/{id}, which was
/// [AllowAnonymous], returned full message content to any caller, and always reported
/// senderId as Guid.Empty — forcing the admin service to guess the author by name.
/// </summary>
[ApiController]
[Route("api/moderation-lookup")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class ModerationLookupController : ControllerBase
{
    private readonly IMessageRepository _messages;
    private readonly IRoomRepository _rooms;

    public ModerationLookupController(IMessageRepository messages, IRoomRepository rooms)
    {
        _messages = messages;
        _rooms = rooms;
    }

    /// <summary>The snapshot shape the admin service consumes.</summary>
    public sealed record MessageSnapshot(
        Guid Id, string Content, Guid AuthorUserId, string AuthorAnonymousName,
        Guid? RoomId, string? RoomName);

    [HttpGet("messages/{messageId:guid}")]
    public async Task<ActionResult<MessageSnapshot>> GetMessage(
        Guid messageId, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        // An already-removed message can still be reported on: the report queue needs
        // the snapshot even if the content is gone, so the author is still attributable.
        var room = await _rooms.GetByIdAsync(message.RoomId, ct);

        return Ok(new MessageSnapshot(
            message.Id,
            message.IsVisible ? message.Content : "(this message has been removed)",
            message.Author.UserId,
            message.Author.AnonymousName,
            message.RoomId,
            room?.Name));
    }
}
