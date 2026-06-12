using Admin.Domain.Enums;

namespace Admin.Domain.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public MessageType MessageType { get; set; }
    public Guid MessageAuthorId { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public string MessageAuthorName { get; set; } = string.Empty;
    public Guid ReportedByUserId { get; set; }
    public string ReportedByUserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public bool IsAutoRemoved { get; set; } = false;
}
