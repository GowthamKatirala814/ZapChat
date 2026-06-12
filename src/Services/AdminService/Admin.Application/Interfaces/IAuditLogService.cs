using Admin.Application.DTOs;

namespace Admin.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string targetType, string targetId, Guid performedBy);
    Task<IEnumerable<AuditLogDto>> GetLogsAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLogDto>> GetLogsByTargetAsync(string targetType, string targetId);
    Task<int> GetTotalCountAsync();
}
