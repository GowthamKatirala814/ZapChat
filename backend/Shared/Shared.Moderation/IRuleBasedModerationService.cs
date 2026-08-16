namespace Shared.Moderation;

public interface IRuleBasedModerationService
{
    Task<FallbackModerationResult> ModerateAsync(string content);
}
