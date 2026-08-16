using Microsoft.Extensions.DependencyInjection;

namespace Shared.Moderation;

public static class ModerationServiceCollectionExtensions
{
    public static IServiceCollection AddRuleBasedModeration(this IServiceCollection services, string dictionariesPath = "")
    {
        services.AddSingleton<IRuleBasedModerationService>(sp => 
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RuleBasedModerationService>>();
            return new RuleBasedModerationService(logger, dictionariesPath);
        });

        return services;
    }
}
