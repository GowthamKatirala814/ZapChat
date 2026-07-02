using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrivateChat.Infrastructure.Persistence.DbContexts;
using System;
using System.Linq;
using System.Threading.Tasks;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddDbContext<PrivateChatDbContext>(options =>
            options.UseSqlServer("Server=localhost;Database=ZapChat_PrivateChatDb;Trusted_Connection=True;TrustServerCertificate=True;"));
    });

var host = builder.Build();

using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<PrivateChatDbContext>();

var conversations = await context.Conversations.Include(c => c.Messages).ToListAsync();

foreach (var conv in conversations)
{
    var messages = conv.Messages.OrderBy(m => m.SentAt).ToList();
    var lastMsg = messages.LastOrDefault();
    if (lastMsg != null)
    {
        conv.LastMessageAt = lastMsg.SentAt;
        conv.LastMessagePreview = lastMsg.Content;
    }
    
    conv.User1UnreadCount = messages.Count(m => m.SenderId != conv.User1Id && !m.IsRead);
    conv.User2UnreadCount = messages.Count(m => m.SenderId != conv.User2Id && !m.IsRead);
}

await context.SaveChangesAsync();
Console.WriteLine("Backfill completed.");
