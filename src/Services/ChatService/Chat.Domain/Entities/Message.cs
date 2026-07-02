namespace Chat.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public string AnonymousName { get; set; }
        = string.Empty;

    public string Content { get; set; }
        = string.Empty;

    public DateTime SentAt { get; set; }
        = DateTime.UtcNow;

    public ChatRoom ChatRoom { get; set; }
        = null!;
    public ICollection<MessageReaction> Reactions
    { get; set; }
    = new List<MessageReaction>();
    public Guid? ParentMessageId { get; set; }

    public Message? ParentMessage { get; set; }

    public ICollection<Message> Replies { get; set; }
        = new List<Message>();
    public string? AttachmentUrl { get; set; }

    public string? AttachmentType { get; set; }

    public string? FileName { get; set; }

    public bool IsRemoved { get; set; } = false;

    public DateTime? RemovedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public bool IsEdited { get; set; } = false;

    public DateTime? EditedAt { get; set; }

}