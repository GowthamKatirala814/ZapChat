using PrivateChat.Application.DTOs;

namespace PrivateChat.Application.Interfaces;

/// <summary>
/// Service responsible for validating chat messages against company rules and AI content safety policies.
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Evaluates the message content. Always returns a valid ModerationResult.
    /// Implementation MUST fail-open (return Allow) if external AI dependencies are unavailable.
    /// </summary>
    Task<ModerationResult> ModerateAsync(ModerationRequest request);
}
