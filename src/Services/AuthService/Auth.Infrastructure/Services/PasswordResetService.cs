using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly AuthDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordResetService(
        AuthDbContext context,
        IEmailService emailService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> SendOtpAsync(string email)
    {
        // Find user — if not found, return true anyway (do not reveal whether email exists)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        if (user is null)
            return true;

        // Get anonymous name for the email greeting
        var anonymousName = await _context.AnonymousProfiles
            .Where(a => a.UserId == user.Id)
            .Select(a => a.AnonymousName)
            .FirstOrDefaultAsync() ?? "User";

        // Delete any existing unused OTPs for this user (cleanup)
        var existingOtps = await _context.PasswordResetOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed)
            .ToListAsync();

        if (existingOtps.Any())
        {
            _context.PasswordResetOtps.RemoveRange(existingOtps);
        }

        // Generate 6-digit OTP
        var otpCode = Random.Shared.Next(100000, 999999).ToString();

        var otp = new PasswordResetOtp
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Email     = email,
            OtpCode   = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed    = false
        };

        _context.PasswordResetOtps.Add(otp);
        await _context.SaveChangesAsync();

        // Send email — do not crash if SMTP fails; surface the error up
        await _emailService.SendOtpEmailAsync(email, otpCode, anonymousName);

        return true;
    }

    public async Task<string?> VerifyOtpAsync(string email, string otpCode)
    {
        // Find the user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        if (user is null)
            return null;

        // Find a valid, unused, unexpired OTP with this code
        var otp = await _context.PasswordResetOtps
            .FirstOrDefaultAsync(o =>
                o.UserId  == user.Id     &&
                o.OtpCode == otpCode     &&
                !o.IsUsed                &&
                o.ExpiresAt > DateTime.UtcNow);

        if (otp is null)
            return null;

        // Generate a one-time reset token
        var resetToken = Guid.NewGuid().ToString("N");

        otp.ResetToken = resetToken;
        otp.IsUsed     = true; // Mark used — kept for audit, token still checked separately

        await _context.SaveChangesAsync();

        return resetToken;
    }

    public async Task<bool> ResetPasswordAsync(string resetToken, string newPassword)
    {
        // Find the OTP record with this reset token
        // Give 30-minute window after OTP verification to complete the reset
        var cutoff = DateTime.UtcNow.AddMinutes(-30);

        var otp = await _context.PasswordResetOtps
            .FirstOrDefaultAsync(o =>
                o.ResetToken == resetToken &&
                o.IsUsed                   &&
                o.ExpiresAt > cutoff);

        if (otp is null)
            return false;

        // Find the user
        var user = await _context.Users.FindAsync(otp.UserId);
        if (user is null)
            return false;

        // Hash the new password using the same hasher as Register
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);

        // Invalidate the reset token so it cannot be reused
        otp.ResetToken = null;

        await _context.SaveChangesAsync();

        return true;
    }
}
