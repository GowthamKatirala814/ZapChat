using System.ComponentModel.DataAnnotations;
using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;

namespace Chat.API.Controllers;

/// <summary>
/// Room administration and moderation actions on chat content.
///
/// Routed under /api/chat-admin rather than /api/admin. Five services previously
/// declared a controller at /api/admin, and the gateway routed that prefix to the
/// admin service alone — so all of those endpoints were unreachable from the browser
/// while sitting open to anything that could reach the service port.
/// </summary>
[ApiController]
[Route("api/chat-admin")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class ChatAdminController : ControllerBase
{
    private readonly IRoomService _rooms;
    private readonly IMessageService _messages;
    private readonly IRoomMemberRepository _members;
    private readonly IRoomRepository _roomRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IModerationEventRepository _moderationEvents;

    public ChatAdminController(
        IRoomService rooms,
        IMessageService messages,
        IRoomMemberRepository members,
        IRoomRepository roomRepository,
        IMessageRepository messageRepository,
        IModerationEventRepository moderationEvents)
    {
        _rooms = rooms;
        _messages = messages;
        _members = members;
        _roomRepository = roomRepository;
        _messageRepository = messageRepository;
        _moderationEvents = moderationEvents;
    }

    // ── Rooms ───────────────────────────────────────────────────────────────────

    [HttpGet("rooms")]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> Rooms(
        [FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var rooms = await _roomRepository.ListAsync(includeArchived, ct);

        // Real per-room counts. RoomStatsDto used to hardcode MessagesCount = 0 and
        // ActiveUsers = 0, and MemberCount returned the global active-user total for
        // every room.
        return Ok(rooms.Select(r => new RoomDto(
            r.Id, r.Name, r.Type, r.Branch, r.Description,
            r.MemberCount, r.MessageCount, r.IsArchived, r.CreatedAt,
            r.LastMessage is null
                ? null
                : new LastMessageDto(r.LastMessage.MessageId, r.LastMessage.Preview,
                    r.LastMessage.AuthorName, r.LastMessage.SentAt),
            UnreadCount: 0, IsMember: false)).ToList());
    }

    [HttpPost("rooms")]
    public async Task<ActionResult<RoomDto>> CreateRoom(
        [FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var room = await _rooms.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Rooms), new { }, room);
    }

    [HttpPut("rooms/{roomId:guid}")]
    public async Task<ActionResult<RoomDto>> UpdateRoom(
        Guid roomId, [FromBody] UpdateRoomRequest request, CancellationToken ct)
        => Ok(await _rooms.UpdateAsync(roomId, request, ct));

    /// <summary>
    /// Archives a room. Messages are retained — the old flow hard-deleted the room in
    /// Chat (cascading every message) while Admin only soft-deleted its own copy.
    /// </summary>
    [HttpDelete("rooms/{roomId:guid}")]
    public async Task<IActionResult> ArchiveRoom(Guid roomId, CancellationToken ct)
    {
        await _rooms.ArchiveAsync(roomId, ct);
        return NoContent();
    }

    [HttpPost("rooms/{roomId:guid}/restore")]
    public async Task<IActionResult> RestoreRoom(Guid roomId, CancellationToken ct)
    {
        await _rooms.RestoreAsync(roomId, ct);
        return NoContent();
    }

    [HttpGet("rooms/{roomId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<RoomMemberDto>>> Members(
        Guid roomId, CancellationToken ct)
        => Ok(await _rooms.GetMembersAsync(roomId, ct));

    // ── Moderation ──────────────────────────────────────────────────────────────

    public sealed class RemoveMessageRequest
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Removes a message and tells every client. The old admin delete only marked
    /// reports reviewed and wrote an audit entry — the message stayed visible.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> RemoveMessage(
        Guid messageId, [FromBody] RemoveMessageRequest request, CancellationToken ct)
    {
        await _messages.ModerationDeleteAsync(messageId, request.Reason, ct);
        return NoContent();
    }

    /// <summary>Removes everything one author posted. Used by automated moderation.</summary>
    [HttpPost("users/{userId:guid}/remove-messages")]
    public async Task<ActionResult<object>> RemoveAllByAuthor(
        Guid userId, [FromBody] RemoveMessageRequest request, CancellationToken ct)
    {
        var removed = await _messages.ModerationDeleteAllByAuthorAsync(userId, request.Reason, ct);
        await _members.DeactivateAllForUserAsync(userId, ct);

        return Ok(new { removed });
    }

    [HttpGet("moderation/stats")]
    public async Task<ActionResult<ModerationStatsDto>> ModerationStats(CancellationToken ct)
        => Ok(await _moderationEvents.GetStatsAsync(ct));

    // ── Analytics ───────────────────────────────────────────────────────────────
    // Aggregations against the one database, replacing per-chart HTTP round trips
    // that loaded whole tables into memory.

    [HttpGet("analytics/summary")]
    public async Task<ActionResult<object>> Summary(CancellationToken ct) => Ok(new
    {
        totalRooms = await _roomRepository.CountAsync(includeArchived: false, ct),
        totalMessages = await _messageRepository.CountAsync(ct)
    });

    [HttpGet("analytics/messages-per-day")]
    public async Task<ActionResult<object>> MessagesPerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var counts = (await _messageRepository.CountByDayAsync(days, ct))
            .ToDictionary(x => x.Day.Date, x => x.Count);

        // Zero-filled so the chart has a point per day.
        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        return Ok(Enumerable.Range(0, Math.Clamp(days, 1, 365))
            .Select(offset =>
            {
                var day = since.AddDays(offset);
                return new
                {
                    date = day.ToString("yyyy-MM-dd"),
                    count = counts.GetValueOrDefault(day)
                };
            }));
    }

    [HttpGet("analytics/messages-per-hour")]
    public async Task<ActionResult<object>> MessagesPerHour(CancellationToken ct)
    {
        var counts = await _messageRepository.CountByHourAsync(ct);
        return Ok(counts.Select(c => new { hour = c.Hour, count = c.Count }));
    }

    [HttpGet("analytics/top-rooms")]
    public async Task<ActionResult<object>> TopRooms(
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var counts = await _messageRepository.CountByRoomAsync(top, ct);

        var rooms = (await _roomRepository.GetManyAsync(
                counts.Select(c => c.RoomId).ToList(), ct))
            .ToDictionary(r => r.Id, r => r.Name);

        return Ok(counts.Select(c => new
        {
            roomId = c.RoomId,
            roomName = rooms.GetValueOrDefault(c.RoomId, "(deleted room)"),
            messageCount = c.Count
        }));
    }

    /// <summary>Most active participants, by anonymous name. Never by real identity.</summary>
    [HttpGet("analytics/top-authors")]
    public async Task<ActionResult<object>> TopAuthors(
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var counts = await _messageRepository.CountByAuthorAsync(top, ct);
        return Ok(counts.Select(c => new { anonymousName = c.AnonymousName, messageCount = c.Count }));
    }
}
