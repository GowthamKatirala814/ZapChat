using Auth.Application.Abstractions;
using Auth.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Auth.API.Controllers;

/// <summary>
/// Lets an administrator check the email configuration and prove delivery works.
///
/// Two rules shape this controller. It never returns a secret — only whether one is
/// present. And the test send goes exclusively to the *caller's own* mailbox, taken from
/// their token: an endpoint that accepts a recipient is an open relay behind an
/// authentication check, and one leaked admin session would turn ZapChat into a
/// spam source that authenticates as your own domain.
/// </summary>
[ApiController]
[Route("api/auth/admin/email")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class EmailDiagnosticsController : ControllerBase
{
    private readonly EmailOptions _options;
    private readonly IEmailService _email;
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly ILogger<EmailDiagnosticsController> _logger;

    public EmailDiagnosticsController(
        IOptions<EmailOptions> options,
        IEmailService email,
        ICurrentUser currentUser,
        IUserRepository users,
        ILogger<EmailDiagnosticsController> logger)
    {
        _options = options.Value;
        _email = email;
        _currentUser = currentUser;
        _users = users;
        _logger = logger;
    }

    /// <summary>
    /// The effective email configuration, with every secret reduced to "configured" or
    /// "missing".
    /// </summary>
    [HttpGet("config")]
    public ActionResult<EmailConfigReport> GetConfig()
    {
        var smtp = _options.Smtp;
        var graph = _options.Graph;

        return Ok(new EmailConfigReport(
            Provider: _options.Provider.ToString(),
            DeliversMail: !_options.IsLogTransport,
            SenderEmail: _options.SenderEmail,
            SenderName: _options.SenderName,
            Endpoint: _email.ProviderEndpoint,
            ResendCooldownSeconds: _options.ResendCooldownSeconds,
            RetryMaxAttempts: _options.Retry.MaxAttempts,
            Smtp: _options.Provider == EmailProvider.Smtp
                ? new SmtpReport(
                    Host: smtp.Host,
                    Port: smtp.Port,
                    Security: smtp.Security.ToString(),
                    AuthMode: smtp.AuthMode.ToString(),
                    Username: smtp.Username,
                    // Presence only. The value never leaves the process.
                    PasswordConfigured: !string.IsNullOrWhiteSpace(smtp.Password))
                : null,
            Graph: _options.Provider == EmailProvider.Graph
                ? new GraphReport(
                    TenantIdConfigured: !string.IsNullOrWhiteSpace(graph.TenantId),
                    ClientIdConfigured: !string.IsNullOrWhiteSpace(graph.ClientId),
                    ClientSecretConfigured: !string.IsNullOrWhiteSpace(graph.ClientSecret),
                    Authority: graph.Authority)
                : null));
    }

    /// <summary>
    /// Sends a test message to the calling administrator's own mailbox.
    ///
    /// The recipient is not a parameter, deliberately — see the class remarks.
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<EmailTestResult>> SendTest(CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("That account no longer exists.");

        _logger.LogInformation(
            "Administrator {UserId} requested an email delivery test via {Provider}.",
            userId, _email.ProviderName);

        try
        {
            await _email.SendDeliveryTestAsync(user.Email, ct);
        }
        catch (EmailDeliveryException ex)
        {
            // The failure kind and the provider's hint are genuinely useful to an
            // administrator and contain no credential, so unlike a user-facing error
            // this one says what went wrong.
            return Ok(new EmailTestResult(
                Delivered: false,
                Provider: _email.ProviderName,
                Endpoint: ex.Endpoint,
                Detail: $"{ex.Kind}: {ex.Message}"));
        }

        return Ok(new EmailTestResult(
            Delivered: true,
            Provider: _email.ProviderName,
            Endpoint: _email.ProviderEndpoint,
            Detail: _options.IsLogTransport
                ? "The log transport is active — the message was written to the service log, not sent."
                : "The provider accepted the message. Check the mailbox."));
    }

    public sealed record EmailConfigReport(
        string Provider,
        bool DeliversMail,
        string SenderEmail,
        string SenderName,
        string Endpoint,
        int ResendCooldownSeconds,
        int RetryMaxAttempts,
        SmtpReport? Smtp,
        GraphReport? Graph);

    public sealed record SmtpReport(
        string Host, int Port, string Security, string AuthMode,
        string Username, bool PasswordConfigured);

    public sealed record GraphReport(
        bool TenantIdConfigured, bool ClientIdConfigured, bool ClientSecretConfigured,
        string Authority);

    public sealed record EmailTestResult(
        bool Delivered, string Provider, string Endpoint, string Detail);
}
