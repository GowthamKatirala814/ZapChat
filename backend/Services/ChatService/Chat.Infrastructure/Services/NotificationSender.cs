using System.Net.Http.Json;
using Chat.Application.Abstractions;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Configuration;

namespace Chat.Infrastructure.Services;

/// <summary>
/// Posts notifications to the notification service.
///
/// Two things were wrong before: the typed HttpClient had no BaseAddress because
/// ServiceUrls:NotificationService was missing from Chat's appsettings.json (so every
/// call threw), and the call carried no credentials. Both are fixed by resolving the
/// client by name from configuration and attaching a service token.
///
/// Failures are logged and swallowed on purpose — a notification is not worth failing
/// a delivered message over — but the log line names the cause, unlike the bare
/// catch {} it replaces.
/// </summary>
public sealed class NotificationSender : INotificationSender
{
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(IHttpClientFactory httpClients, ILogger<NotificationSender> logger)
    {
        _httpClients = httpClients;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid userId, string title, string message, string type,
        Guid? sourceId = null, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Notification);

            if (client.BaseAddress is null)
            {
                _logger.LogWarning(
                    "ServiceUrls:NotificationService is not configured; dropped a {Type} notification for {UserId}.",
                    type, userId);
                return;
            }

            var response = await client.PostAsJsonAsync("api/notifications/internal", new
            {
                userId, title, message, type, sourceId
            }, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Notification service returned {Status} for a {Type} notification to {UserId}.",
                    (int)response.StatusCode, type, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Could not deliver a {Type} notification to {UserId}.", type, userId);
        }
    }

    public async Task DeleteBySourceAsync(Guid sourceId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Notification);
            if (client.BaseAddress is null) return;

            await client.DeleteAsync($"api/notifications/internal/by-source/{sourceId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Could not remove notifications for source {SourceId}.", sourceId);
        }
    }
}

/// <summary>Supplies the content root to Infrastructure without an ASP.NET reference.</summary>
public sealed class HostEnvironmentAccessor : IHostEnvironmentAccessor
{
    public HostEnvironmentAccessor(string contentRootPath) => ContentRootPath = contentRootPath;

    public string ContentRootPath { get; }
}
