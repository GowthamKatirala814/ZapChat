using Auth.Application.DTOs;

namespace Auth.Application.Interfaces;

public interface IGeminiModerationService
{
    Task<GeminiModerationResponse> ModerateContentAsync(GeminiModerationRequest request);
    Task<object> GetUsageStatsAsync();
}
