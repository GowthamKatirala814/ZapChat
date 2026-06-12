using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class ResetPasswordRequestDto
{
    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
