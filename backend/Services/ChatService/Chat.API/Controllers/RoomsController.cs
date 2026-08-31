using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Results;
using ZapChat.Shared.Realtime;

namespace Chat.API.Controllers;

/// <summary>
/// Rooms and their message history.
///
/// Every action requires an authenticated caller (deny-by-default) and every
/// room-scoped action re-checks read access in the service layer.
/// </summary>
[ApiController]
[Route("api/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly IRoomService _rooms;
    private readonly IMessageService _messages;

    public RoomsController(IRoomService rooms, IMessageService messages)
    {
        _rooms = rooms;
        _messages = messages;
    }

    /// <summary>
    /// The reactions this platform accepts.
    ///
    /// Published so the client renders the server's actual list instead of its own copy.
    /// The two had drifted: the picker offered two emoji the server rejected and omitted
    /// four it accepted, which is only discoverable by clicking every button.
    ///
    /// Static per deployment, so it is cacheable and needs no authentication beyond the
    /// service default.
    /// </summary>
    [HttpGet("reaction-options")]
    public ActionResult<IReadOnlyList<ReactionCatalogue.Reaction>> ReactionOptions()
    {
        Response.Headers.CacheControl = "private, max-age=3600";
        return Ok(ReactionCatalogue.All);
    }

    /// <summary>Rooms this caller may see, with their own unread counts.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> List(CancellationToken ct)
        => Ok(await _rooms.GetVisibleRoomsAsync(ct));

    [HttpGet("{roomId:guid}")]
    public async Task<ActionResult<RoomDto>> Get(Guid roomId, CancellationToken ct)
        => Ok(await _rooms.GetRoomAsync(roomId, ct));

    [HttpPost("{roomId:guid}/join")]
    public async Task<ActionResult<RoomDto>> Join(Guid roomId, CancellationToken ct)
        => Ok(await _rooms.JoinAsync(roomId, ct));

    [HttpPost("{roomId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid roomId, CancellationToken ct)
    {
        await _rooms.LeaveAsync(roomId, ct);
        return NoContent();
    }

    /// <summary>Clears the caller's unread count. Identity comes from the token.</summary>
    [HttpPost("{roomId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid roomId, CancellationToken ct)
    {
        await _rooms.MarkReadAsync(roomId, ct);
        return NoContent();
    }

    [HttpGet("{roomId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<RoomMemberDto>>> Members(
        Guid roomId, CancellationToken ct)
        => Ok(await _rooms.GetMembersAsync(roomId, ct));

    /// <summary>
    /// Newest-first page of history. Cursor-paginated: pass the previous response's
    /// nextCursor as ?before=. The old endpoint returned the entire room history
    /// unbounded and unauthenticated.
    /// </summary>
    [HttpGet("{roomId:guid}/messages")]
    public async Task<ActionResult<CursorPage<MessageDto>>> Messages(
        Guid roomId,
        [FromQuery] string? before,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
        => Ok(await _messages.GetHistoryAsync(roomId, before, limit, ct));

    /// <summary>
    /// Sends a message over REST. Same code path as the hub, so both produce identical
    /// persistence, moderation and broadcasts.
    /// </summary>
    [HttpPost("{roomId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> Send(
        Guid roomId, [FromBody] SendMessageRequest request, CancellationToken ct)
        => Ok(await _messages.SendAsync(roomId, request, ct));

    /// <summary>
    /// Called by Auth after a registration completes. Requires the Admin role, which
    /// service tokens carry.
    /// </summary>
    [HttpPost("internal/join-defaults/{userId:guid}")]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<IActionResult> JoinDefaults(Guid userId, CancellationToken ct)
    {
        await _rooms.JoinDefaultRoomsAsync(userId, ct);
        return NoContent();
    }
}

/// <summary>Individual message operations.</summary>
[ApiController]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;
    private readonly IRoomService _rooms;

    public MessagesController(IMessageService messages, IRoomService rooms)
    {
        _messages = messages;
        _rooms = rooms;
    }

    [HttpGet("{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> Get(Guid messageId, CancellationToken ct)
        => Ok(await _messages.GetAsync(messageId, ct));

    [HttpPut("{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> Edit(
        Guid messageId, [FromBody] EditMessageRequest request, CancellationToken ct)
        => Ok(await _messages.EditAsync(messageId, request, ct));

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid messageId, CancellationToken ct)
    {
        await _messages.DeleteAsync(messageId, ct);
        return NoContent();
    }

    /// <summary>Adds or removes the caller's reaction. The server decides which.</summary>
    [HttpPost("{messageId:guid}/reactions")]
    public async Task<ActionResult<MessageDto>> React(
        Guid messageId, [FromBody] ReactRequest request, CancellationToken ct)
        => Ok(await _messages.ToggleReactionAsync(messageId, request.Emoji, ct));

    /// <summary>Who has read this message. Previously always returned an empty list.</summary>
    [HttpGet("{messageId:guid}/read-by")]
    public async Task<ActionResult<IReadOnlyList<ReadReceiptDto>>> ReadBy(
        Guid messageId, CancellationToken ct)
        => Ok(await _rooms.GetReadReceiptsAsync(messageId, ct));
}
