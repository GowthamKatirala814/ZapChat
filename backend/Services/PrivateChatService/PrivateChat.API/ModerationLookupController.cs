using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivateChat.Application;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace PrivateChat.API;

/// <summary>
/// Direct-message lookup for the admin service when a private message is reported.
///
/// Admin-only. Deliberately narrow: it returns one message by id and never the
/// surrounding conversation, so the moderation path can act on reported content without
/// granting anyone the ability to read a private thread.
///
/// This also fixes a route mismatch that made private-message reporting impossible: the
/// admin service called PrivateChat at api/messages/{id} while the actual route was
/// api/privatemessages/{id}, so every private report 404'd and was rejected with
/// "Cannot report a message that does not exist."
/// </summary>
[ApiController]
[Route("api/moderation-lookup")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class ModerationLookupController : ControllerBase
{
    private readonly IDirectMessageRepository _messages;

    public ModerationLookupController(IDirectMessageRepository messages) => _messages = messages;

    public sealed record MessageSnapshot(
        Guid Id, string Content, Guid AuthorUserId, string AuthorAnonymousName,
        Guid? RoomId, string? RoomName);

    [HttpGet("direct-messages/{messageId:guid}")]
    public async Task<ActionResult<MessageSnapshot>> GetMessage(
        Guid messageId, CancellationToken ct)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        return Ok(new MessageSnapshot(
            message.Id,
            message.IsVisible ? message.Content : "(this message has been removed)",
            message.Sender.UserId,
            message.Sender.AnonymousName,
            // A conversation is not a room; there is no room context to report.
            RoomId: null,
            RoomName: null));
    }
}
