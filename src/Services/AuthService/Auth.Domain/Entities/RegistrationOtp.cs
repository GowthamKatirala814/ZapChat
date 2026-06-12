namespace Auth.Domain.Entities;

/// <summary>
/// Temporary record holding pending registration data until Step 3 (CompleteRegistration) succeeds.
/// No User entity exists until CompleteRegistration is called and succeeds.
/// Deleted immediately after a successful CompleteRegistration.
/// </summary>
public class RegistrationOtp
{
    public Guid Id { get; set; }

    /// <summary>The email address being verified.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Stored temporarily until the User is created.</summary>
    public string FullName { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    /// <summary>6-digit numeric OTP code.</summary>
    public string OtpCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>CreatedAt + 10 minutes.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set to true after the OTP is successfully verified in Step 2.</summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Generated after OTP verification. Passed by the frontend to Step 3 (CompleteRegistration).
    /// Null until OTP is verified.
    /// </summary>
    public string? VerificationToken { get; set; }
}
