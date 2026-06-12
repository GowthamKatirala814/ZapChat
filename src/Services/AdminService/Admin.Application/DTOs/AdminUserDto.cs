namespace Admin.Application.DTOs;

/// <summary>
/// Represents a user as visible to an admin.
/// Real FullName and Email are NEVER exposed here.
/// Admin only sees: AnonymousName, Department, Branch, CreatedAt.
/// Department/Branch/CreatedAt availability depends on Auth Service integration.
/// </summary>
public class AdminUserDto
{
    public Guid Id { get; set; }
    public string AnonymousName { get; set; } = string.Empty;

    /// <summary>
    /// Available when Auth Service exposes full user profile endpoint.
    /// Empty string otherwise.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Available when Auth Service exposes full user profile endpoint.
    /// Empty string otherwise.
    /// </summary>
    public string Branch { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
