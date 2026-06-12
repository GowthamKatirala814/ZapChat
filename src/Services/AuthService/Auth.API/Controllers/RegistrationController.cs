using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

/// <summary>
/// Handles the 3-step email-verified registration flow.
/// All endpoints are public — no [Authorize] required.
/// </summary>
[ApiController]
[Route("api/auth/register")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(
        IRegistrationService registrationService,
        ILogger<RegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger              = logger;
    }

    /// <summary>
    /// Step 1 — Validate account details and send a 6-digit OTP to the provided email.
    /// No account is created at this point.
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiateRegistrationRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _registrationService.InitiateRegistrationAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Registration] InitiateRegistration failed for {Email}", request.Email);
            return StatusCode(500, new InitiateRegistrationResponseDto
            {
                Success = false,
                Message = "An unexpected error occurred. Please try again."
            });
        }
    }

    /// <summary>
    /// Step 2 — Verify the 6-digit OTP.
    /// Returns a one-time VerificationToken on success, which must be passed to Step 3.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyRegistrationOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _registrationService.VerifyRegistrationOtpAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Step 3 — Set password and create the account.
    /// The VerificationToken from Step 2 must be provided.
    /// Account is only created here — never in Steps 1 or 2.
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteRegistrationRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _registrationService.CompleteRegistrationAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
