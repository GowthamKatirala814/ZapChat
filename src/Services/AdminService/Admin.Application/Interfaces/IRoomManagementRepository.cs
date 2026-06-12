using Admin.Domain.Entities;

namespace Admin.Application.Interfaces;

public interface IRoomManagementRepository
{
    Task AddAsync(RoomManagement room);
    Task<RoomManagement?> GetByIdAsync(Guid id);
    Task<IEnumerable<RoomManagement>> GetAllAsync(bool includeDeleted = false);
    Task UpdateAsync(RoomManagement room);
    Task SoftDeleteAsync(Guid id, DateTime deletedAt);
    Task<int> GetActiveRoomCountAsync();
}
