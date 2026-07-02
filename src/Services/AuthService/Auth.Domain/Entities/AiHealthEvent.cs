namespace Auth.Domain.Entities;

public class AiHealthEvent
{
    public Guid Id { get; set; }
    
    // The day this event belongs to (for grouping/retention)
    public DateTime Date { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    
    // Detailed message (e.g. "Rate limit exceeded (HTTP 429)")
    public string Message { get; set; } = string.Empty;
}
