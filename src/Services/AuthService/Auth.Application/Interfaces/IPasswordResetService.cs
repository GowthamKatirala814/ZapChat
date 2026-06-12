namespace Auth.Application.Interfaces;

public interface IPasswordResetService
{
    Task<bool> SendOtpAsync(string email);

    Task<string?> VerifyOtpAsync(string email, string otpCode);

    Task<bool> ResetPasswordAsync(string resetToken, string newPassword);
}
