namespace ZapChat.Shared.Mongo;

/// <summary>
/// Bound from the "Mongo" configuration section. Every service reads its own
/// section so services can share one server but never share a database.
/// </summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    /// <summary>Connection string. Defaults to a local standalone mongod.</summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>Database name — one per service.</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Fail startup if indexes cannot be created. Off in dev so a cold start still works.</summary>
    public bool FailFastOnIndexError { get; set; }
}
