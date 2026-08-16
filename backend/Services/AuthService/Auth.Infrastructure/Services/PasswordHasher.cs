using Auth.Application.Abstractions;

namespace Auth.Infrastructure.Services;

/// <summary>
/// BCrypt with a work factor of 12. Hashes produced by the previous implementation
/// (default work factor 11) verify unchanged, so migrated accounts keep their
/// passwords.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash)) return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must fail closed, not throw a 500.
            return false;
        }
    }
}
