namespace Admin.Application.DTOs;

public class RoomStatsDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    /// <summary>
    /// Total messages in this room.
    /// Integration point: requires ChatService to expose message count endpoint.
    /// Currently returns 0.
    /// </summary>
    public int MessagesCount { get; set; }

    /// <summary>
    /// Currently active users in this room.
    /// Integration point: requires ChatService to expose active users endpoint.
    /// Currently returns 0.
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Number of reports filed against messages in this room.
    /// Sourced directly from Admin Service's ReportedMessages table.
    /// </summary>
    public int ReportsCount { get; set; }
}
