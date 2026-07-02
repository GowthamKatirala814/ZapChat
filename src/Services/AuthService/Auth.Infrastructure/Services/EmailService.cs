using Auth.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Auth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Sends a password-reset OTP. Existing forgot-password flow — not changed.</summary>
    public async Task SendOtpEmailAsync(string toEmail, string otpCode, string anonymousName)
    {
        var message = BuildMessage(toEmail, anonymousName, "Your ZapChat Password Reset Code");

        message.Body = new TextPart("plain")
        {
            Text = $"""
                Hi {anonymousName},

                Your verification code is: {otpCode}

                This code expires in 10 minutes.

                If you did not request a password reset, you can safely ignore this email.

                — The ZapChat Team
                """
        };

        await SendAsync(message);
    }

    /// <summary>Sends an account-verification OTP during the new multi-step registration flow.</summary>
    public async Task SendRegistrationOtpEmailAsync(string toEmail, string otpCode, string fullName)
    {
        var message = BuildMessage(toEmail, fullName, "Verify your ZapChat account");

        message.Body = new TextPart("plain")
        {
            Text = $"""
                Welcome to ZapChat!

                Hi {fullName},

                Your email verification code is: {otpCode}

                This code expires in 10 minutes.

                If you did not create a ZapChat account, ignore this email.

                — The ZapChat Team
                """
        };

        await SendAsync(message);
    }

    /// <summary>Sends a generic HTML email (e.g. for admin alerts).</summary>
    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = BuildMessage(toEmail, "Administrator", subject);

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        await SendAsync(message);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private MimeMessage BuildMessage(string toEmail, string displayName, string subject)
    {
        var smtpHost    = _configuration["EmailSettings:SmtpHost"]    ?? "smtp.gmail.com";
        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
        var senderName  = _configuration["EmailSettings:SenderName"]  ?? "ZapChat";

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(senderName, senderEmail));
        msg.To.Add(new MailboxAddress(displayName, toEmail));
        msg.Subject = subject;
        return msg;
    }

    private async Task SendAsync(MimeMessage message)
    {
        var smtpHost   = _configuration["EmailSettings:SmtpHost"]   ?? "smtp.gmail.com";
        var smtpPort   = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
        var appPassword = _configuration["EmailSettings:AppPassword"]  ?? "";

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(senderEmail, appPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
