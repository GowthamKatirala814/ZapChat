using Microsoft.AspNetCore.SignalR;
using PrivateChat.Domain.Entities;
using PrivateChat.Infrastructure.Persistence.DbContexts;

namespace PrivateChat.API.Hubs;

public class PrivateChatHub : Hub
{
    private readonly PrivateChatDbContext _context;

    public PrivateChatHub(
        PrivateChatDbContext context)
    {
        _context = context;
    }

    public async Task SendPrivateMessage(
    Guid conversationId,
    Guid senderId,
    string receiverId,
    string senderName,
    string message)
    {
        try
        {
            Console.WriteLine("==========");
            Console.WriteLine("STEP 1");

            Console.WriteLine(
                $"ConversationId: {conversationId}");

            Console.WriteLine(
                $"SenderId: {senderId}");

            Console.WriteLine(
                $"ReceiverId: {receiverId}");

            Console.WriteLine(
                $"SenderName: {senderName}");

            Console.WriteLine(
                $"Message: {message}");

            var privateMessage =
                new PrivateMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = message,
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                };

            Console.WriteLine("STEP 2");

            _context.Messages.Add(
                privateMessage);

            Console.WriteLine("STEP 3");

            await _context.SaveChangesAsync();

            Console.WriteLine("STEP 4");

            await Clients.User(receiverId)
                .SendAsync(
                    "ReceivePrivateMessage",
                    senderName,
                    message,
                    privateMessage.SentAt);

            Console.WriteLine("STEP 5");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex.ToString());

            throw new HubException(
                ex.ToString());
        }
    }

}