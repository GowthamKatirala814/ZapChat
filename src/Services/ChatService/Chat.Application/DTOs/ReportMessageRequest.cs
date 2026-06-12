namespace Chat.Application.DTOs;

public class ReportMessageRequest
{
    public Guid MessageId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
