using Admin.Application.Interfaces;
using Admin.Infrastructure.Configuration;
using Admin.Infrastructure.Persistence.DbContexts;
using Admin.Infrastructure.Repositories;
using Admin.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers & API Explorer ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ─── Swagger with Bearer Auth ────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Admin.API — ZapPulse Admin Service",
        Version = "v1",
        Description = "Admin Service for ZapPulse: Dashboard, User Management, Moderation, " +
                      "Room Management, Analytics, and Audit Logs."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Admin role required for all endpoints."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// ─── Strongly Typed Configuration ───────────────────────────────────────────
// ServiceUrls are bound from appsettings.json "ServiceUrls" section.
// Change the URL in appsettings.json — no code changes required.
builder.Services.Configure<ServiceUrlsOptions>(
    builder.Configuration.GetSection(ServiceUrlsOptions.SectionName));

// ─── ServiceUrls Validation ─────────────────────────────────────────────────
// Fail fast if required service URLs are not configured.
var chatServiceUrl = builder.Configuration["ServiceUrls:ChatService"];
var privateChatServiceUrl = builder.Configuration["ServiceUrls:PrivateChatService"];
var pollServiceUrl = builder.Configuration["ServiceUrls:PollService"];
var notificationServiceUrl = builder.Configuration["ServiceUrls:NotificationService"];

if (string.IsNullOrEmpty(chatServiceUrl))
{
    throw new InvalidOperationException(
        "ServiceUrls:ChatService is not configured in appsettings.json. " +
        "This is required for moderation auto-removal functionality.");
}

if (string.IsNullOrEmpty(privateChatServiceUrl))
{
    throw new InvalidOperationException(
        "ServiceUrls:PrivateChatService is not configured in appsettings.json. " +
        "This is required for moderation auto-removal functionality.");
}

if (string.IsNullOrEmpty(pollServiceUrl))
{
    throw new InvalidOperationException(
        "ServiceUrls:PollService is not configured in appsettings.json. " +
        "This is required for dashboard statistics functionality.");
}

if (string.IsNullOrEmpty(notificationServiceUrl))
{
    throw new InvalidOperationException(
        "ServiceUrls:NotificationService is not configured in appsettings.json. " +
        "This is required for dashboard statistics functionality.");
}

// ─── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── HTTP Clients (IHttpClientFactory) ───────────────────────────────────────
// Named client for Auth Service. BaseAddress resolved at runtime from IOptions<ServiceUrlsOptions>.
// Never hardcoded — change the URL in appsettings.json.
builder.Services.AddHttpClient("AuthService", (serviceProvider, client) =>
{
    var opts = serviceProvider.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.AuthService))
        client.BaseAddress = new Uri(opts.AuthService.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient("ChatService", (serviceProvider, client) =>
{
    var opts = serviceProvider.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.ChatService))
        client.BaseAddress = new Uri(opts.ChatService.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient("PrivateChatService", (serviceProvider, client) =>
{
    var opts = serviceProvider.GetRequiredService<IOptions<ServiceUrlsOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.PrivateChatService))
        client.BaseAddress = new Uri(opts.PrivateChatService.TrimEnd('/') + "/");
});

// ─── Repository Registrations ─────────────────────────────────────────────────
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IBlockedUserRepository, BlockedUserRepository>();
builder.Services.AddScoped<IModerationSettingsRepository, ModerationSettingsRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IRoomManagementRepository, RoomManagementRepository>();
builder.Services.AddScoped<IRoomMembershipRepository, RoomMembershipRepository>();

// ─── Service Registrations ────────────────────────────────────────────────────
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

builder.Services.AddHostedService<Admin.API.Services.ModerationBackgroundService>();

// ─── JWT Authentication ───────────────────────────────────────────────────────
// Admin Service reads JWT settings independently — Auth Service is NOT modified.
// The same Secret/Issuer/Audience values validate tokens issued by Auth Service.
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
    });

// ─── Authorization ────────────────────────────────────────────────────────────
// Admin Policy: requires the "Admin" role claim in the JWT.
// All controllers use [Authorize(Roles = "Admin")].
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin.API v1");
        c.RoutePrefix = "swagger";
    });
}

// CORS must be before HTTPS redirection to ensure headers are preserved
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
