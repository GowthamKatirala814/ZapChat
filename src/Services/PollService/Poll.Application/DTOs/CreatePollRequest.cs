namespace Poll.Application.DTOs;

public class CreatePollRequest
{
    public string Question { get; set; }
        = string.Empty;

    public List<string> Options { get; set; }
        = new();

    public Guid CreatorId { get; set; }
}