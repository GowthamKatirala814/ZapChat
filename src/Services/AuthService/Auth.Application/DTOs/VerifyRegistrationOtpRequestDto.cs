using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class VerifyRegistrationOtpRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string OtpCode { get; set; } = string.Empty;
}

public class VerifyRegistrationOtpResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Only populated on success. Passed to CompleteRegistration (Step 3).</summary>
    public string? VerificationToken { get; set; }
}
