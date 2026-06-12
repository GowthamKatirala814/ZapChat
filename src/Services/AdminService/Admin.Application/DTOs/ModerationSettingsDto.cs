namespace Admin.Application.DTOs;

public class ModerationSettingsDto
{
    public Guid Id { get; set; }
    public int ReportThreshold { get; set; }
    public bool AutoDeleteEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}
