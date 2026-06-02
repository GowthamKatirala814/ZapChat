namespace PrivateChat.Application.DTOs;

public class SendMessageRequest
{
    public Guid ConversationId { get; set; }

    public Guid SenderId { get; set; }

    public string Content { get; set; }
        = string.Empty;
}