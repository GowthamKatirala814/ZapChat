using Admin.Application.DTOs;

namespace Admin.Application.Interfaces;

public interface IAnalyticsService
{
    // ── Existing methods (unchanged) ──────────────────────────────────────────

    Task<IEnumerable<ChartDataPointDto>> GetMostActiveRoomsAsync(int top = 10);
    Task<IEnumerable<ChartDataPointDto>> GetMostActiveUsersAsync(int top = 10);
    Task<IEnumerable<ChartDataPointDto>> GetDailyMessagesAsync(int days = 30);
    Task<IEnumerable<ChartDataPointDto>> GetDailyPollsAsync(int days = 30);
    Task<IEnumerable<ChartDataPointDto>> GetDailyNotificationsAsync(int days = 30);
    Task<IEnumerable<ChartDataPointDto>> GetDailyReportsAsync(int days = 30);
    Task<IEnumerable<ChartDataPointDto>> GetUserGrowthAsync(int days = 30);

    Task<IEnumerable<ActiveRoomDto>> GetActiveRoomsAsync(int top = 10);
    Task<IEnumerable<ActiveUserDto>> GetActiveUsersAsync(int top = 10);
    Task<IEnumerable<DailyCountDto>> GetPrivateChatVolumeAsync(int days = 30);
    Task<IEnumerable<MostVotedPollDto>> GetMostVotedPollsAsync(int top = 10);
    Task<IEnumerable<ReportReasonDto>> GetReportReasonsAsync();
    Task<IEnumerable<DailyCountDto>> GetReportTrendsAsync(int days = 30);

    // ── New analytics methods ─────────────────────────────────────────────────

    /// <summary>Room health: report-rate ratio per room, top 10 sorted by report rate descending.</summary>
    Task<IEnumerable<RoomHealthDto>> GetRoomHealthAsync(int top = 10);

    /// <summary>Poll participation: top polls sorted by vote count with participation rate.</summary>
    Task<IEnumerable<PollParticipationDto>> GetPollParticipationAsync(int top = 6);

    /// <summary>Message volume grouped by hour of day (0–23), all-time, returned as 24 items.</summary>
    Task<IEnumerable<HourlyActivityDto>> GetHourlyActivityAsync();

    /// <summary>Keyword-based sentiment distribution (positive/neutral/negative) per room.</summary>
    Task<IEnumerable<RoomSentimentDto>> GetRoomSentimentAsync(int top = 8);
}
