namespace Admin.Application.DTOs;

/// <summary>Health index for a chat room based on report-to-message ratio.</summary>
public class RoomHealthDto
{
    public string RoomName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int ReportCount { get; set; }
    public double ReportRate { get; set; }

    /// <summary>"Healthy" / "Monitor" / "Critical"</summary>
    public string Health { get; set; } = string.Empty;
}
