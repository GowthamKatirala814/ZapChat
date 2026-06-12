using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class DeleteUserRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = "Permanently deleted by admin";
}
