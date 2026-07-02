namespace Chat.Domain.Entities;

public class ChatRoom
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAt { get; set; }

    public string? LastMessagePreview { get; set; }

    public ICollection<Message> Messages { get; set; }
        = new List<Message>();
}