using Admin.Domain.Enums;

namespace Admin.Application.DTOs;

public class ReportDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public Guid MessageAuthorId { get; set; }
    public string MessageAuthorName { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public string MessageTypeName { get; set; } = string.Empty;
    public Guid ReportedByUserId { get; set; }
    public string ReportedByUserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public ReportStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsAutoRemoved { get; set; }
}
