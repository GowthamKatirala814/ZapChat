namespace Admin.Domain.Entities;

public class BlockedUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// SHA-256 hash of the user's email.
    /// Stored so the user cannot re-register with the same email once permanently blocked.
    /// Integration point: Auth Service can call GET /api/admin/blocked-email-hashes to enforce this.
    /// </summary>
    public string EmailHash { get; set; } = string.Empty;

    /// <summary>
    /// Original user ID from Auth Service (for cross-service reference)
    /// </summary>
    public Guid UserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Admin user ID who performed the block/delete action
    /// </summary>
    public Guid BlockedByAdmin { get; set; }

    /// <summary>
    /// True when the record represents a permanent delete, not a temporary block
    /// </summary>
    public bool IsPermanentDelete { get; set; } = false;
}
