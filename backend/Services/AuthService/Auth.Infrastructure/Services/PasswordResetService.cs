using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Auth.Domain.Documents;
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
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IUserRepository users,
        IOtpRepository otps,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        ITokenService tokens,
        IEmailService email,
        ILogger<PasswordResetService> logger)
    {
        _users = users;
        _otps = otps;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _tokens = tokens;
        _email = email;
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
        // One message, computed before the lookup and returned by every path below, so
        // it cannot accidentally come to depend on whether the account exists. Only the
        // development reveal deviates from it, and that is deliberate and gated twice.
        var always = _email.DeliversToLog
            ? "No email was sent: this server is using the log transport. " +
              "If an account exists, the code is in the auth service log."
            : "If an account with that email exists, a reset code has been sent.";

        var user = await _users.GetByEmailAsync(request.Email, ct);

        if (user is null || !user.CanSignIn)
        {
            _logger.LogInformation(
                "Password reset requested for an unknown or inactive address; no mail sent.");
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
            await _email.SendPasswordResetOtpAsync(user.Email, code, user.Anonymous.Name);
        }
        catch (Exception ex)
        {
            // Logged, but still reported as success — the response must not differ.
            _logger.LogError(ex, "Failed to send a password reset code to user {UserId}.", user.Id);
        }

        if (_email.RevealsCodes)
        {
            // Development host with the log transport, and nothing else — see
            // EmailOptions.RevealCodesInResponses for the two gates that guard this.
            //
            // Note what it costs: this reply differs from the one an unknown address
            // gets, so the constant-response property above no longer holds here. That
            // is acceptable *only* on such a host, because the code is already sitting in
            // a plaintext log there — there is no confidentiality left to protect. On
            // every other host this branch does not execute and all callers get `always`.
            return new StepResult(true, $"Development mode — no email was sent. Your reset code is {code}.");
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
