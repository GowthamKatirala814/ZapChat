namespace Poll.Domain.Entities;

public class Poll
{
    public Guid Id { get; set; }

    public string Question { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public ICollection<PollOption> Options
        = new List<PollOption>();
}