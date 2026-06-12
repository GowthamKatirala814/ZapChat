using Admin.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class UpdateReportStatusRequest
{
    [Required]
    public ReportStatus Status { get; set; }
}
