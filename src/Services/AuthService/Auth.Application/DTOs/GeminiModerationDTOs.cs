namespace Auth.Application.DTOs;

public class GeminiModerationRequest
{
    public string Content { get; set; } = string.Empty;
}

public class GeminiModerationResponse
{
    public bool AllowMessage { get; set; }
    public string Category { get; set; } = "OTHER";
    public string Explanation { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
