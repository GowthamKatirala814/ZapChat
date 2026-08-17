using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure.Email;

/// <summary>
/// Ordinary SMTP submission.
///
/// Kept alongside the Graph sender because it is the only option for providers that are
/// not Microsoft 365, and because a tenant that already permits SMTP AUTH can use it
/// without waiting for an app registration. On Microsoft 365 it needs "Authenticated
/// SMTP" enabled for the sending mailbox — OAuth2 does not remove that requirement, it
/// only replaces the password.
///
/// Every message carries both an HTML and a plain-text body as a multipart/alternative,
/// so a client that refuses HTML still shows the code rather than an empty message.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IMicrosoftTokenProvider _tokens;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        IMicrosoftTokenProvider tokens,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _tokens = tokens;
        _logger = logger;
    }

    public string Name => "SMTP";

    public string Endpoint => $"{_options.Smtp.Host}:{_options.Smtp.Port}";

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var domain = GraphEmailSender.DomainOf(message.ToEmail);
        var smtp = _options.Smtp;

        var mime = BuildMessage(message);

        using var client = new SmtpClient
        {
            Timeout = smtp.TimeoutSeconds * 1000,
        };

        try
        {
            var security = smtp.Security switch
            {
                SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
                SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.None,
            };

            await client.ConnectAsync(smtp.Host, smtp.Port, security, ct);

            await AuthenticateAsync(client, ct);

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation(
                "Email accepted by {Provider} {Endpoint} for a recipient at {Domain}: {Subject}",
                Name, Endpoint, domain, message.Subject);
        }
        catch (EmailDeliveryException)
        {
            throw;
        }
        catch (AuthenticationException ex)
        {
            // Never log the exception's full text here without thought — MailKit does not
            // include the password, but the message can echo the username, which is fine,
            // while the credential itself must never appear.
            _logger.LogError(
                "SMTP authentication failed at {Endpoint} as {Username} (auth mode {Mode}). {Hint}",
                Endpoint, UsernameForLog(), smtp.AuthMode, AuthFailureHint());

            throw new EmailDeliveryException(
                EmailFailureKind.Authentication, Name, Endpoint, domain,
                "The mail server rejected the credentials.", ex);
        }
        catch (SmtpCommandException ex)
        {
            // The status code separates "your message is wrong" from "come back later".
            var kind = ex.StatusCode switch
            {
                SmtpStatusCode.MailboxBusy => EmailFailureKind.Transient,
                SmtpStatusCode.TransactionFailed => EmailFailureKind.Transient,
                SmtpStatusCode.ServiceNotAvailable => EmailFailureKind.Transient,
                SmtpStatusCode.InsufficientStorage => EmailFailureKind.Transient,
                _ => EmailFailureKind.Rejected,
            };

            _logger.LogError(
                "SMTP command failed at {Endpoint} for {Domain}: {Status} during {ErrorCode} ({Kind}). {Message}",
                Endpoint, domain, ex.StatusCode, ex.ErrorCode, kind, ex.Message);

            throw new EmailDeliveryException(
                kind, Name, Endpoint, domain,
                $"The mail server refused the message ({ex.StatusCode}).", ex);
        }
        catch (Exception ex) when (ex is SmtpProtocolException or IOException or TimeoutException
                                       or OperationCanceledException)
        {
            _logger.LogError(ex,
                "SMTP transport failure at {Endpoint} for {Domain}.", Endpoint, domain);

            throw new EmailDeliveryException(
                EmailFailureKind.Transient, Name, Endpoint, domain,
                "The connection to the mail server failed.", ex);
        }
    }

    private async Task AuthenticateAsync(SmtpClient client, CancellationToken ct)
    {
        var smtp = _options.Smtp;

        switch (smtp.AuthMode)
        {
            case SmtpAuthMode.None:
                // A local capture server. Nothing to do.
                return;

            case SmtpAuthMode.OAuth2:
            {
                var token = await _tokens.GetTokenAsync(MicrosoftTokenProvider.OutlookScope, ct);

                // XOAUTH2 must be the only advertised mechanism we attempt, otherwise
                // MailKit may fall back to a basic mechanism and send the token where a
                // password is expected.
                client.AuthenticationMechanisms.Clear();
                client.AuthenticationMechanisms.Add("XOAUTH2");

                await client.AuthenticateAsync(
                    new SaslMechanismOAuth2(UsernameOrSender(), token), ct);
                return;
            }

            default:
                await client.AuthenticateAsync(UsernameOrSender(), smtp.Password, ct);
                return;
        }
    }

    private MimeMessage BuildMessage(EmailMessage message)
    {
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;

        if (!string.IsNullOrWhiteSpace(_options.ReplyToEmail))
            mime.ReplyTo.Add(MailboxAddress.Parse(_options.ReplyToEmail));

        // multipart/alternative: clients that render HTML use it, the rest fall back to
        // the text part. Both carry the code.
        mime.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        }.ToMessageBody();

        return mime;
    }

    private string UsernameOrSender() =>
        string.IsNullOrWhiteSpace(_options.Smtp.Username)
            ? _options.SenderEmail
            : _options.Smtp.Username;

    /// <summary>The username is not a secret, but the password must never reach a log.</summary>
    private string UsernameForLog() => UsernameOrSender();

    /// <summary>
    /// What an authentication failure usually means, for this host.
    ///
    /// The cause differs enough between providers that a generic message sends people
    /// down the wrong path — a Microsoft 365 hint shown for a Gmail failure had someone
    /// checking Exchange settings for a problem that was a missing App Password.
    /// </summary>
    private string AuthFailureHint()
    {
        var host = _options.Smtp.Host;

        if (host.Contains("gmail", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("google", StringComparison.OrdinalIgnoreCase))
        {
            return "For Gmail this is almost always the credential: it must be a 16-character " +
                   "App Password (Google Account -> Security -> App passwords, which requires " +
                   "2-Step Verification), not the account password.";
        }

        if (host.Contains("office365", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("outlook", StringComparison.OrdinalIgnoreCase))
        {
            return "On Microsoft 365 this is usually 'Authenticated SMTP' being disabled for the " +
                   "mailbox, or security defaults blocking basic authentication. Consider the " +
                   "Graph provider instead, which needs neither.";
        }

        return "Check the username and credential, and whether the provider requires an " +
               "application-specific password.";
    }
}
