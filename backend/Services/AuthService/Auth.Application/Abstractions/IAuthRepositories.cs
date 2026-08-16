using Auth.Domain.Documents;

namespace Auth.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task InsertAsync(RefreshTokenDocument token, CancellationToken ct = default);
    Task<RefreshTokenDocument?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    Task<bool> RevokeAsync(Guid id, string reason, CancellationToken ct = default);

    /// <summary>
    /// Revokes every token in a rotation chain. Called when an already-used token is
    /// presented, which indicates the token was stolen.
    /// </summary>
    Task<long> RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct = default);

    Task<long> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default);
}

public interface IOtpRepository
{
    Task InsertAsync(OtpDocument otp, CancellationToken ct = default);

    /// <summary>Most recent usable code for an address and purpose.</summary>
    Task<OtpDocument?> GetLatestAsync(
        string email, OtpPurpose purpose, CancellationToken ct = default);

    Task<OtpDocument?> GetByFollowUpTokenAsync(string tokenHash, CancellationToken ct = default);

    Task<bool> IncrementAttemptsAsync(Guid id, CancellationToken ct = default);

    Task<bool> MarkVerifiedAsync(Guid id, string followUpTokenHash, CancellationToken ct = default);

    Task<bool> ConsumeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Clears earlier unverified codes when a new one is requested.</summary>
    Task<long> InvalidatePendingAsync(
        string email, OtpPurpose purpose, CancellationToken ct = default);
}

public interface IAiUsageRepository
{
    Task<AiUsageDocument> GetOrCreateTodayAsync(CancellationToken ct = default);

    /// <summary>Atomically bumps counters for one moderation outcome.</summary>
    Task RecordOutcomeAsync(
        bool success, bool blocked, string? errorKind, string? errorMessage,
        CancellationToken ct = default);

    Task AppendHealthEventAsync(
        string previousStatus, string newStatus, string message, CancellationToken ct = default);

    Task<IReadOnlyList<AiUsageDocument>> GetRecentAsync(int days, CancellationToken ct = default);
}
