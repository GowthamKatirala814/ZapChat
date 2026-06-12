using Admin.Application.DTOs;
using Admin.Domain.Enums;

namespace Admin.Application.Interfaces;

public interface IModerationService
{
    Task<IEnumerable<ReportDto>> GetReportsAsync(ReportStatus? statusFilter = null, bool? isAutoRemoved = null, int page = 1, int pageSize = 50);
    Task<ReportDto?> GetReportByIdAsync(Guid reportId);
    Task<ReportDto> SubmitReportAsync(ReportMessageRequest request);
    Task MarkReportAsReviewedAsync(Guid reportId, Guid adminId);
    Task IgnoreReportAsync(Guid reportId, Guid adminId);
    Task DeleteMessageAsync(Guid messageId, Guid adminId);
    Task DeleteUserAsync(Guid userId, Guid adminId);
    Task<ModerationSettingsDto> GetSettingsAsync();
    Task<ModerationSettingsDto> UpdateSettingsAsync(UpdateModerationSettingsRequest request, Guid adminId);
}
