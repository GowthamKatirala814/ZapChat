using Chat.Application.DTOs;

namespace Chat.Application.Interfaces;

/// <summary>
/// Evaluates an incoming message for content policy violations.
/// First applies fast local rules, then calls Gemini AI only when rules pass.
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Evaluates the message in <paramref name="request"/> and returns a moderation decision.
    /// The returned <see cref="ModerationResult"/> must be checked before saving or broadcasting.
    /// </summary>
    Task<ModerationResult> ModerateAsync(ModerationRequest request);
}
