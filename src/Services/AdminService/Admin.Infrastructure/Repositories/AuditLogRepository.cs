using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AdminDbContext _context;

    public AuditLogRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        return await _context.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByTargetAsync(string targetType, string targetId)
    {
        return await _context.AuditLogs
            .Where(x => x.EntityType == targetType && x.EntityId == targetId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByPerformedByAsync(Guid adminId)
    {
        return await _context.AuditLogs
            .Where(x => x.PerformedBy == adminId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.AuditLogs.CountAsync();
    }
}
