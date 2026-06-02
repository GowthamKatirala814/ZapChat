namespace Auth.Application.DTOs;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;
}
