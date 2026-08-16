using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

/// <summary>
/// The three-step registration flow. All steps are anonymous by necessity — the
/// caller has no account yet.
/// </summary>
[ApiController]
[Route("api/auth/register")]
[AllowAnonymous]
public sealed class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registration;

    public RegistrationController(IRegistrationService registration) =>
        _registration = registration;

    /// <summary>Step 1 — validate details and email a 6-digit code. No account is created.</summary>
    [HttpPost("initiate")]
    public async Task<ActionResult<StepResult>> Initiate(
        [FromBody] InitiateRegistrationRequest request, CancellationToken ct)
        => Ok(await _registration.InitiateAsync(request, ct));

    /// <summary>Step 2 — verify the code and receive a one-time token for step 3.</summary>
    [HttpPost("verify-otp")]
    public async Task<ActionResult<StepResult>> VerifyOtp(
        [FromBody] VerifyOtpRequest request, CancellationToken ct)
        => Ok(await _registration.VerifyOtpAsync(request, ct));

    /// <summary>Step 3 — set a password. The account is created here and only here.</summary>
    [HttpPost("complete")]
    public async Task<ActionResult<StepResult>> Complete(
        [FromBody] CompleteRegistrationRequest request, CancellationToken ct)
        => Ok(await _registration.CompleteAsync(request, ct));
}

/// <summary>Forgot-password flow. Also necessarily anonymous.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _reset;

    public PasswordResetController(IPasswordResetService reset) => _reset = reset;

    [HttpPost("forgot-password")]
    public async Task<ActionResult<StepResult>> Forgot(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
        => Ok(await _reset.RequestAsync(request, ct));

    [HttpPost("verify-otp")]
    public async Task<ActionResult<StepResult>> VerifyOtp(
        [FromBody] VerifyOtpRequest request, CancellationToken ct)
        => Ok(await _reset.VerifyOtpAsync(request, ct));

    [HttpPost("reset-password")]
    public async Task<ActionResult<StepResult>> Reset(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
        => Ok(await _reset.ResetAsync(request, ct));
}
