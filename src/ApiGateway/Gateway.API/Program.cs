using Gateway.API.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Response Compression ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Safe for API gateway (no user-supplied content mixing)
});

// ── CORS ──────────────────────────────────────────────────────────────────────
// The gateway is the only public-facing entry point.
// AllowedOrigins is configured via appsettings.json or environment variable override.
// Supports comma-separated list for multiple origins.
var allowedOrigins = builder.Configuration["AllowedOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for HttpOnly cookie forwarding
    });
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
// Uses ASP.NET Core 7+ built-in RateLimiter. All limits are per-IP (remote address).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: strict — prevents brute-force attacks
    options.AddPolicy("login-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Register: same as login
    options.AddPolicy("register-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Refresh: more lenient — frequent silent refresh calls are expected
    options.AddPolicy("refresh-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // General API: generous — covers all other REST endpoints
    options.AddPolicy("general-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20
            }));

    // SignalR: very generous — real-time hubs generate many small requests
    // (negotiate, ping, connect, messages). This limit is per-IP across all hub paths.
    options.AddPolicy("signalr-limit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 100
            }));
});

// ── Health Checks ─────────────────────────────────────────────────────────────
// URLs are configured via appsettings.json HealthCheckUrls section.
// Override in production via environment variables:
// HealthCheckUrls__AuthService=https://zapchat-auth.onrender.com/health
var hcConfig = builder.Configuration.GetSection("HealthCheckUrls");
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri(hcConfig["AuthService"]!),         name: "auth-service",         tags: ["services"])
    .AddUrlGroup(new Uri(hcConfig["ChatService"]!),         name: "chat-service",         tags: ["services"])
    .AddUrlGroup(new Uri(hcConfig["AdminService"]!),        name: "admin-service",        tags: ["services"])
    .AddUrlGroup(new Uri(hcConfig["PrivateChatService"]!),  name: "privatechat-service",  tags: ["services"])
    .AddUrlGroup(new Uri(hcConfig["NotificationService"]!), name: "notification-service", tags: ["services"])
    .AddUrlGroup(new Uri(hcConfig["PollService"]!),         name: "poll-service",         tags: ["services"]);

// ── YARP Reverse Proxy ────────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────

// 1. Exception handler — catches unhandled exceptions, returns 500
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("""{"error":"An unexpected error occurred at the gateway."}""");
    });
});

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Response Compression
app.UseResponseCompression();

// 4. CORS — must come before routing and rate limiting
app.UseCors("GatewayPolicy");

// 5. Correlation ID — inject before logging so the logger can read it
app.UseMiddleware<CorrelationIdMiddleware>();

// 6. Security Headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// 7. Request Logging — wraps everything below to capture final status code
app.UseMiddleware<RequestLoggingMiddleware>();

// 8. Rate Limiting
app.UseRateLimiter();

// 9. Gateway-owned endpoints
app.MapHealthChecks("/health").RequireCors("GatewayPolicy");

// 10. YARP Reverse Proxy — routes everything else to downstream services
app.MapReverseProxy();

app.Run();