using Admin.Domain.Entities;

namespace Admin.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetByTargetAsync(string targetType, string targetId);
    Task<IEnumerable<AuditLog>> GetByPerformedByAsync(Guid adminId);
    Task<int> GetTotalCountAsync();
}
