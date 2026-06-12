namespace Admin.Application.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid CreatedByAdmin { get; set; }
    public string CreatedByAdminName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}
