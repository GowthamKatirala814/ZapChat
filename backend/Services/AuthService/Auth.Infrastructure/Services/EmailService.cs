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

    /// <summary>
    /// Include the one-time code in the API response instead of only in the log.
    ///
    /// NOT bindable from configuration by design — Program.cs sets it, and only when
    /// the host is in the Development environment AND the log transport is active. Two
    /// independent gates, because a single one is not enough: on the password-reset
    /// path this value turns "forgot password" into "take over any account", since the
    /// endpoint is unauthenticated and the caller supplies the victim's address.
    /// </summary>
    public bool RevealCodesInResponses { get; private set; }

    /// <summary>Called only from the composition root. See the property remarks.</summary>
    public void EnableCodeRevealForDevelopment() => RevealCodesInResponses = UseLogTransport;

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

        if (DeliversToLog)
        {
            // Logged once at construction, at warning level, so it is obvious in the
            // startup output why no mail is arriving.
            _logger.LogWarning(
                "Email is using the LOG TRANSPORT: verification codes are written to this " +
                "log and no mail is sent. Configure Email:SenderEmail and Email:AppPassword " +
                "and set Email:UseLogTransport=false to send real messages.");
        }
    }

    /// <inheritdoc />
    public bool DeliversToLog => _options.UseLogTransport || !_options.IsConfigured;

    /// <inheritdoc />
    public bool RevealsCodes => _options.RevealCodesInResponses && DeliversToLog;

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
        if (DeliversToLog)
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

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.SenderEmail, _options.AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Sent '{Subject}' to {Recipient}.", subject, recipient);
        }
        catch (Exception ex)
        {
            // The caller turns this into a 503 with a "try again shortly" message. Log
            // the host and port too: an authentication failure against the wrong SMTP
            // server for the sender's domain is by far the most common cause, and the
            // exception alone does not say which server was tried.
            _logger.LogError(ex,
                "SMTP delivery of '{Subject}' to {Recipient} failed via {Host}:{Port} as {Sender}.",
                subject, recipient, _options.SmtpHost, _options.SmtpPort, _options.SenderEmail);

            throw;
        }
    }
}
