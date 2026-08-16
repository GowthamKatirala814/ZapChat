using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Documents;

public enum OtpPurpose
{
    Registration,
    PasswordReset
}

/// <summary>
/// Collection "otps".
///
/// Replaces two near-identical SQL tables (RegistrationOtps, PasswordResetOtps)
/// with one collection discriminated by <see cref="Purpose"/>. They differed only
/// in which extra fields they carried, which is exactly what a document handles
/// well and a relational schema does not.
///
/// Hardening applied while porting:
///   * The code is stored as a hash, not plaintext.
///   * <see cref="Attempts"/> is enforced, so a 6-digit code can no longer be
///     brute-forced within its 10-minute window.
///   * <see cref="ExpiresAt"/> carries a TTL index, so expired rows are removed
///     by the database instead of accumulating.
/// </summary>
public sealed class OtpDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public OtpPurpose Purpose { get; set; }

    /// <summary>Lower-cased email the code was sent to.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>SHA-256 of the 6-digit code.</summary>
    public string CodeHash { get; set; } = string.Empty;

    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// One-time token handed out after successful verification and required by the
    /// final step. Stored hashed.
    /// </summary>
    public string? FollowUpTokenHash { get; set; }

    public bool IsConsumed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Registration only: details captured in step 1, applied in step 3.</summary>
    public PendingRegistration? Pending { get; set; }

    /// <summary>Password reset only: the account being reset.</summary>
    public Guid? UserId { get; set; }

    public bool IsUsable(DateTime now) =>
        !IsConsumed && ExpiresAt > now && Attempts < MaxAttempts;
}

/// <summary>
/// Account details held between registration step 1 and step 3. No user row exists
/// until the flow completes, so this is the only place they live.
/// </summary>
public sealed class PendingRegistration
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}
