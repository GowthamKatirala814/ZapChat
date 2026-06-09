namespace Poll.Domain.Entities;

public class PollReaction
{
    public Guid Id { get; set; }

    public Guid PollId { get; set; }

    public Guid UserId { get; set; }

    public bool IsUpvote { get; set; }

    public DateTime ReactedAt { get; set; }
        = DateTime.UtcNow;
}
