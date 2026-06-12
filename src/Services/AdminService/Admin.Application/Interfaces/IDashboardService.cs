using Admin.Application.DTOs;

namespace Admin.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 20);
}
