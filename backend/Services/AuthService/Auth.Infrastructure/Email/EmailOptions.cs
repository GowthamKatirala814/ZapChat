using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Email;

/// <summary>How outbound mail is delivered.</summary>
public enum EmailProvider
{
    /// <summary>
    /// Not chosen. Startup fails rather than picking something — a default here is how
    /// a deployment ends up quietly not sending mail.
    /// </summary>
    None = 0,

    /// <summary>
    /// Microsoft Graph <c>sendMail</c> with OAuth2 client credentials. The recommended
    /// path for Microsoft 365: it needs no SMTP AUTH, so it keeps working on tenants
    /// where client SMTP submission is disabled — which is the default for new tenants
    /// and the direction Microsoft is moving.
    /// </summary>
    Graph = 1,

    /// <summary>Ordinary SMTP submission, with either a password or an OAuth2 token.</summary>
    Smtp = 2,

    /// <summary>
    /// Writes the message to the log instead of sending it.
    ///
    /// For automated tests only, and it must be chosen explicitly — nothing ever falls
    /// back to it. Selecting it on a Production host is a startup error, because a
    /// server that silently logs verification codes instead of mailing them looks
    /// healthy while every registration is broken.
    /// </summary>
    Log = 3,
}

/// <summary>How the SMTP connection is secured.</summary>
public enum SmtpSecurity
{
    /// <summary>Upgrade a plaintext connection with STARTTLS. Port 587.</summary>
    StartTls = 0,

    /// <summary>TLS from the first byte. Port 465.</summary>
    SslOnConnect = 1,

    /// <summary>
    /// No transport security. Only for a local capture server such as Papercut or
    /// MailHog; credentials would cross the wire in the clear otherwise.
    /// </summary>
    None = 2,
}

/// <summary>How the SMTP session authenticates.</summary>
public enum SmtpAuthMode
{
    /// <summary>Mailbox password or app password. Requires SMTP AUTH on the mailbox.</summary>
    Password = 0,

    /// <summary>
    /// XOAUTH2 using the same client credentials as the Graph provider. Still requires
    /// SMTP AUTH to be enabled for the mailbox — OAuth replaces the password, not the
    /// protocol permission.
    /// </summary>
    OAuth2 = 1,

    /// <summary>Anonymous submission, for a local capture server.</summary>
    None = 2,
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Required. See <see cref="EmailProvider"/>.</summary>
    public EmailProvider Provider { get; set; } = EmailProvider.None;

    /// <summary>
    /// The From address. Must be a real mailbox the provider authorises this application
    /// to send as — for Graph that means the mailbox targeted by the application access
    /// policy, not merely an address in the domain.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "ZapChat";

    /// <summary>Optional. Where replies go, if that is not the sender mailbox.</summary>
    public string? ReplyToEmail { get; set; }

    /// <summary>Public base URL of the app, used for links in email bodies.</summary>
    public string AppUrl { get; set; } = "http://localhost:5173";

    public SmtpOptions Smtp { get; set; } = new();
    public GraphOptions Graph { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Smallest gap between two codes for the same address, enforced in the application
    /// rather than at the gateway. The gateway limiter partitions by IP, which does not
    /// stop the same mailbox being targeted from many addresses — the shape "email
    /// bombing" actually takes.
    /// </summary>
    [Range(0, 3600)]
    public int ResendCooldownSeconds { get; set; } = 60;

    public bool IsLogTransport => Provider == EmailProvider.Log;
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;
    public SmtpAuthMode AuthMode { get; set; } = SmtpAuthMode.Password;

    /// <summary>Usually the sender mailbox. Defaults to SenderEmail when blank.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Secret. Supplied only through the environment or user-secrets — never committed,
    /// and never written to a log.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Microsoft Entra application credentials for Graph and for SMTP XOAUTH2.
/// </summary>
public sealed class GraphOptions
{
    /// <summary>Directory (tenant) id, or the verified domain.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Application (client) id of the app registration.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Secret. Environment or user-secrets only.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Sovereign-cloud override; the default is the global endpoint.</summary>
    public string Authority { get; set; } = "https://login.microsoftonline.com";

    public string GraphBaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}

public sealed class RetryOptions
{
    /// <summary>
    /// Total attempts, not retries. Bounded deliberately: a verification email that has
    /// not gone out in three tries is not going out, and repeated attempts risk the user
    /// receiving several codes of which only the newest works.
    /// </summary>
    [Range(1, 5)]
    public int MaxAttempts { get; set; } = 3;

    [Range(50, 10_000)]
    public int BaseDelayMs { get; set; } = 400;
}

/// <summary>
/// Validates the email configuration at startup.
///
/// Registered with ValidateOnStart, so a misconfigured host fails immediately and
/// loudly instead of accepting registrations and dropping every message. The messages
/// name the exact setting to fix and never echo a secret's value.
/// </summary>
public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    private readonly bool _isProduction;

    public EmailOptionsValidator(bool isProduction) => _isProduction = isProduction;

    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var errors = new List<string>();

        if (options.Provider == EmailProvider.None)
        {
            errors.Add(
                "Email:Provider is not set. Choose 'Graph' (recommended for Microsoft 365), " +
                "'Smtp', or 'Log' (automated tests only).");
        }

        if (options.Provider == EmailProvider.Log && _isProduction)
        {
            errors.Add(
                "Email:Provider is 'Log', which does not send mail. That is a test-only " +
                "transport and is refused in Production.");
        }

        if (options.Provider is EmailProvider.Graph or EmailProvider.Smtp)
        {
            if (string.IsNullOrWhiteSpace(options.SenderEmail))
                errors.Add("Email:SenderEmail is required — it is the mailbox mail is sent from.");
            else if (!options.SenderEmail.Contains('@'))
                errors.Add("Email:SenderEmail is not an email address.");
        }

        if (options.Provider == EmailProvider.Graph && !options.Graph.IsConfigured)
        {
            errors.Add(
                "Email:Graph needs TenantId, ClientId and ClientSecret. Supply the secret " +
                "through ZAPCHAT_EMAIL__GRAPH__CLIENTSECRET or user-secrets.");
        }

        if (options.Provider == EmailProvider.Smtp)
        {
            if (string.IsNullOrWhiteSpace(options.Smtp.Host))
                errors.Add("Email:Smtp:Host is required.");

            if (options.Smtp.Port is < 1 or > 65535)
                errors.Add("Email:Smtp:Port must be between 1 and 65535.");

            if (options.Smtp.AuthMode == SmtpAuthMode.Password &&
                string.IsNullOrWhiteSpace(options.Smtp.Password))
            {
                errors.Add(
                    "Email:Smtp:Password is required for AuthMode 'Password'. Supply it " +
                    "through ZAPCHAT_EMAIL__SMTP__PASSWORD or user-secrets.");
            }

            if (options.Smtp.AuthMode == SmtpAuthMode.OAuth2 && !options.Graph.IsConfigured)
            {
                errors.Add(
                    "SMTP AuthMode 'OAuth2' uses the Email:Graph credentials, which are " +
                    "not configured.");
            }

            if (options.Smtp.Security == SmtpSecurity.None &&
                options.Smtp.AuthMode != SmtpAuthMode.None && _isProduction)
            {
                errors.Add(
                    "Email:Smtp:Security 'None' would send credentials in cleartext. " +
                    "Refused in Production.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
