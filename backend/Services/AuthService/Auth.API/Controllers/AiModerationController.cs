using System.ComponentModel.DataAnnotations;
using Auth.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Shared.Auth;

namespace Auth.API.Controllers;

/// <summary>
/// AI content classification.
///
/// Both routes previously had no authorization at all and were exposed through the
/// gateway, so anyone could burn the paid Gemini quota — and because the moderation
/// gate fails open, exhausting the quota disabled moderation platform-wide.
///
/// Classification now requires the Admin role, which sibling services obtain via a
/// service token. Health reporting requires an administrator.
/// </summary>
[ApiController]
[Route("api/ai-moderation")]
public sealed class AiModerationController : ControllerBase
{
    private readonly IAiModerationService _moderation;

    public AiModerationController(IAiModerationService moderation) => _moderation = moderation;

    public sealed class ClassifyRequest
    {
        [Required, MaxLength(4000)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>Called by Chat and PrivateChat with a service token.</summary>
    [HttpPost("classify")]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<ActionResult<AiModerationResult>> Classify(
        [FromBody] ClassifyRequest request, CancellationToken ct)
        => Ok(await _moderation.ClassifyAsync(request.Content, ct));

    /// <summary>Powers the admin AI-health page.</summary>
    [HttpGet("health")]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<ActionResult<AiHealthDto>> Health(CancellationToken ct)
        => Ok(await _moderation.GetHealthAsync(ct));
}
