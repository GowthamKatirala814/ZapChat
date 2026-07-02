namespace Auth.Domain.Entities;

public class GeminiUsage
{
    public Guid Id { get; set; }
    
    // There should only ever be one active row per day.
    public DateTime Date { get; set; }
    
    public int RequestsToday { get; set; }
    
    public int EstimatedDailyQuota { get; set; }
    
    public double UsagePercentage { get; set; }
    
    public string? LastThresholdReached { get; set; }
    
    public bool EmailSent50 { get; set; }
    public bool EmailSent90 { get; set; }
    public bool EmailSent100 { get; set; }
    
    public bool QuotaExhausted { get; set; }
    
    // AI Health Metrics
    public int SuccessfulRequests { get; set; }
    public int BlockedMessages { get; set; }
    public int SafeMessages { get; set; }
    public int FailedRequests { get; set; }
    public int Error429s { get; set; }
    public int TimeoutErrors { get; set; }
    public int ConfigurationErrors { get; set; }
    public int AuthenticationErrors { get; set; }
    public int ServerErrors { get; set; }
    public int InvalidResponses { get; set; }
    
    public string CurrentStatus { get; set; } = "Healthy";
    
    public DateTime? LastSuccessfulModeration { get; set; }
    public DateTime? LastFailedModeration { get; set; }
    
    public string? LastErrorMessage { get; set; }
    
    // If it recovered today, the time it happened
    public DateTime? RecoveryTime { get; set; }
    
    public DateTime LastUpdated { get; set; }
}
