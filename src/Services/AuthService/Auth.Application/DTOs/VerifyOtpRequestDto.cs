using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class VerifyOtpRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}

public class VerifyOtpResponseDto
{
    public bool Success { get; set; }
    public string? ResetToken { get; set; }
    public string Message { get; set; } = string.Empty;
}
