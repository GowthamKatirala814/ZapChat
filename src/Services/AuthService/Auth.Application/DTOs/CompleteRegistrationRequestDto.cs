using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class CompleteRegistrationRequestDto
{
    [Required]
    public string VerificationToken { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CompleteRegistrationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
