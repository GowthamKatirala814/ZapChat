using Admin.Domain.Entities;

namespace Admin.Application.Interfaces;

public interface IModerationSettingsRepository
{
    Task<ModerationSettings?> GetAsync();
    Task<ModerationSettings> GetOrCreateDefaultAsync();
    Task UpdateAsync(ModerationSettings settings);
}
