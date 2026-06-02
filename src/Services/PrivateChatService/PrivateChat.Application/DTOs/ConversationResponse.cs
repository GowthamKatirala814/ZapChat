namespace PrivateChat.Application.DTOs;

public class ConversationResponse
{
    public Guid MessageId { get; set; }

    public Guid SenderId { get; set; }

    public string Content { get; set; }
        = string.Empty;

    public bool IsRead { get; set; }

    public DateTime SentAt { get; set; }
}