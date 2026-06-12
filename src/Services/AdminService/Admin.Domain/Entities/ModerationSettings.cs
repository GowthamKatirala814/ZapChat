namespace Admin.Domain.Entities;

public class ModerationSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Number of reports on a single message before auto-action is triggered.
    /// Default is 5 per spec.
    /// </summary>
    public int ReportThreshold { get; set; } = 5;

    /// <summary>
    /// When true, messages that reach ReportThreshold are automatically marked as removed
    /// and a moderation event + audit entry are created.
    /// </summary>
    public bool AutoDeleteEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
