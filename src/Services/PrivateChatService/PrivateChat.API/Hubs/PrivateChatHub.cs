using Microsoft.AspNetCore.SignalR;

namespace PrivateChat.API.Hubs;

public class PrivateChatHub : Hub
{
    public async Task SendPrivateMessage(
        string receiverId,
        string senderName,
        string message)
    {
        await Clients.User(receiverId)
            .SendAsync(
                "ReceivePrivateMessage",
                senderName,
                message);
    }
}