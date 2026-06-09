namespace Poll.Application.DTOs;

public class VoteRequest
{
    public Guid PollId { get; set; }

    public Guid? OptionId { get; set; }

    public Guid UserId { get; set; }
}