namespace Auth.Domain.Entities;

public class PasswordResetOtp
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string OtpCode { get; set; } = string.Empty;

    public string? ResetToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    // Navigation property
    public User User { get; set; } = null!;
}
