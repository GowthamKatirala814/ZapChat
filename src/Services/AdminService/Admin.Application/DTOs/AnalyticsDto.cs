namespace Admin.Application.DTOs;

public class AnalyticsDto
{
    /// <summary>Rooms ranked by number of reports (proxy for activity)</summary>
    public List<ChartDataPointDto> MostActiveRooms { get; set; } = new();

    /// <summary>Users ranked by number of reports they filed</summary>
    public List<ChartDataPointDto> MostActiveUsers { get; set; } = new();

    /// <summary>
    /// Daily message counts.
    /// Integration point: requires ChatService message history endpoint.
    /// </summary>
    public List<ChartDataPointDto> DailyMessages { get; set; } = new();

    /// <summary>
    /// Daily poll creation counts.
    /// Integration point: requires PollService endpoint.
    /// </summary>
    public List<ChartDataPointDto> DailyPolls { get; set; } = new();

    /// <summary>
    /// Daily notification counts.
    /// Integration point: requires NotificationService endpoint.
    /// </summary>
    public List<ChartDataPointDto> DailyNotifications { get; set; } = new();

    /// <summary>Reports submitted per day — sourced from Admin DB</summary>
    public List<ChartDataPointDto> DailyReports { get; set; } = new();

    /// <summary>
    /// User registrations over time.
    /// Sourced from Admin's BlockedUser/AuditLog records (approximation).
    /// Full data requires Auth Service CreatedAt in its user list API.
    /// </summary>
    public List<ChartDataPointDto> UserGrowth { get; set; } = new();
}
