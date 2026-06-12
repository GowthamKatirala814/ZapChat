using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;

namespace Admin.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(string action, string targetType, string targetId, Guid performedBy)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = targetType,
            EntityId = targetId,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow
        };

        await _repository.AddAsync(log);
    }

    public async Task<IEnumerable<AuditLogDto>> GetLogsAsync(int page = 1, int pageSize = 50)
    {
        var logs = await _repository.GetAllAsync(page, pageSize);
        return logs.Select(MapToDto);
    }

    public async Task<IEnumerable<AuditLogDto>> GetLogsByTargetAsync(string targetType, string targetId)
    {
        var logs = await _repository.GetByTargetAsync(targetType, targetId);
        return logs.Select(MapToDto);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _repository.GetTotalCountAsync();
    }

    private static AuditLogDto MapToDto(AuditLog log) => new()
    {
        Id = log.Id,
        Action = log.Action,
        TargetType = log.EntityType,
        TargetId = log.EntityId,
        PerformedBy = log.PerformedBy,
        Timestamp = log.Timestamp
    };
}
