namespace Admin.Domain.Entities;

/// <summary>
/// Tracks which users are members of which rooms.
/// Used for auto-adding users to rooms and displaying member counts.
/// </summary>
public class RoomMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid RoomId { get; set; }
    
    public Guid UserId { get; set; }
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}
