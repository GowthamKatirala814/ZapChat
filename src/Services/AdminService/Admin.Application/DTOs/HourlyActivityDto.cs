namespace Admin.Application.DTOs;

/// <summary>Message count for a specific hour of the day (0–23).</summary>
public class HourlyActivityDto
{
    public int Hour { get; set; }
    public int MessageCount { get; set; }
}
