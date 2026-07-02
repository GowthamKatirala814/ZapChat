using Admin.Application.DTOs;

namespace Admin.Application.Interfaces;

public interface IRoomManagementService
{
    Task<IEnumerable<RoomDto>> GetRoomsAsync(bool includeDeleted = false);
    Task<RoomDto?> GetRoomByIdAsync(Guid roomId);
    Task<RoomDto> CreateRoomAsync(CreateRoomRequest request, Guid adminId);
    Task<RoomDto> UpdateRoomAsync(Guid roomId, UpdateRoomRequest request, Guid adminId);
    Task DeleteRoomAsync(Guid roomId, Guid adminId);
    Task<RoomStatsDto> GetRoomStatsAsync(Guid roomId);
    
    /// <summary>
    /// Adds a user to all existing rooms. Used when a new user registers.
    /// </summary>
    Task AddUserToAllRoomsAsync(Guid userId);

    /// <summary>
    /// Gets all active user IDs and Names for a given room.
    /// </summary>
    Task<IEnumerable<RoomMemberDto>> GetMembersAsync(Guid roomId);
}
