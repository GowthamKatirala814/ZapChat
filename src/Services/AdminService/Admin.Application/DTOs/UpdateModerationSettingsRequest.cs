using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class UpdateModerationSettingsRequest
{
    [Required]
    [Range(1, 100)]
    public int ReportThreshold { get; set; } = 5;

    [Required]
    public bool AutoDeleteEnabled { get; set; } = true;
}
