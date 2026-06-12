namespace Admin.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Action performed, e.g. "UserBlocked", "UserDeleted", "RoomCreated", "ThresholdChanged"
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of the target entity, e.g. "User", "Room", "Report"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the target entity (stored as string to accommodate different ID types)
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Admin user ID who performed the action
    /// </summary>
    public Guid PerformedBy { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
