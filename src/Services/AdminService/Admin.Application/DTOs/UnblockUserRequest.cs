using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class UnblockUserRequest
{
    [Required]
    public Guid UserId { get; set; }
}
