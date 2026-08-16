using System.Threading.RateLimiting;
using Gateway.API.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "ZAPCHAT_");

// ── CORS ──────────────────────────────────────────────────────────────────────
// The gateway is the only public entry point, so it owns CORS. Origins come from
// configuration rather than a hardcoded literal.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddPolicy("GatewayPolicy", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

// ── Rate limiting ─────────────────────────────────────────────────────────────
// Partitioned per remote IP. The important change from before is that the strict
// policy now covers OTP verification and password reset, not just /login and the
// exact /register path — previously those fell through to the 300/min general
// bucket, which made brute-forcing a 6-digit code practical.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static Func<HttpContext, RateLimitPartition<string>> PerIp(
        int permitLimit, int windowSeconds, int queueLimit) =>
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = queueLimit
            });

    options.AddPolicy("login", PerIp(5, 60, 0));
    options.AddPolicy("register", PerIp(5, 60, 0));
    options.AddPolicy("refresh", PerIp(20, 60, 5));
    options.AddPolicy("report", PerIp(20, 60, 0));
    options.AddPolicy("upload", PerIp(30, 60, 5));
    options.AddPolicy("general", PerIp(300, 60, 20));
    options.AddPolicy("realtime", PerIp(1000, 60, 100));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"code":"rate_limited","message":"Too many requests. Please slow down."}""", ct);
    };
});

// ── Health checks ─────────────────────────────────────────────────────────────
// Probes each service's readiness endpoint, which in turn pings MongoDB — so a
// healthy gateway now means the databases are actually reachable.
var healthChecks = builder.Services.AddHealthChecks();

foreach (var service in builder.Configuration.GetSection("HealthChecks:Services").GetChildren())
{
    if (!string.IsNullOrWhiteSpace(service.Value))
    {
        healthChecks.AddUrlGroup(
            new Uri(service.Value), name: service.Key, tags: ["downstream"]);
    }
}

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────────────

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status502BadGateway;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(
        """{"code":"gateway_error","message":"The request could not be routed."}""");
}));

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("GatewayPolicy");

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRateLimiter();

// Liveness: the gateway process itself.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Gateway" }))
    .RequireCors("GatewayPolicy");

// Readiness: every downstream service and its database.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("downstream"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var entries = string.Join(",", report.Entries.Select(e =>
            $"\"{e.Key}\":\"{e.Value.Status}\""));

        await context.Response.WriteAsync(
            $"{{\"status\":\"{report.Status}\",\"services\":{{{entries}}}}}");
    }
}).RequireCors("GatewayPolicy");

app.MapReverseProxy();

app.Run();
