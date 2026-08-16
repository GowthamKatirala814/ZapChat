using Chat.API.Hubs;
using Chat.Application.Abstractions;
using Chat.Application.Services;
using Chat.Infrastructure.Persistence;
using Chat.Infrastructure.Repositories;
using Chat.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Shared.Moderation;
using ZapChat.Shared;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

var service = new ZapChatHost.ServiceInfo("Chat", HubPaths: ["/hubs/chat"]);
builder.AddZapChatDefaults(service);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    // Keeps a dead connection from lingering longer than the presence TTL.
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddJsonProtocol(options =>
{
    // Hub payloads must be shaped identically to REST responses, or the client needs
    // two mappers for the same DTO.
    options.PayloadSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ── Options ───────────────────────────────────────────────────────────────────
builder.Services.Configure<FileUploadOptions>(
    builder.Configuration.GetSection(FileUploadOptions.SectionName));

// ── Persistence ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<ChatMongoContext>();
builder.Services.AddSingleton<IMongoIndexProvider, ChatIndexes>();

builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomMemberRepository, RoomMemberRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IModerationEventRepository, ModerationEventRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IPresenceRepository, PresenceRepository>();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IChatBroadcaster, ChatBroadcaster>();
builder.Services.AddScoped<INotificationSender, NotificationSender>();

// ── File storage ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IHostEnvironmentAccessor>(
    _ => new HostEnvironmentAccessor(builder.Environment.ContentRootPath));
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

// ── Moderation ────────────────────────────────────────────────────────────────
// One shared pipeline, replacing the near-identical copies that lived in Chat and
// PrivateChat. The classifier lives in Auth, reached with a service token.
builder.Services.AddModerationPipeline();
builder.Services.AddServiceClient(
    ModerationPipeline.AiClientName, urls => urls.AuthService, callerName: "chat-service");

builder.Services.AddServiceClient(
    ServiceClients.Notification, urls => urls.NotificationService, callerName: "chat-service");

var app = builder.Build();

app.UseZapChatDefaults(service);
app.MapHub<ChatHub>("/hubs/chat");

// Create the rooms that must always exist. Previously these lived only in SQL seed
// scripts, so a fresh install had no rooms at all.
using (var scope = app.Services.CreateScope())
{
    var rooms = scope.ServiceProvider.GetRequiredService<IRoomService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await rooms.EnsureSystemRoomsAsync();
    }
    catch (Exception ex)
    {
        // Do not stop the service: the rooms can be created by an admin, and a
        // Mongo hiccup at boot should not take chat down.
        logger.LogError(ex, "Could not ensure the system rooms exist.");
    }
}

app.Run();
