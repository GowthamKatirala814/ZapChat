using Admin.Domain.Entities;
using Admin.Domain.Enums;

namespace Admin.Application.Interfaces;

public interface IReportRepository
{
    Task AddAsync(Report report);
    Task<Report?> GetByIdAsync(Guid id);
    Task<IEnumerable<Report>> GetAllAsync(ReportStatus? statusFilter = null, bool? isAutoRemoved = null, int page = 1, int pageSize = 50);
    Task<IEnumerable<Report>> GetByMessageIdAsync(Guid messageId);
    Task<IEnumerable<Report>> GetByReporterIdAsync(Guid reporterId);
    Task UpdateAsync(Report report);
    Task<int> GetCountByMessageIdAsync(Guid messageId);
    Task<int> GetTotalCountAsync(ReportStatus? statusFilter = null);
    Task<int> GetPendingCountAsync();

    Task<IEnumerable<(DateTime Date, int Count)>> GetDailyCountsAsync(int days = 30);
    Task<IEnumerable<(Guid RoomId, int ReportCount)>> GetReportCountsByRoomAsync();
}
