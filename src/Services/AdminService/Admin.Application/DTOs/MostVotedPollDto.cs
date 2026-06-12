namespace Admin.Application.DTOs;

public class MostVotedPollDto
{
    public Guid PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public int TotalVotes { get; set; }
    public DateTime CreatedAt { get; set; }
}
