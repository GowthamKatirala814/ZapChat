using Microsoft.EntityFrameworkCore;
using PrivateChat.Infrastructure.Persistence.DbContexts;
using PrivateChat.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddDbContext<PrivateChatDbContext>(
    options =>
    {
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection"));
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(
    "AllowFrontend"
);

app.UseAuthorization();

app.MapControllers();

app.MapHub<PrivateChatHub>(
    "/privateChatHub");

app.Run();