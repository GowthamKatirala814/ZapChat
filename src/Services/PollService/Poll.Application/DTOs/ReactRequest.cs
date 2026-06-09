namespace Poll.Application.DTOs;

public class ReactRequest
{
    public Guid PollId { get; set; }

    public Guid UserId { get; set; }

    public bool? IsUpvote { get; set; }
}
