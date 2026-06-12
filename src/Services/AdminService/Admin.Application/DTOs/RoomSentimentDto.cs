namespace Admin.Application.DTOs;

/// <summary>Keyword-based sentiment distribution for a chat room.</summary>
public class RoomSentimentDto
{
    public string RoomName { get; set; } = string.Empty;

    /// <summary>Percentage of messages with positive sentiment (0–100).</summary>
    public int Positive { get; set; }

    /// <summary>Percentage of messages with neutral sentiment (0–100).</summary>
    public int Neutral { get; set; }

    /// <summary>Percentage of messages with negative sentiment (0–100).</summary>
    public int Negative { get; set; }
}
