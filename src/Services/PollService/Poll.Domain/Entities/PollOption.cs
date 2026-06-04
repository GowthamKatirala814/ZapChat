namespace Poll.Domain.Entities;

public class PollOption
{
    public Guid Id { get; set; }

    public Guid PollId { get; set; }

    public string OptionText { get; set; }
        = string.Empty;

    public int VoteCount { get; set; }

    public Poll Poll { get; set; }
        = null!;
}