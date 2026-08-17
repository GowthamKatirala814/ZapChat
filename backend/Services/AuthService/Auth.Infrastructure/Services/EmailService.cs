using Auth.Application.Abstractions;
using Auth.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Delivers verification and password-reset mail.
///
/// The orchestration layer: it picks the message, hands it to the configured sender, and
/// retries only the failures worth retrying. It never swallows one — if this method
/// returns, the provider accepted the message, and if it throws, no caller may tell the
/// user their mail is on the way.
///
/// That contract is the whole point of the class. The previous version wrote codes to a
/// log and returned successfully, so the API reported "sent to your email" for messages
/// that were never sent to anyone.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IEmailSender _sender;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailSender sender,
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    public bool DeliversToLog => _options.IsLogTransport;

    public string ProviderName => _sender.Name;

    public string ProviderEndpoint => _sender.Endpoint;

    public Task SendRegistrationOtpAsync(
        string toEmail, string otpCode, string fullName, int expiryMinutes,
        CancellationToken ct = default) =>
        DeliverAsync(
            EmailTemplates.RegistrationCode(toEmail, fullName, otpCode, expiryMinutes, _options.AppUrl),
            "registration",
            ct);

    public Task SendPasswordResetOtpAsync(
        string toEmail, string otpCode, string anonymousName, int expiryMinutes,
        CancellationToken ct = default) =>
        DeliverAsync(
            EmailTemplates.PasswordResetCode(toEmail, anonymousName, otpCode, expiryMinutes, _options.AppUrl),
            "password-reset",
            ct);

    public Task SendDeliveryTestAsync(string toEmail, CancellationToken ct = default) =>
        DeliverAsync(
            EmailTemplates.DeliveryTest(toEmail, _sender.Name, _sender.Endpoint),
            "delivery-test",
            ct);

    /// <summary>
    /// Sends, retrying transient failures with exponential backoff.
    ///
    /// Only transient failures are retried. Repeating an authentication or rejection
    /// failure cannot succeed, and each attempt is time the user spends staring at a
    /// spinner before being told it did not work. The attempt cap is low for a second
    /// reason: a provider that accepted a message and then reported a timeout would, on
    /// retry, deliver a second code — and only the newest one works, so the user reads
    /// the wrong email and the code "does not work".
    /// </summary>
    private async Task DeliverAsync(EmailMessage message, string purpose, CancellationToken ct)
    {
        var domain = GraphEmailSender.DomainOf(message.ToEmail);
        var attempts = Math.Max(1, _options.Retry.MaxAttempts);

        _logger.LogInformation(
            "Sending a {Purpose} email via {Provider} to a recipient at {Domain}.",
            purpose, _sender.Name, domain);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _sender.SendAsync(message, ct);
                return;
            }
            catch (EmailDeliveryException ex) when (ex.IsRetryable && attempt < attempts)
            {
                var delay = TimeSpan.FromMilliseconds(
                    _options.Retry.BaseDelayMs * Math.Pow(2, attempt - 1));

                _logger.LogWarning(
                    "Attempt {Attempt}/{Attempts} to send a {Purpose} email failed ({Kind}); " +
                    "retrying in {Delay}ms. {Message}",
                    attempt, attempts, purpose, ex.Kind, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay, ct);
            }
            catch (EmailDeliveryException ex)
            {
                // Terminal. Everything needed to diagnose it, and no credential: the
                // provider, where it was sending, the recipient's domain, and the class
                // of failure.
                _logger.LogError(ex,
                    "Giving up on a {Purpose} email after {Attempt} attempt(s). " +
                    "Provider={Provider} Endpoint={Endpoint} Domain={Domain} Kind={Kind}",
                    purpose, attempt, ex.Provider, ex.Endpoint, ex.RecipientDomain, ex.Kind);

                throw;
            }
        }
    }
}
