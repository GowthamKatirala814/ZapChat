namespace Admin.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DeletedUsers { get; set; }
    public int BlockedUsers { get; set; }
    public int TotalChatRooms { get; set; }
    public int TotalPrivateConversations { get; set; }
    public int TotalMessages { get; set; }
    public int TotalPolls { get; set; }
    public int TotalNotifications { get; set; }
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
}
