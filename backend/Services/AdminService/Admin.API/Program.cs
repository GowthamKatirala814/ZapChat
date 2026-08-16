using Admin.Application;
using Admin.Application.Services;
using Admin.Infrastructure.Persistence;
using Admin.Infrastructure.Services;
using ZapChat.Shared;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

var service = new ZapChatHost.ServiceInfo("Admin", HubPaths: []);
builder.AddZapChatDefaults(service);

// ── Persistence ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<AdminMongoContext>();
builder.Services.AddSingleton<IMongoIndexProvider, AdminIndexes>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IBlockedUserRepository, BlockedUserRepository>();
builder.Services.AddScoped<IModerationSettingsRepository, ModerationSettingsRepository>();

// ── Application ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAutoModerationService, AutoModerationService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ── Cross-service gateway ─────────────────────────────────────────────────────
// Every client carries a service token, which is why these calls now succeed. The old
// code registered an auth-forwarding handler on the Auth client only, so the Chat,
// PrivateChat, Poll and Notification calls all 401'd and were swallowed.
builder.Services.AddScoped<IPlatformGateway, PlatformGateway>();

builder.Services.AddServiceClient(
    ServiceClients.Auth, urls => urls.AuthService, callerName: "admin-service");
builder.Services.AddServiceClient(
    ServiceClients.Chat, urls => urls.ChatService, callerName: "admin-service");
builder.Services.AddServiceClient(
    ServiceClients.PrivateChat, urls => urls.PrivateChatService, callerName: "admin-service");
builder.Services.AddServiceClient(
    ServiceClients.Poll, urls => urls.PollService, callerName: "admin-service");
builder.Services.AddServiceClient(
    ServiceClients.Notification, urls => urls.NotificationService, callerName: "admin-service");

// ── Automated moderation ──────────────────────────────────────────────────────
builder.Services.AddHostedService<AutoModerationWorker>();

var app = builder.Build();

app.UseZapChatDefaults(service);

app.Run();

/// <summary>
/// Runs the single automated moderation rule on a schedule.
///
/// The rule itself lives in <see cref="IAutoModerationService"/> and is also reachable
/// on demand, so the behaviour is identical whether it runs on a timer or is triggered
/// by an admin. The old design had the timer version and a second, divergent copy inside
/// the report controller.
/// </summary>
internal sealed class AutoModerationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<AutoModerationWorker> _logger;

    public AutoModerationWorker(IServiceProvider services, ILogger<AutoModerationWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the host finish starting before the first sweep.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = _services.CreateScope();
                var moderation = scope.ServiceProvider.GetRequiredService<IAutoModerationService>();

                var actioned = await moderation.RunAsync(stoppingToken);

                if (actioned > 0)
                {
                    _logger.LogInformation(
                        "Automatic moderation actioned {Count} author(s).", actioned);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Logged and retried on the next tick — one bad sweep must not kill the worker.
                _logger.LogError(ex, "The automatic moderation sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
