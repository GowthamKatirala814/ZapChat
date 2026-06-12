using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class UpdateRoomRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}
