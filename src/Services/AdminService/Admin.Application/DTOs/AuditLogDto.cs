namespace Admin.Application.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
}
