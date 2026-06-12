namespace Admin.Application.DTOs;

public class RecentActivityDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Activity type: UserRegistered, UserBlocked, UserDeleted, RoomCreated,
    /// RoomDeleted, PollCreated, PollClosed, MessageReported
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity (user, room, report, etc.)
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
