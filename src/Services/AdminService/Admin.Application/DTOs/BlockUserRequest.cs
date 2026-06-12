using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class BlockUserRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
