namespace PrivateChat.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    public Guid User1Id { get; set; }

    public Guid User2Id { get; set; }

    /// <summary>
    /// Denormalized timestamp of the last message in this conversation.
    /// Updated atomically with every message save.
    /// Used for ordering the conversation list without a MAX(Messages.SentAt) join.
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    public string? LastMessagePreview { get; set; }

    public int User1UnreadCount { get; set; } = 0;

    public int User2UnreadCount { get; set; } = 0;

    public ICollection<PrivateMessage> Messages { get; set; } = new List<PrivateMessage>();
}