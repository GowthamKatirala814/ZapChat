using PrivateChat.API;
using PrivateChat.Application;
using PrivateChat.Infrastructure.Persistence;
using PrivateChat.Infrastructure.Repositories;
using Shared.Moderation;
using ZapChat.Shared;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

var service = new ZapChatHost.ServiceInfo("PrivateChat", HubPaths: ["/hubs/private-chat"]);
builder.AddZapChatDefaults(service);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ── Persistence ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<PrivateChatMongoContext>();
builder.Services.AddSingleton<IMongoIndexProvider, PrivateChatIndexes>();

builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IDirectMessageRepository, DirectMessageRepository>();
builder.Services.AddScoped<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddScoped<IModerationEventRepository, ModerationEventRepository>();

// ── Application ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IPrivateChatBroadcaster, PrivateChatBroadcaster>();
builder.Services.AddScoped<INotificationSender, NotificationSender>();
builder.Services.AddScoped<IUserDirectory, UserDirectory>();

// ── Moderation (the shared pipeline, not a second copy) ───────────────────────
builder.Services.AddModerationPipeline();
builder.Services.AddServiceClient(
    ModerationPipeline.AiClientName, urls => urls.AuthService,
    callerName: "privatechat-service");

builder.Services.AddServiceClient(
    ServiceClients.Auth, urls => urls.AuthService, callerName: "privatechat-service");

builder.Services.AddServiceClient(
    ServiceClients.Notification, urls => urls.NotificationService,
    callerName: "privatechat-service");

var app = builder.Build();

app.UseZapChatDefaults(service);
app.MapHub<PrivateChatHub>("/hubs/private-chat");

app.Run();
