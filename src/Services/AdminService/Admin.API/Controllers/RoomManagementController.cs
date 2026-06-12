using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Json;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin/rooms")]
[Authorize(Roles = "Admin")]
public class RoomManagementController : ControllerBase
{
    private readonly IRoomManagementService _roomService;
    private readonly IHttpClientFactory _httpClientFactory;

    public RoomManagementController(IRoomManagementService roomService, IHttpClientFactory httpClientFactory)
    {
        _roomService = roomService;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Returns all active rooms. Set includeDeleted=true to include soft-deleted rooms.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRooms([FromQuery] bool includeDeleted = false)
    {
        var rooms = await _roomService.GetRoomsAsync(includeDeleted);
        return Ok(rooms);
    }

    /// <summary>
    /// Returns a single room by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room is null) return NotFound();
        return Ok(room);
    }

    /// <summary>
    /// Returns statistics for a room: report count, and placeholder counts
    /// for messages and active users (requires ChatService integration).
    /// </summary>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoomStats(Guid id)
    {
        try
        {
            var stats = await _roomService.GetRoomStatsAsync(id);
            return Ok(stats);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Creates a new room in the Admin Service.
    /// Sends notifications to all active users about the new room.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = GetAdminId();
        var room = await _roomService.CreateRoomAsync(request, adminId);

        // Send notifications to all active users
        await SendRoomCreationNotificationsAsync(room);

        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
    }

    /// <summary>
    /// Updates an existing room's name and description.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var adminId = GetAdminId();
            var updated = await _roomService.UpdateRoomAsync(id, request, adminId);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft-deletes a room. The record is preserved for audit purposes.
    /// Integration point: ChatService should poll GET /api/admin/rooms to detect deletions.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        try
        {
            var adminId = GetAdminId();
            await _roomService.DeleteRoomAsync(id, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private Guid GetAdminId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Sends room creation notifications to all active users.
    /// </summary>
    private async Task SendRoomCreationNotificationsAsync(RoomDto room)
    {
        try
        {
            // Get all active users from Auth Service
            var httpClient = _httpClientFactory.CreateClient();
            var authResponse = await httpClient.GetFromJsonAsync<List<AuthUserRecord>>("http://localhost:5001/api/auth/users");
            
            if (authResponse != null)
            {
                var activeUsers = authResponse.Where(u => u.IsActive && !u.IsDeleted).ToList();
                var notificationClient = _httpClientFactory.CreateClient();

                foreach (var user in activeUsers)
                {
                    var notificationRequest = new
                    {
                        UserId = user.Id,
                        Title = "New Room Created",
                        Message = $"A new room '{room.Name}' has been created. {room.Description}"
                    };

                    await notificationClient.PostAsJsonAsync("http://localhost:5005/api/notification", notificationRequest);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the room creation
            // In production, you'd use proper logging
            Console.WriteLine($"Failed to send room creation notifications: {ex.Message}");
        }
    }
}

public class AuthUserRecord
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
