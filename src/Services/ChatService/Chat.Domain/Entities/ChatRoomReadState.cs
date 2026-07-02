namespace Chat.Domain.Entities;

public class ChatRoomReadState
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }
    public ChatRoom ChatRoom { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int UnreadCount { get; set; } = 0;

    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
}
