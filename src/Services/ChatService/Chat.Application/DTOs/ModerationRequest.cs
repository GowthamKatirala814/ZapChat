namespace Chat.Application.DTOs;

/// <summary>
/// Carries the information needed to evaluate a message for moderation.
/// </summary>
public class ModerationRequest
{
    /// <summary>The raw message text to evaluate.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>The anonymous display name of the sender (used for audit logging).</summary>
    public string AnonymousName { get; set; } = string.Empty;

    /// <summary>The room name the message was sent in.</summary>
    public string RoomName { get; set; } = string.Empty;

    /// <summary>The room's primary key (used for the audit log FK).</summary>
    public Guid RoomId { get; set; }

    /// <summary>The authenticated user ID (nullable; used for audit logging).</summary>
    public string? UserId { get; set; }
}

