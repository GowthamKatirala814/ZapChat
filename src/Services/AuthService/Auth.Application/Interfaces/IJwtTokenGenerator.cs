namespace Auth.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(
        Guid userId,
        string email,
        string anonymousName,
        List<string> roles);
}