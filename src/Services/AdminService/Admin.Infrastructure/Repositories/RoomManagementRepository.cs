using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Repositories;

public class RoomManagementRepository : IRoomManagementRepository
{
    private readonly AdminDbContext _context;

    public RoomManagementRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RoomManagement room)
    {
        await _context.RoomManagements.AddAsync(room);
        await _context.SaveChangesAsync();
    }

    public async Task<RoomManagement?> GetByIdAsync(Guid id)
    {
        return await _context.RoomManagements.FindAsync(id);
    }

    public async Task<IEnumerable<RoomManagement>> GetAllAsync(bool includeDeleted = false)
    {
        var query = _context.RoomManagements.AsQueryable();

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(RoomManagement room)
    {
        _context.RoomManagements.Update(room);
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id, DateTime deletedAt)
    {
        var room = await _context.RoomManagements.FindAsync(id);
        if (room is not null)
        {
            room.IsDeleted = true;
            room.DeletedAt = deletedAt;
            _context.RoomManagements.Update(room);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetActiveRoomCountAsync()
    {
        return await _context.RoomManagements.CountAsync(x => !x.IsDeleted);
    }
}
