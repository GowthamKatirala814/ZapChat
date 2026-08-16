using Auth.API.Infrastructure;
using Auth.Application.Abstractions;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;
using Microsoft.Extensions.Options;
using ZapChat.Shared;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

// CORS, compression, Swagger, JSON, Mongo, JWT (+cookie), deny-by-default
// authorization, index bootstrap, health checks.
builder.AddZapChatDefaults(new ZapChatHost.ServiceInfo("Auth", HubPaths: []));

// ── Options ───────────────────────────────────────────────────────────────────
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.Configure<CookieOptionsConfig>(
    builder.Configuration.GetSection(CookieOptionsConfig.SectionName));

// ── Persistence ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthMongoContext>();
builder.Services.AddSingleton<IMongoIndexProvider, AuthIndexes>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IAiUsageRepository, AiUsageRepository>();

// ── Domain services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAnonymousNameService, AnonymousNameService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IAiModerationService, AiModerationService>();
builder.Services.AddSingleton<AuthCookieWriter>();

// ── Outbound HTTP ─────────────────────────────────────────────────────────────
// Chat owns room membership, so registration calls Chat to seed default rooms.
builder.Services.AddServiceClient(
    ServiceClients.Chat, urls => urls.ChatService, callerName: "auth-service");

builder.Services.AddHttpClient("gemini", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var app = builder.Build();

app.UseZapChatDefaults(new ZapChatHost.ServiceInfo("Auth", HubPaths: []));

app.Run();
