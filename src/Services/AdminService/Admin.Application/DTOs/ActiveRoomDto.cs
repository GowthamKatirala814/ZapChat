namespace Admin.Application.DTOs;

public class ActiveRoomDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
}
