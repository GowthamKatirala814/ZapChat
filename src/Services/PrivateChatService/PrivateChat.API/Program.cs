using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrivateChat.API.Hubs;
using PrivateChat.API.Providers;
using PrivateChat.Infrastructure.Persistence.DbContexts;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── ProblemDetails (RFC 7807) ─────────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ── Memory Cache (used by ContentModerationService for SHA-256 deduplication) ─
builder.Services.AddMemoryCache();

// ── Content Moderation ────────────────────────────────────────────────────────
builder.Services.AddScoped<PrivateChat.Application.Interfaces.IContentModerationService, PrivateChat.Infrastructure.Services.ContentModerationService>();

builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("AuthService", client =>
{
    var authUrl = builder.Configuration["ServiceUrls:AuthService"];
    if (!string.IsNullOrWhiteSpace(authUrl))
        client.BaseAddress = new Uri(authUrl.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient("NotificationService", client =>
{
    var notifUrl = builder.Configuration["ServiceUrls:NotificationService"];
    if (!string.IsNullOrWhiteSpace(notifUrl))
        client.BaseAddress = new Uri(notifUrl.TrimEnd('/') + "/");
});

// ── Response Compression ──────────────────────────────────────────────────────

builder.Services.AddResponseCompression(opts => opts.EnableForHttps = true);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration["AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<PrivateChatDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ── JWT Authentication ─────────────────────────────────────────────────────────
// Same settings as Auth.API — validates tokens issued by Auth Service.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };

        // SignalR sends the JWT via query string, not headers
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/privateChatHub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Custom IUserIdProvider so SignalR maps connections to user IDs
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Global Exception Handler ──────────────────────────────────────────────────
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(feature?.Error, "Unhandled exception on {Path}", feature?.Path);
        var problem = new { status = 500, title = "An unexpected error occurred.", traceId = context.TraceIdentifier };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<PrivateChatHub>("/privateChatHub");

// Lightweight health endpoint for gateway health checks
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "PrivateChat" }));

app.Run();