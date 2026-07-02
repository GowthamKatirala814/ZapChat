using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(200, ErrorMessage = "Full name must not exceed 200 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [MaxLength(100, ErrorMessage = "Department must not exceed 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch is required.")]
    [MaxLength(100, ErrorMessage = "Branch must not exceed 100 characters.")]
    public string Branch { get; set; } = string.Empty;
}
