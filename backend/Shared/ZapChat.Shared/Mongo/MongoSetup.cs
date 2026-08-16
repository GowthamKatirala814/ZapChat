using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ZapChat.Shared.Mongo;

/// <summary>
/// Registers the Mongo client and the global serialization conventions.
///
/// The conventions are applied exactly once per process, before any document is
/// mapped. Two of them are load-bearing and cannot be changed after data exists:
///
///   * Guid  -> stored as a BSON string, so an id is readable in the shell and is
///             the same value in every service that references it.
///   * DateTime -> always UTC on the way in and on the way out.
/// </summary>
public static class MongoSetup
{
    private static readonly object Gate = new();
    private static bool _conventionsRegistered;

    public static IServiceCollection AddZapChatMongo(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RegisterConventions();

        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "Mongo:ConnectionString must be configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Database),
                "Mongo:Database must be configured.")
            .ValidateOnStart();

        // One IMongoClient per process. The driver pools connections internally,
        // so this must be a singleton — creating clients per request leaks sockets.
        services.AddSingleton<IMongoClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(opts.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(settings);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return sp.GetRequiredService<IMongoClient>().GetDatabase(opts.Database);
        });

        return services;
    }

    /// <summary>
    /// Global BSON conventions. Idempotent and thread-safe — several services may
    /// be hosted in one process during tests.
    /// </summary>
    public static void RegisterConventions()
    {
        lock (Gate)
        {
            if (_conventionsRegistered) return;
            _conventionsRegistered = true;

            // Guids as strings. Must be registered before any class map is built.
            BsonSerializer.TryRegisterSerializer(
                new GuidSerializer(BsonType.String));
            BsonSerializer.TryRegisterSerializer(
                new NullableSerializer<Guid>(new GuidSerializer(BsonType.String)));

            // DateTimes always round-trip as UTC.
            BsonSerializer.TryRegisterSerializer(
                new DateTimeSerializer(DateTimeKind.Utc));
            BsonSerializer.TryRegisterSerializer(
                new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));

            ConventionRegistry.Register(
                "zapchat",
                new ConventionPack
                {
                    // camelCase field names — matches the JSON the API returns,
                    // so documents read the same in mongosh and in the browser.
                    new CamelCaseElementNameConvention(),
                    // Additive schema changes don't break reads of older documents.
                    new IgnoreExtraElementsConvention(true),
                    // Keeps documents small: absent means null/default.
                    new IgnoreIfNullConvention(true),
                    new EnumRepresentationConvention(BsonType.String),
                },
                _ => true);
        }
    }
}
