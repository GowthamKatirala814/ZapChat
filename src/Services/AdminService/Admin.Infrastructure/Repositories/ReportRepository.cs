using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Domain.Enums;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AdminDbContext _context;

    public ReportRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();
    }

    public async Task<Report?> GetByIdAsync(Guid id)
    {
        return await _context.Reports.FindAsync(id);
    }

    public async Task<IEnumerable<Report>> GetAllAsync(
        ReportStatus? statusFilter = null,
        bool? isAutoRemoved = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Reports.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(x => x.Status == statusFilter.Value);

        if (isAutoRemoved.HasValue)
            query = query.Where(x => x.IsAutoRemoved == isAutoRemoved.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Report>> GetByMessageIdAsync(Guid messageId)
    {
        return await _context.Reports
            .Where(x => x.MessageId == messageId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Report>> GetByReporterIdAsync(Guid reporterId)
    {
        return await _context.Reports
            .Where(x => x.ReportedByUserId == reporterId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Report report)
    {
        _context.Reports.Update(report);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetCountByMessageIdAsync(Guid messageId)
    {
        return await _context.Reports
            .CountAsync(x => x.MessageId == messageId && x.Status == ReportStatus.Pending);
    }

    public async Task<int> GetTotalCountAsync(ReportStatus? statusFilter = null)
    {
        var query = _context.Reports.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(x => x.Status == statusFilter.Value);

        return await query.CountAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.Reports.CountAsync(x => x.Status == ReportStatus.Pending);
    }

    public async Task<IEnumerable<(DateTime Date, int Count)>> GetDailyCountsAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days).Date;

        var results = await _context.Reports
            .Where(x => x.CreatedAt >= since)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return results.Select(r => (r.Date, r.Count));
    }

    public async Task<IEnumerable<(Guid RoomId, int ReportCount)>> GetReportCountsByRoomAsync()
    {
        var results = await _context.Reports
            .Where(x => x.MessageType == MessageType.Room)
            .GroupBy(x => x.MessageId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return results.Select(r => (r.RoomId, r.Count));
    }
}
