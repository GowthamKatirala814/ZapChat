namespace Auth.Infrastructure.Email;

/// <summary>A message to deliver. Both bodies are always supplied.</summary>
public sealed record EmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody,
    string TextBody);

/// <summary>
/// Why a delivery failed, which decides whether retrying can help.
///
/// The distinction is the point: retrying an authentication failure just burns the
/// user's time before they get an error, while retrying a dropped connection usually
/// works on the second attempt.
/// </summary>
public enum EmailFailureKind
{
    /// <summary>Timeout, connection reset, 5xx from the provider. Worth retrying.</summary>
    Transient,

    /// <summary>Bad credentials, expired secret, missing permission. Never retry.</summary>
    Authentication,

    /// <summary>
    /// Sender not authorised, mailbox not found, recipient rejected, message refused.
    /// Never retry — the provider has made a decision about the message itself.
    /// </summary>
    Rejected,

    /// <summary>The configuration cannot produce a valid request at all.</summary>
    Configuration,
}

/// <summary>
/// A delivery failure.
///
/// Carries the provider, host and recipient *domain* for diagnostics — never the
/// credential, and never the full recipient address, so a log shared while debugging
/// does not disclose who is registering.
/// </summary>
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(
        EmailFailureKind kind,
        string provider,
        string endpoint,
        string recipientDomain,
        string message,
        Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        Provider = provider;
        Endpoint = endpoint;
        RecipientDomain = recipientDomain;
    }

    public EmailFailureKind Kind { get; }
    public string Provider { get; }
    public string Endpoint { get; }
    public string RecipientDomain { get; }

    public bool IsRetryable => Kind == EmailFailureKind.Transient;
}

/// <summary>
/// One way of getting a message out. Implementations do exactly one delivery attempt;
/// retrying is the orchestrator's job.
/// </summary>
public interface IEmailSender
{
    /// <summary>Provider name, for logs and diagnostics.</summary>
    string Name { get; }

    /// <summary>Where mail goes — an SMTP host:port or a Graph endpoint. Never a secret.</summary>
    string Endpoint { get; }

    /// <summary>
    /// Delivers the message, or throws <see cref="EmailDeliveryException"/>.
    ///
    /// Returning normally must mean the provider accepted the message. It does not
    /// promise the recipient's mailbox accepted it — no submission API can promise that
    /// — but it does mean the application is no longer responsible for it.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
