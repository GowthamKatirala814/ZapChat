using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Registration step 1 — validate details, email a code. No account created.</summary>
public sealed class InitiateRegistrationRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Department { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Branch { get; set; } = string.Empty;
}

/// <summary>Registration step 2 — verify the code, receive a one-time token.</summary>
public sealed class VerifyOtpRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}

/// <summary>Registration step 3 — set a password, create the account.</summary>
public sealed class CompleteRegistrationRequest
{
    [Required]
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// Server-side policy. Previously only the React form enforced this, so the API
    /// accepted any non-empty string.
    /// </summary>
    [Required, StringLength(128, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Self-service profile edit. Branch is intentionally absent: it gates branch-room
/// access, so it is admin-managed rather than self-asserted.
/// </summary>
public sealed class UpdateProfileRequest
{
    [MaxLength(120)]
    public string? Department { get; set; }
}

/// <summary>Admin-only branch change.</summary>
public sealed class SetBranchRequest
{
    [Required, MaxLength(120)]
    public string Branch { get; set; } = string.Empty;
}

public sealed class SoftDeleteUserRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Uniform envelope for the multi-step flows the React wizard consumes.</summary>
public sealed record StepResult(bool Success, string Message, string? Token = null);
