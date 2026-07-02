using Chat.API.Hubs;
using Chat.API.Services;
using Chat.Application.Interfaces;
using Chat.Infrastructure.Persistence.DbContexts;
using Chat.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Response Compression ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression(opts => opts.EnableForHttps = true);

// ── ProblemDetails ────────────────────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ── Memory Cache (used by ContentModerationService for SHA-256 deduplication) ─
builder.Services.AddMemoryCache();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

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
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ── HTTP Clients ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<INotificationService, NotificationService>(client =>
{
    var notifUrl = builder.Configuration["ServiceUrls:NotificationService"];
    if (!string.IsNullOrWhiteSpace(notifUrl))
        client.BaseAddress = new Uri(notifUrl.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient("AuthService", client =>
{
    var authUrl = builder.Configuration["ServiceUrls:AuthService"];
    if (!string.IsNullOrWhiteSpace(authUrl))
        client.BaseAddress = new Uri(authUrl.TrimEnd('/') + "/");
});

// Named client for Admin Service (used in MessagesController for reports)
builder.Services.AddHttpClient("AdminService", client =>
{
    var adminUrl = builder.Configuration["ServiceUrls:AdminService"];
    if (!string.IsNullOrWhiteSpace(adminUrl))
        client.BaseAddress = new Uri(adminUrl.TrimEnd('/') + "/");
});

// ── Gemini AI Named Client (used by ContentModerationService) ─────────────────
builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("GeminiSettings:TimeoutSeconds", 10));
});

// ── Content Moderation Service ────────────────────────────────────────────────
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();

// ── JWT Authentication ─────────────────────────────────────────────────────────
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<PresenceTracker>();

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
app.MapHub<ChatHub>("/chatHub");

// Lightweight health endpoint for gateway health checks
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Chat" }));

app.Run();
