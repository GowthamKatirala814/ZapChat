using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Email;

/// <summary>
/// Sends through Microsoft Graph <c>POST /users/{sender}/sendMail</c>.
///
/// This is the right default for Microsoft 365. SMTP client submission needs SMTP AUTH
/// enabled on the mailbox — off by default on new tenants, commonly disabled by policy
/// on old ones, and on a deprecation path — whereas Graph works through an application
/// permission that an administrator grants once and can scope to a single mailbox.
///
/// The permission needed is <c>Mail.Send</c> (Application, admin consent). Grant it and
/// then restrict it with an Exchange application access policy, so the registration
/// cannot send as anyone other than the ZapChat mailbox.
/// </summary>
public sealed class GraphEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IMicrosoftTokenProvider _tokens;
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<GraphEmailSender> _logger;

    public GraphEmailSender(
        IOptions<EmailOptions> options,
        IMicrosoftTokenProvider tokens,
        IHttpClientFactory httpClients,
        ILogger<GraphEmailSender> logger)
    {
        _options = options.Value;
        _tokens = tokens;
        _httpClients = httpClients;
        _logger = logger;
    }

    public string Name => "Microsoft Graph";

    public string Endpoint =>
        $"{_options.Graph.GraphBaseUrl}/users/{_options.SenderEmail}/sendMail";

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var domain = DomainOf(message.ToEmail);
        var token = await _tokens.GetTokenAsync(MicrosoftTokenProvider.GraphScope, ct);

        // Graph takes one MIME-ish JSON object. `saveToSentItems: false` keeps the
        // service mailbox from accumulating a copy of every verification code — the
        // codes are single-use and short-lived, and a Sent Items folder full of them is
        // a liability rather than an audit trail.
        var payload = new
        {
            message = new
            {
                subject = message.Subject,
                body = new { contentType = "HTML", content = message.HtmlBody },
                toRecipients = new[]
                {
                    new { emailAddress = new { address = message.ToEmail, name = message.ToName } },
                },
                replyTo = string.IsNullOrWhiteSpace(_options.ReplyToEmail)
                    ? null
                    : new[] { new { emailAddress = new { address = _options.ReplyToEmail! } } },
            },
            saveToSentItems = false,
        };

        var client = _httpClients.CreateClient("graph-mail");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload, options: new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            }),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new EmailDeliveryException(
                EmailFailureKind.Transient, Name, Endpoint, domain,
                "Could not reach Microsoft Graph.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Email accepted by {Provider} for a recipient at {Domain}: {Subject}",
                Name, domain, message.Subject);
            return;
        }

        await ThrowForFailureAsync(response, domain, ct);
    }

    /// <summary>
    /// Turns a Graph error into the right failure kind.
    ///
    /// The classification decides whether the orchestrator retries, so it matters that a
    /// 403 (missing permission) is not treated the same as a 503 (Graph busy).
    /// </summary>
    private async Task ThrowForFailureAsync(
        HttpResponseMessage response, string domain, CancellationToken ct)
    {
        var body = await SafeReadAsync(response, ct);
        var status = (int)response.StatusCode;

        var kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => EmailFailureKind.Authentication,
            HttpStatusCode.Forbidden => EmailFailureKind.Authentication,
            HttpStatusCode.NotFound => EmailFailureKind.Configuration,
            HttpStatusCode.TooManyRequests => EmailFailureKind.Transient,
            HttpStatusCode.RequestTimeout => EmailFailureKind.Transient,
            >= HttpStatusCode.InternalServerError => EmailFailureKind.Transient,
            _ => EmailFailureKind.Rejected,
        };

        // The body carries Graph's own diagnosis and no credential — it is the single
        // most useful thing in the log when a grant is missing, so it is kept.
        _logger.LogError(
            "{Provider} rejected a message for {Domain}: HTTP {Status} ({Kind}). Sender={Sender}. Detail: {Detail}",
            Name, domain, status, kind, _options.SenderEmail, Truncate(body, 600));

        var hint = kind switch
        {
            EmailFailureKind.Authentication =>
                "Check that the app registration has the Mail.Send APPLICATION permission " +
                "with admin consent granted, and that the client secret has not expired.",
            EmailFailureKind.Configuration =>
                $"Check that '{_options.SenderEmail}' is a real, licensed mailbox in this tenant.",
            _ => "See the service log for the provider's response.",
        };

        throw new EmailDeliveryException(
            kind, Name, Endpoint, domain,
            $"Microsoft Graph returned HTTP {status}. {hint}");
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return "(no response body)";
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    /// <summary>
    /// The recipient's domain, for logs.
    ///
    /// Only the domain: it is enough to tell an internal delivery from an external one
    /// while a log left open on a screen does not disclose who is signing up.
    /// </summary>
    internal static string DomainOf(string email)
    {
        var at = email.LastIndexOf('@');
        return at >= 0 && at < email.Length - 1 ? email[(at + 1)..] : "unknown";
    }
}
