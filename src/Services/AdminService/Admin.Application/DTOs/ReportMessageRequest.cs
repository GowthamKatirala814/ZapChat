using Admin.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class ReportMessageRequest
{
    [Required]
    public Guid MessageId { get; set; }

    [Required]
    public MessageType MessageType { get; set; }

    [Required]
    public Guid ReportedByUserId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
