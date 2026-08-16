using Poll.API;
using Poll.Application;
using Poll.Infrastructure.Persistence;
using ZapChat.Shared;
using ZapChat.Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);

var service = new ZapChatHost.ServiceInfo("Poll", HubPaths: ["/hubs/polls"]);
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

builder.Services.AddScoped<PollMongoContext>();
builder.Services.AddSingleton<IMongoIndexProvider, PollIndexes>();

builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IPollVoteRepository, PollVoteRepository>();
builder.Services.AddScoped<IPollReactionRepository, PollReactionRepository>();

builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IPollBroadcaster, PollBroadcaster>();

var app = builder.Build();

app.UseZapChatDefaults(service);
app.MapHub<PollHub>("/hubs/polls");

app.Run();
