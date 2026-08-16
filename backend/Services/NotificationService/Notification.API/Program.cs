using Notification.API;
using Notification.Application;
using Notification.Infrastructure;
using ZapChat.Shared;
using ZapChat.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

var service = new ZapChatHost.ServiceInfo("Notification", HubPaths: ["/hubs/notifications"]);
builder.AddZapChatDefaults(service);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.Configure<WebPushOptions>(
    builder.Configuration.GetSection(WebPushOptions.SectionName));

builder.Services.AddScoped<NotificationMongoContext>();
builder.Services.AddSingleton<IMongoIndexProvider, NotificationIndexes>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationBroadcaster, NotificationBroadcaster>();

// Push is only wired up when VAPID keys are present; otherwise a no-op implementation
// is registered so nothing pretends to deliver. The old code shipped a placeholder
// private key, so every send threw and the exception was swallowed.
var webPush = builder.Configuration.GetSection(WebPushOptions.SectionName).Get<WebPushOptions>();

if (webPush?.IsConfigured == true)
    builder.Services.AddScoped<IPushDispatcher, WebPushDispatcher>();
else
    builder.Services.AddSingleton<IPushDispatcher, DisabledPushDispatcher>();

var app = builder.Build();

app.UseZapChatDefaults(service);
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
