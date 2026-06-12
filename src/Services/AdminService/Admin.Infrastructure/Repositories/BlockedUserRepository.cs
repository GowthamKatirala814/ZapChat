using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Repositories;

public class BlockedUserRepository : IBlockedUserRepository
{
    private readonly AdminDbContext _context;

    public BlockedUserRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BlockedUser blockedUser)
    {
        await _context.BlockedUsers.AddAsync(blockedUser);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid userId)
    {
        var record = await _context.BlockedUsers.FirstOrDefaultAsync(x => x.UserId == userId);
        if (record is not null)
        {
            _context.BlockedUsers.Remove(record);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<BlockedUser>> GetAllAsync()
    {
        return await _context.BlockedUsers
            .OrderByDescending(x => x.BlockedAt)
            .ToListAsync();
    }

    public async Task<BlockedUser?> GetByUserIdAsync(Guid userId)
    {
        return await _context.BlockedUsers.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<BlockedUser?> GetByEmailHashAsync(string emailHash)
    {
        return await _context.BlockedUsers.FirstOrDefaultAsync(x => x.EmailHash == emailHash);
    }

    public async Task<bool> IsBlockedAsync(Guid userId)
    {
        return await _context.BlockedUsers.AnyAsync(x => x.UserId == userId);
    }

    public async Task<int> GetBlockedCountAsync()
    {
        return await _context.BlockedUsers.CountAsync(x => !x.IsPermanentDelete);
    }

    public async Task<int> GetPermanentlyDeletedCountAsync()
    {
        return await _context.BlockedUsers.CountAsync(x => x.IsPermanentDelete);
    }

    public async Task<IEnumerable<string>> GetAllBlockedEmailHashesAsync()
    {
        return await _context.BlockedUsers
            .Where(x => x.IsPermanentDelete && !string.IsNullOrEmpty(x.EmailHash))
            .Select(x => x.EmailHash)
            .ToListAsync();
    }
}
