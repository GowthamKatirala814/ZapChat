using Admin.Domain.Entities;

namespace Admin.Application.Interfaces;

public interface IRoomMembershipRepository
{
    Task AddAsync(RoomMembership membership);
    Task AddRangeAsync(IEnumerable<RoomMembership> memberships);
    Task RemoveAsync(Guid roomId, Guid userId);
    Task RemoveAllForRoomAsync(Guid roomId);
    Task<IEnumerable<RoomMembership>> GetByRoomIdAsync(Guid roomId);
    Task<IEnumerable<RoomMembership>> GetByUserIdAsync(Guid userId);
    Task<int> GetMemberCountAsync(Guid roomId);
    Task<int> GetActiveMemberCountAsync(Guid roomId, IEnumerable<Guid> blockedUserIds);
    Task<bool> IsMemberAsync(Guid roomId, Guid userId);
}
