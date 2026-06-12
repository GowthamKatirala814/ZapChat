using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Repositories;

public class RoomMembershipRepository : IRoomMembershipRepository
{
    private readonly AdminDbContext _context;

    public RoomMembershipRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RoomMembership membership)
    {
        await _context.RoomMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<RoomMembership> memberships)
    {
        await _context.RoomMemberships.AddRangeAsync(memberships);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid roomId, Guid userId)
    {
        var membership = await _context.RoomMemberships
            .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);
        
        if (membership is not null)
        {
            _context.RoomMemberships.Remove(membership);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveAllForRoomAsync(Guid roomId)
    {
        var memberships = await _context.RoomMemberships
            .Where(m => m.RoomId == roomId)
            .ToListAsync();
        
        _context.RoomMemberships.RemoveRange(memberships);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<RoomMembership>> GetByRoomIdAsync(Guid roomId)
    {
        return await _context.RoomMemberships
            .Where(m => m.RoomId == roomId && m.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<RoomMembership>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RoomMemberships
            .Where(m => m.UserId == userId && m.IsActive)
            .ToListAsync();
    }

    public async Task<int> GetMemberCountAsync(Guid roomId)
    {
        return await _context.RoomMemberships
            .CountAsync(m => m.RoomId == roomId && m.IsActive);
    }

    public async Task<int> GetActiveMemberCountAsync(Guid roomId, IEnumerable<Guid> blockedUserIds)
    {
        var blockedSet = blockedUserIds.ToHashSet();
        var memberships = await _context.RoomMemberships
            .Where(m => m.RoomId == roomId && m.IsActive)
            .ToListAsync();

        // Filter out blocked users in memory (since blocked users are in a different DB)
        return memberships.Count(m => !blockedSet.Contains(m.UserId));
    }

    public async Task<bool> IsMemberAsync(Guid roomId, Guid userId)
    {
        return await _context.RoomMemberships
            .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);
    }
}
