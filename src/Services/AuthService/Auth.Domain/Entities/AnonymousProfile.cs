namespace Auth.Domain.Entities;

public class AnonymousProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string AnonymousName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}