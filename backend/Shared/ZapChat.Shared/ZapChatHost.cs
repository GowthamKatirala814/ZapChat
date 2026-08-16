using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Errors;
using ZapChat.Shared.Http;
using ZapChat.Shared.Mongo;

namespace ZapChat.Shared;

/// <summary>
/// The pieces of Program.cs that were copy-pasted (and drifted) across six
/// services: CORS, compression, Swagger + bearer definition, JSON naming, the
/// exception handler, and the health endpoint.
/// </summary>
public static class ZapChatHost
{
    public const string CorsPolicy = "ZapChatFrontend";

    public sealed record ServiceInfo(string Name, string[] HubPaths);

    public static WebApplicationBuilder AddZapChatDefaults(
        this WebApplicationBuilder builder,
        ServiceInfo service)
    {
        var config = builder.Configuration;

        // Environment variables win over appsettings, so secrets never need to be
        // written to a file. ZAPCHAT_JWT__SECRET -> Jwt:Secret
        config.AddEnvironmentVariables(prefix: "ZAPCHAT_");

        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                // Enums as names, not ordinals. A client reading roomType === 1 breaks
                // the moment a value is inserted into the enum; "Branch" does not.
                o.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"ZapChat {service.Name} API",
                Version = "v1"
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme, Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddResponseCompression(o => o.EnableForHttps = true);
        builder.Services.AddMemoryCache();
        builder.Services.AddProblemDetails();

        // Mongo + auth + service identity
        builder.Services.AddZapChatMongo(config);
        builder.Services.AddZapChatAuth(config, service.HubPaths);
        builder.Services.AddSingleton<IServiceTokenProvider, ServiceTokenProvider>();
        builder.Services.AddHostedService<MongoIndexBootstrapper>();

        builder.Services.Configure<ServiceUrlsOptions>(
            config.GetSection(ServiceUrlsOptions.SectionName));

        builder.Services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongodb", tags: ["ready"]);

        // Allowed frontend origins come from configuration, not from a literal.
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:5173"];

        builder.Services.AddCors(options =>
            options.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        return builder;
    }

    public static WebApplication UseZapChatDefaults(this WebApplication app, ServiceInfo service)
    {
        // First in the pipeline so it can catch everything below it.
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseResponseCompression();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Liveness: is the process up.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = (ctx, _) =>
            {
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    $"{{\"status\":\"healthy\",\"service\":\"{service.Name}\"}}");
            }
        }).AllowAnonymous();

        // Readiness: is MongoDB actually reachable.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var entries = string.Join(",", report.Entries.Select(e =>
                    $"\"{e.Key}\":{{\"status\":\"{e.Value.Status}\",\"description\":{System.Text.Json.JsonSerializer.Serialize(e.Value.Description)}}}"));
                await ctx.Response.WriteAsync(
                    $"{{\"status\":\"{report.Status}\",\"service\":\"{service.Name}\",\"checks\":{{{entries}}}}}");
            }
        }).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Registers a named HttpClient for a sibling service, with credentials attached
    /// and a bounded timeout.
    /// </summary>
    public static IServiceCollection AddServiceClient(
        this IServiceCollection services,
        string name,
        Func<ServiceUrlsOptions, string> urlSelector,
        string callerName,
        params string[] serviceRoles)
    {
        services.AddTransient(sp => new ServiceAuthHandler(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<IServiceTokenProvider>(),
            new ServiceAuthOptions
            {
                CallerName = callerName,
                ServiceRoles = serviceRoles.Length > 0 ? serviceRoles : [ZapChatRoles.Admin]
            }));

        services.AddHttpClient(name, (sp, client) =>
        {
            var urls = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceUrlsOptions>>().Value;
            var baseAddress = ServiceUrlsOptions.BaseAddress(urlSelector(urls));
            if (baseAddress is not null) client.BaseAddress = baseAddress;
            // A hung dependency must not hang the caller — this was missing entirely.
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler<ServiceAuthHandler>();

        return services;
    }
}
