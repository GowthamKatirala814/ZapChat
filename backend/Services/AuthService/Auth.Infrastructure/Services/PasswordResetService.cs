using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Auth.Domain.Documents;
using Auth.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Errors;

namespace Auth.Infrastructure.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _users;
    private readonly IOtpRepository _otps;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IEmailService _email;
    private readonly OtpResendCooldown _cooldown;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IUserRepository users,
        IOtpRepository otps,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        ITokenService tokens,
        IEmailService email,
        OtpResendCooldown cooldown,
        ILogger<PasswordResetService> logger)
    {
        _users = users;
        _otps = otps;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _tokens = tokens;
        _email = email;
        _cooldown = cooldown;
        _logger = logger;
    }

    /// <summary>
    /// Always reports success. Whether the address is registered must not be
    /// observable here, because unlike registration this endpoint is unauthenticated
    /// and would otherwise be an account-enumeration oracle.
    /// </summary>
    public async Task<StepResult> RequestAsync(
        ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // One message for every outcome, decided before the lookup so it cannot come to
        // depend on what the lookup found. Whether an address is registered must stay
        // unobservable: this endpoint is unauthenticated and the caller supplies the
        // address, so any difference here is an account-enumeration oracle.
        const string always = "If an account with that email exists, a reset code has been sent.";

        var user = await _users.GetByEmailAsync(request.Email, ct);

        if (user is null || !user.CanSignIn)
        {
            _logger.LogInformation(
                "Password reset requested for an unknown or inactive address; no mail sent.");
            return new StepResult(true, always);
        }

        // Per-address cooldown. Note that it returns `always` rather than a 429: telling
        // the caller "you are being rate limited" would itself confirm the account
        // exists, since an unknown address never hits this path. The real client learns
        // the same thing from the countdown it already runs after the first request.
        var pending = await _otps.GetLatestAsync(request.Email, OtpPurpose.PasswordReset, ct);

        if (pending is not null && _cooldown.IsTooSoon(pending.CreatedAt))
        {
            _logger.LogInformation(
                "Password reset for user {UserId} suppressed by the resend cooldown.", user.Id);
            return new StepResult(true, always);
        }

        await _otps.InvalidatePendingAsync(request.Email, OtpPurpose.PasswordReset, ct);

        var code = _tokens.CreateNumericCode();

        await _otps.InsertAsync(new OtpDocument
        {
            Purpose = OtpPurpose.PasswordReset,
            Email = user.Email,
            UserId = user.Id,
            CodeHash = _tokens.Hash(code),
            ExpiresAt = DateTime.UtcNow.Add(OtpLifetime)
        }, ct);

        try
        {
            await _email.SendPasswordResetOtpAsync(
                user.Email, code, user.Anonymous.Name, (int)OtpLifetime.TotalMinutes, ct);
        }
        catch (Exception ex)
        {
            // Invalidate the code that was never delivered, then report success anyway.
            //
            // A 503 here would leak account existence just as surely as a 429 would —
            // only a registered address can reach a code that fails to send. The
            // operator learns about it from the log, which is where a delivery outage
            // belongs; the user sees the same sentence as everyone else.
            await _otps.InvalidatePendingAsync(request.Email, OtpPurpose.PasswordReset, ct);

            _logger.LogError(ex,
                "The password reset email for user {UserId} could not be sent.", user.Id);
        }

        return new StepResult(true, always);
    }

    public async Task<StepResult> VerifyOtpAsync(
        VerifyOtpRequest request, CancellationToken ct = default)
    {
        var otp = await _otps.GetLatestAsync(request.Email, OtpPurpose.PasswordReset, ct);

        if (otp is null || !otp.IsUsable(DateTime.UtcNow))
            throw new ValidationException("That code is invalid or has expired. Request a new one.");

        if (!await _otps.IncrementAttemptsAsync(otp.Id, ct))
        {
            await _otps.ConsumeAsync(otp.Id, ct);
            throw new ValidationException("Too many incorrect attempts. Request a new code.");
        }

        if (!FixedTimeEquals(otp.CodeHash, _tokens.Hash(request.OtpCode)))
            throw new ValidationException("That code is incorrect.");

        var resetToken = _tokens.CreateOpaqueToken();
        await _otps.MarkVerifiedAsync(otp.Id, _tokens.Hash(resetToken), ct);

        return new StepResult(true, "Code verified. You can set a new password now.", resetToken);
    }

    public async Task<StepResult> ResetAsync(
        ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new ValidationException("The passwords do not match.");

        var otp = await _otps.GetByFollowUpTokenAsync(_tokens.Hash(request.ResetToken), ct);

        if (otp is null || otp.UserId is null || !otp.IsUsable(DateTime.UtcNow))
            throw new ValidationException("This reset link is invalid or has expired.");

        if (!await _otps.ConsumeAsync(otp.Id, ct))
            throw new ConflictException("This reset link has already been used.");

        var updated = await _users.SetPasswordHashAsync(
            otp.UserId.Value, _hasher.HashPassword(request.NewPassword), ct);

        if (!updated)
            throw new NotFoundException("That account no longer exists.");

        // A password reset must invalidate existing sessions — otherwise a stolen
        // session survives the very action taken to recover the account.
        var revoked = await _refreshTokens.RevokeAllForUserAsync(
            otp.UserId.Value, "Password was reset.", ct);

        _logger.LogInformation(
            "Password reset for user {UserId}; revoked {Count} refresh token(s).",
            otp.UserId, revoked);

        return new StepResult(true, "Your password has been reset. You can sign in now.");
    }

    private static bool FixedTimeEquals(string a, string b) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a),
            System.Text.Encoding.UTF8.GetBytes(b));
}
