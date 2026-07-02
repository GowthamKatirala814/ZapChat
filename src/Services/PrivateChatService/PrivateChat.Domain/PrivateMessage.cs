namespace PrivateChat.Domain.Entities;

public class PrivateMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid SenderId { get; set; }

    /// <summary>Anonymous display name at time of send — never a real name.</summary>
    public string SenderName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Guid? ParentMessageId { get; set; }

    public PrivateMessage? ParentMessage { get; set; }

    public ICollection<PrivateMessage> Replies { get; set; }
        = new List<PrivateMessage>();

    public ICollection<PrivateMessageReaction> Reactions { get; set; }
        = new List<PrivateMessageReaction>();

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