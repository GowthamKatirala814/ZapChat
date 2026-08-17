using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Email;

/// <summary>
/// Writes the message to the log instead of sending it.
///
/// This exists for the automated suites, which need to read a verification code without
/// a mailbox to read it from. Three things keep it from becoming the accidental default
/// it used to be:
///
///   * It is only ever constructed when Email:Provider is explicitly "Log". Nothing
///     falls back to it — a misconfigured Graph or SMTP provider fails, loudly.
///   * EmailOptionsValidator refuses it on a Production host.
///   * It announces itself at warning level on every send, so a log that looks like a
///     working system is impossible to mistake for one.
/// </summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public string Name => "Log (no delivery)";

    public string Endpoint => "log://auth-service";

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[EMAIL:NOT-SENT] The log transport is active, so this message was NOT delivered. " +
            "To={Recipient} Subject={Subject}\n{Body}",
            message.ToEmail, message.Subject, message.TextBody);

        return Task.CompletedTask;
    }
}
