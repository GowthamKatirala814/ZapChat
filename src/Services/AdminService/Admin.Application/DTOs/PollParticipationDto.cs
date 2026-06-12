namespace Admin.Application.DTOs;

/// <summary>Poll participation data for the analytics chart.</summary>
public class PollParticipationDto
{
    public string PollQuestion { get; set; } = string.Empty;
    public int TotalVotes { get; set; }

    /// <summary>Participation rate as a percentage (0–100). May be 0 if total user count is unavailable.</summary>
    public int ParticipationRate { get; set; }
}
