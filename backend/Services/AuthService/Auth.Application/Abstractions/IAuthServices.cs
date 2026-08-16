using Auth.Application.DTOs;
using Auth.Domain.Documents;

namespace Auth.Application.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IEmailService
{
    Task SendPasswordResetOtpAsync(string toEmail, string otpCode, string anonymousName);
    Task SendRegistrationOtpAsync(string toEmail, string otpCode, string fullName);
    Task SendAsync(string toEmail, string subject, string htmlBody);
}

/// <summary>Issues access tokens. The only place claims are assembled.</summary>
public interface ITokenService
{
    string CreateAccessToken(UserDocument user);

    /// <summary>Returns the raw token to hand to the client, and the document to persist.</summary>
    (string raw, RefreshTokenDocument document) CreateRefreshToken(Guid userId, Guid? familyId = null);

    string Hash(string value);

    /// <summary>Cryptographically random URL-safe token for OTP follow-up steps.</summary>
    string CreateOpaqueToken();

    /// <summary>Cryptographically secure 6-digit code. Never Random.Shared.</summary>
    string CreateNumericCode(int digits = 6);
}

/// <summary>
/// Allocates unique anonymous names. One implementation, replacing the ~200-line
/// word list that was duplicated verbatim in AuthController and RegistrationService.
/// </summary>
public interface IAnonymousNameService
{
    Task<string> AllocateAsync(CancellationToken ct = default);
}

public interface IAuthenticationService
{
    Task<(AuthResultDto result, string accessToken, string refreshToken)> LoginAsync(
        LoginRequest request, CancellationToken ct = default);

    Task<(AuthResultDto result, string accessToken, string refreshToken)> RefreshAsync(
        string presentedRefreshToken, CancellationToken ct = default);

    Task LogoutAsync(string? presentedRefreshToken, CancellationToken ct = default);
}

public interface IRegistrationService
{
    Task<StepResult> InitiateAsync(InitiateRegistrationRequest request, CancellationToken ct = default);
    Task<StepResult> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task<StepResult> CompleteAsync(CompleteRegistrationRequest request, CancellationToken ct = default);
}

public interface IPasswordResetService
{
    Task<StepResult> RequestAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<StepResult> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task<StepResult> ResetAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

/// <summary>
/// Called by Chat and PrivateChat to classify content. Kept in Auth because the
/// Gemini key and quota tracker live here.
/// </summary>
public interface IAiModerationService
{
    Task<AiModerationResult> ClassifyAsync(string content, CancellationToken ct = default);
    Task<AiHealthDto> GetHealthAsync(CancellationToken ct = default);
}

public sealed record AiModerationResult(
    bool IsSafe,
    string Category,
    double Confidence,
    string Explanation,
    bool EngineAvailable);

public sealed record AiHealthDto(
    string Status,
    int RequestsToday,
    int EstimatedQuota,
    double UsagePercentage,
    int Successful,
    int Failed,
    int BlockedMessages,
    int SafeMessages,
    AiErrorCounters Errors,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    string? LastErrorMessage,
    IReadOnlyList<AiHealthEvent> Events);
