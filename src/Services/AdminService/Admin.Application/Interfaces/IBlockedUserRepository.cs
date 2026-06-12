using Admin.Domain.Entities;

namespace Admin.Application.Interfaces;

public interface IBlockedUserRepository
{
    Task AddAsync(BlockedUser blockedUser);
    Task RemoveAsync(Guid userId);
    Task<IEnumerable<BlockedUser>> GetAllAsync();
    Task<BlockedUser?> GetByUserIdAsync(Guid userId);
    Task<BlockedUser?> GetByEmailHashAsync(string emailHash);
    Task<bool> IsBlockedAsync(Guid userId);
    Task<int> GetBlockedCountAsync();
    Task<int> GetPermanentlyDeletedCountAsync();

    /// <summary>
    /// Integration contract endpoint: returns all email hashes of permanently deleted users.
    /// Auth Service can call this to prevent re-registration.
    /// </summary>
    Task<IEnumerable<string>> GetAllBlockedEmailHashesAsync();
}
