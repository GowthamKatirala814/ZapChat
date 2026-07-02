namespace Auth.Application.Interfaces;

public interface IEmailService
{
    /// <summary>Sends a password-reset OTP to an existing user (forgot password flow).</summary>
    Task SendOtpEmailAsync(string toEmail, string otpCode, string anonymousName);

    /// <summary>Sends an account verification OTP during the new multi-step registration flow.</summary>
    Task SendRegistrationOtpEmailAsync(string toEmail, string otpCode, string fullName);

    /// <summary>Sends a generic HTML email (e.g. for admin alerts).</summary>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
