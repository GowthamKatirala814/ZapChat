using Auth.API.Infrastructure;
using Auth.Application.Abstractions;
using Auth.Infrastructure.Email;
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

// ── Email ─────────────────────────────────────────────────────────────────────
// Bound and validated at startup with ValidateOnStart, so a host that cannot send mail
// refuses to start rather than accepting registrations and dropping every message.
builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<EmailOptions>>(
    new EmailOptionsValidator(builder.Environment.IsProduction()));

builder.Services.AddSingleton<OtpResendCooldown>();
builder.Services.AddSingleton<IMicrosoftTokenProvider, MicrosoftTokenProvider>();

builder.Services.AddHttpClient("entra-token").ConfigureHttpClient(
    client => client.Timeout = TimeSpan.FromSeconds(20));

builder.Services.AddHttpClient("graph-mail").ConfigureHttpClient(
    client => client.Timeout = TimeSpan.FromSeconds(30));

// One sender, chosen from configuration. There is no fallback: an unreachable Graph or
// SMTP provider produces an error, never a silent switch to the log.
builder.Services.AddSingleton<IEmailSender>(provider =>
{
    var options = provider.GetRequiredService<IOptions<EmailOptions>>();

    return options.Value.Provider switch
    {
        EmailProvider.Graph => ActivatorUtilities.CreateInstance<GraphEmailSender>(provider),
        EmailProvider.Smtp => ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider),
        EmailProvider.Log => ActivatorUtilities.CreateInstance<LogEmailSender>(provider),
        _ => throw new InvalidOperationException(
            "Email:Provider is not configured. Startup validation should have caught this."),
    };
});

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
