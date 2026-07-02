using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/gemini-moderation")]
public class GeminiModerationController : ControllerBase
{
    private readonly IGeminiModerationService _geminiModerationService;
    private readonly ILogger<GeminiModerationController> _logger;

    public GeminiModerationController(
        IGeminiModerationService geminiModerationService,
        ILogger<GeminiModerationController> logger)
    {
        _geminiModerationService = geminiModerationService;
        _logger = logger;
    }

    [HttpPost("moderate")]
    public async Task<IActionResult> ModerateContent([FromBody] GeminiModerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Content cannot be empty.");
        }

        var result = await _geminiModerationService.ModerateContentAsync(request);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetUsageStats()
    {
        var stats = await _geminiModerationService.GetUsageStatsAsync();
        return Ok(stats);
    }
}
