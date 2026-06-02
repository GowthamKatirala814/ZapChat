namespace Chat.Domain.Entities;

public class MessageReaction
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public string AnonymousName { get; set; }
        = string.Empty;

    public string Reaction { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public Message Message { get; set; }
        = null!;
}