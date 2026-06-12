using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Admin.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Repositories;

public class ModerationSettingsRepository : IModerationSettingsRepository
{
    private readonly AdminDbContext _context;

    public ModerationSettingsRepository(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<ModerationSettings?> GetAsync()
    {
        return await _context.ModerationSettings.FirstOrDefaultAsync();
    }

    public async Task<ModerationSettings> GetOrCreateDefaultAsync()
    {
        var settings = await _context.ModerationSettings.FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new ModerationSettings
            {
                Id = Guid.NewGuid(),
                ReportThreshold = 5,
                AutoDeleteEnabled = true,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.ModerationSettings.AddAsync(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task UpdateAsync(ModerationSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.ModerationSettings.Update(settings);
        await _context.SaveChangesAsync();
    }
}
