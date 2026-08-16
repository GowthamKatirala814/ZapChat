using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ZapChat.Shared.Mongo;

/// <summary>
/// A service declares its indexes by implementing this. Index creation in Mongo is
/// idempotent, so this runs on every startup and is safe to re-run.
/// </summary>
public interface IMongoIndexProvider
{
    Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct);
}

/// <summary>
/// Creates every declared index at startup. Runs before the app serves traffic.
/// Index errors are logged and swallowed by default — a missing index degrades
/// performance but should not stop a developer's cold start. Set
/// Mongo:FailFastOnIndexError=true in production.
/// </summary>
public sealed class MongoIndexBootstrapper : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MongoIndexBootstrapper> _logger;

    public MongoIndexBootstrapper(IServiceProvider services, ILogger<MongoIndexBootstrapper> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var database = sp.GetRequiredService<IMongoDatabase>();
        var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
        var providers = sp.GetServices<IMongoIndexProvider>().ToList();

        if (providers.Count == 0)
        {
            _logger.LogWarning("No IMongoIndexProvider registered — no indexes will be created.");
            return;
        }

        foreach (var provider in providers)
        {
            try
            {
                await provider.CreateIndexesAsync(database, cancellationToken);
                _logger.LogInformation(
                    "Mongo indexes ensured by {Provider} on database {Database}.",
                    provider.GetType().Name, options.Database);
            }
            catch (Exception ex)
            {
                if (options.FailFastOnIndexError) throw;
                _logger.LogError(ex,
                    "Failed to create Mongo indexes via {Provider}. Continuing without them.",
                    provider.GetType().Name);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
