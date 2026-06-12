namespace Admin.Domain.Entities;

/// <summary>
/// Admin-owned mirror of a chat room.
/// Admin Service manages rooms independently; no direct modification of ChatService.
/// Integration contract: ChatService can later sync room data via Admin's room API.
/// </summary>
public class RoomManagement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete — room is not visible to users but record is preserved for audit
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Admin ID who created this room
    /// </summary>
    public Guid CreatedByAdmin { get; set; }
}
