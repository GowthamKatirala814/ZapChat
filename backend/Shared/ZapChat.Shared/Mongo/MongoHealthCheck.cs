using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ZapChat.Shared.Mongo;

/// <summary>
/// Actually pings MongoDB. The old /health endpoints returned a hardcoded
/// {status:"healthy"} literal, so a service with a dead database reported healthy.
/// </summary>
public sealed class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IMongoDatabase database) => _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cts.Token);

            return HealthCheckResult.Healthy($"MongoDB '{_database.DatabaseNamespace.DatabaseName}' reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"MongoDB '{_database.DatabaseNamespace.DatabaseName}' unreachable.", ex);
        }
    }
}
