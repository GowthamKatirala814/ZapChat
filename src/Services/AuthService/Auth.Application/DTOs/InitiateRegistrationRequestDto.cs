using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class InitiateRegistrationRequestDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string Branch { get; set; } = string.Empty;
}

public class InitiateRegistrationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
