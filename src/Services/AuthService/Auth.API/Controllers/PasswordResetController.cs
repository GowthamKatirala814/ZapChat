using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        IPasswordResetService passwordResetService,
        ILogger<PasswordResetController> logger)
    {
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    /// <summary>
    /// Step 1 — User submits their email. If found, a 6-digit OTP is sent.
    /// Always returns 200 OK regardless of whether the email exists (security).
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _passwordResetService.SendOtpAsync(request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PasswordReset] SendOtp failed for {Email}", request.Email);
            // Do not leak internal errors — still return success message
        }

        return Ok(new ForgotPasswordResponseDto
        {
            Success = true,
            Message = "If an account with that email exists, a reset code has been sent."
        });
    }

    /// <summary>
    /// Step 2 — User submits email + OTP. Returns a one-time reset token on success.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resetToken = await _passwordResetService.VerifyOtpAsync(request.Email, request.OtpCode);

        if (resetToken is null)
        {
            return BadRequest(new VerifyOtpResponseDto
            {
                Success = false,
                Message = "Invalid or expired OTP. Please request a new code."
            });
        }

        return Ok(new VerifyOtpResponseDto
        {
            Success    = true,
            ResetToken = resetToken,
            Message    = "OTP verified. You may now reset your password."
        });
    }

    /// <summary>
    /// Step 3 — User submits the reset token + new password. Hashes and saves it.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new ResetPasswordResponseDto
            {
                Success = false,
                Message = "Passwords do not match."
            });
        }

        var success = await _passwordResetService.ResetPasswordAsync(
            request.ResetToken,
            request.NewPassword);

        if (!success)
        {
            return BadRequest(new ResetPasswordResponseDto
            {
                Success = false,
                Message = "Invalid or expired reset token. Please start the process again."
            });
        }

        return Ok(new ResetPasswordResponseDto
        {
            Success = true,
            Message = "Password reset successfully. You can now log in."
        });
    }
}
