using Auth.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "ZapChat";

    /// <summary>Supplied by user-secrets or ZAPCHAT_EMAIL__APPPASSWORD. Never committed.</summary>
    public string AppPassword { get; set; } = string.Empty;

    /// <summary>
    /// When true, codes are written to the log instead of emailed. Lets the whole
    /// registration and reset flow be exercised locally with no SMTP credentials.
    /// </summary>
    public bool UseLogTransport { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SenderEmail) && !string.IsNullOrWhiteSpace(AppPassword);
}

public sealed class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string otpCode, string anonymousName) =>
        SendPlainAsync(toEmail, anonymousName, "Your ZapChat password reset code",
            $"""
             Hi {anonymousName},

             Your password reset code is: {otpCode}

             It expires in 10 minutes. If you did not request this, you can ignore this email.

             — ZapChat
             """);

    public Task SendRegistrationOtpAsync(string toEmail, string otpCode, string fullName) =>
        SendPlainAsync(toEmail, fullName, "Verify your ZapChat account",
            $"""
             Hi {fullName},

             Your email verification code is: {otpCode}

             It expires in 10 minutes. If you did not sign up for ZapChat, ignore this email.

             — ZapChat
             """);

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var message = Build(toEmail, "ZapChat user", subject);
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();
        await DeliverAsync(message, subject, htmlBody);
    }

    private async Task SendPlainAsync(string toEmail, string displayName, string subject, string body)
    {
        var message = Build(toEmail, displayName, subject);
        message.Body = new TextPart("plain") { Text = body };
        await DeliverAsync(message, subject, body);
    }

    private MimeMessage Build(string toEmail, string displayName, string subject)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        message.To.Add(new MailboxAddress(displayName, toEmail));
        message.Subject = subject;
        return message;
    }

    private async Task DeliverAsync(MimeMessage message, string subject, string body)
    {
        var recipient = message.To.ToString();

        // Development escape hatch: no SMTP credentials needed to test the flows.
        if (_options.UseLogTransport || !_options.IsConfigured)
        {
            if (!_options.UseLogTransport)
            {
                _logger.LogWarning(
                    "Email:SenderEmail/AppPassword are not configured — falling back to the log transport. " +
                    "Set Email:UseLogTransport=true to make this explicit.");
            }

            _logger.LogInformation(
                "[EMAIL:LOG-TRANSPORT] To={Recipient} Subject={Subject}\n{Body}",
                recipient, subject, body);
            return;
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_options.SenderEmail, _options.AppPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Sent '{Subject}' to {Recipient}.", subject, recipient);
    }
}
