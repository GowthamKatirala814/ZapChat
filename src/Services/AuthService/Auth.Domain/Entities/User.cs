using System.Data;

namespace Auth.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Role> Roles { get; set; } = new List<Role>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}