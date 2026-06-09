namespace PrivateChat.Domain.Entities;

public class PrivateMessageReaction
{
    public Guid Id { get; set; }
    
    public Guid PrivateMessageId { get; set; }
    
    public Guid UserId { get; set; }
    
    public string SenderName { get; set; } = string.Empty;
    
    public string Reaction { get; set; } = string.Empty;
    
    public PrivateMessage PrivateMessage { get; set; } = null!;
}
