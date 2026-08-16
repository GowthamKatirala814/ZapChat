using System.Net;
using System.Net.Http.Json;
using Admin.Application;
using Admin.Domain.Documents;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Results;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Every outbound call Admin makes, in one place.
///
/// Three things changed from the old scattered HttpClient usage:
///
///  1. Every client carries a service token, so calls into protected endpoints succeed.
///     Previously only the Auth client forwarded credentials and the rest silently 401'd.
///  2. Failure returns <see cref="Availability{T}"/> — "unavailable" rather than 0 — so
///     an unreachable service is distinguishable from a genuine zero.
///  3. Response shapes are declared once here instead of as private records duplicated
///     across five call sites.
/// </summary>
public sealed class PlatformGateway : IPlatformGateway
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClients;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlatformGateway> _logger;

    public PlatformGateway(
        IHttpClientFactory httpClients, IMemoryCache cache, ILogger<PlatformGateway> logger)
    {
        _httpClients = httpClients;
        _cache = cache;
        _logger = logger;
    }

    private static string ClientFor(ReportTargetKind kind) => kind switch
    {
        ReportTargetKind.RoomMessage => ServiceClients.Chat,
        ReportTargetKind.DirectMessage => ServiceClients.PrivateChat,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // ── Message lookup and removal ──────────────────────────────────────────────

    private sealed record ChatMessageResponse(
        Guid Id, Guid RoomId, string AnonymousName, string Content);

    private sealed record RoomResponse(Guid Id, string Name);

    /// <summary>
    /// Fetches the reported message so the report can snapshot it. For a room message
    /// this also resolves the author's user id, which the old Chat endpoint returned as
    /// Guid.Empty unconditionally — forcing Admin to guess the author by name.
    /// </summary>
    public async Task<MessageSnapshot?> GetMessageAsync(
        ReportTargetKind kind, Guid messageId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ClientFor(kind));
            if (client.BaseAddress is null) return null;

            var path = kind == ReportTargetKind.RoomMessage
                ? $"api/moderation-lookup/messages/{messageId}"
                : $"api/moderation-lookup/direct-messages/{messageId}";

            var response = await client.GetAsync(path, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "{Service} returned {Status} looking up message {MessageId}.",
                    ClientFor(kind), (int)response.StatusCode, messageId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<MessageSnapshot>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up message {MessageId}.", messageId);
            return null;
        }
    }

    /// <summary>
    /// Actually removes the message from the owning service.
    ///
    /// This is the call the old admin path never made: DeleteMessageAsync only marked
    /// reports reviewed and wrote an audit entry, so the UI reported success while the
    /// message stayed visible to every user.
    /// </summary>
    public async Task<bool> RemoveMessageAsync(
        ReportTargetKind kind, Guid messageId, string reason, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ClientFor(kind));

            if (client.BaseAddress is null)
            {
                _logger.LogError(
                    "Cannot remove message {MessageId}: the {Service} URL is not configured.",
                    messageId, ClientFor(kind));
                return false;
            }

            var path = kind == ReportTargetKind.RoomMessage
                ? $"api/chat-admin/messages/{messageId}"
                : $"api/privatechat-admin/messages/{messageId}";

            var request = new HttpRequestMessage(HttpMethod.Delete, path)
            {
                Content = JsonContent.Create(new { reason })
            };

            var response = await client.SendAsync(request, ct);

            if (response.IsSuccessStatusCode) return true;

            // Already gone counts as success — the desired end state holds.
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
                return true;

            _logger.LogError(
                "{Service} returned {Status} removing message {MessageId}.",
                ClientFor(kind), (int)response.StatusCode, messageId);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove message {MessageId}.", messageId);
            return false;
        }
    }

    public async Task<long> RemoveAllMessagesByAuthorAsync(
        Guid authorUserId, string reason, CancellationToken ct = default)
    {
        long removed = 0;

        foreach (var (clientName, path) in new[]
                 {
                     (ServiceClients.Chat, $"api/chat-admin/users/{authorUserId}/remove-messages"),
                     (ServiceClients.PrivateChat,
                         $"api/privatechat-admin/users/{authorUserId}/remove-messages")
                 })
        {
            try
            {
                var client = _httpClients.CreateClient(clientName);
                if (client.BaseAddress is null) continue;

                var response = await client.PostAsJsonAsync(path, new { reason }, ct);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RemovedResponse>(ct);
                    removed += result?.Removed ?? 0;
                }
                else
                {
                    _logger.LogWarning(
                        "{Service} returned {Status} removing messages by {UserId}.",
                        clientName, (int)response.StatusCode, authorUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to remove messages by {UserId} via {Service}.", authorUserId, clientName);
            }
        }

        return removed;
    }

    private sealed record RemovedResponse(long Removed);

    /// <summary>
    /// Disables an account via Auth's internal endpoint. Carries a service token, which
    /// is why it works — the old background service called this unauthenticated, got a
    /// 401, logged a warning, and auto-moderation never actually deleted anyone.
    /// </summary>
    public async Task<bool> DisableAccountAsync(
        Guid userId, string reason, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Auth);
            if (client.BaseAddress is null) return false;

            var response = await client.PostAsJsonAsync(
                $"api/auth/internal/{userId}/soft-delete", new { reason }, ct);

            if (response.IsSuccessStatusCode) return true;

            if (response.StatusCode == HttpStatusCode.Conflict)
                return true; // already disabled

            _logger.LogError(
                "Auth returned {Status} disabling account {UserId}.",
                (int)response.StatusCode, userId);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable account {UserId}.", userId);
            return false;
        }
    }

    private sealed record EmailHashResponse(Guid UserId, string EmailHash);

    /// <summary>
    /// Fetches the SHA-256 of the account's email so a blocked user cannot re-register.
    /// Auth never discloses the address itself.
    /// </summary>
    public async Task<string?> GetEmailHashAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Auth);
            if (client.BaseAddress is null) return null;

            var response = await client.GetAsync($"api/auth/internal/{userId}/email-hash", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Auth returned {Status} fetching the email hash for {UserId}.",
                    (int)response.StatusCode, userId);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<EmailHashResponse>(ct);
            return result?.EmailHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch the email hash for {UserId}.", userId);
            return null;
        }
    }

    private sealed record PublicUserResponse(Guid Id, string AnonymousName);

    public async Task<string?> GetAnonymousNameAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Auth);
            if (client.BaseAddress is null) return null;

            var response = await client.PostAsJsonAsync(
                "api/auth/internal/resolve", new[] { userId }, ct);

            if (!response.IsSuccessStatusCode) return null;

            var users = await response.Content.ReadFromJsonAsync<List<PublicUserResponse>>(ct);
            return users?.FirstOrDefault()?.AnonymousName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve the name for {UserId}.", userId);
            return null;
        }
    }

    // ── Dashboard figures ───────────────────────────────────────────────────────

    private sealed record UserStatsResponse(long Total, long Active, long Deleted);
    private sealed record ChatSummaryResponse(long TotalRooms, long TotalMessages);
    private sealed record PrivateSummaryResponse(long TotalConversations, long TotalMessages);
    private sealed record PollSummaryResponse(long TotalPolls);
    private sealed record NotificationSummaryResponse(long TotalNotifications);

    public Task<Availability<UserCounts>> GetUserCountsAsync(CancellationToken ct = default) =>
        FetchAsync<UserStatsResponse, UserCounts>(
            ServiceClients.Auth, "api/auth/internal/stats", "user counts",
            r => new UserCounts(r.Total, r.Active, r.Deleted), ct);

    public Task<Availability<ChatCounts>> GetChatCountsAsync(CancellationToken ct = default) =>
        FetchAsync<ChatSummaryResponse, ChatCounts>(
            ServiceClients.Chat, "api/chat-admin/analytics/summary", "chat counts",
            r => new ChatCounts(r.TotalRooms, r.TotalMessages), ct);

    public Task<Availability<PrivateChatCounts>> GetPrivateChatCountsAsync(
        CancellationToken ct = default) =>
        FetchAsync<PrivateSummaryResponse, PrivateChatCounts>(
            ServiceClients.PrivateChat, "api/privatechat-admin/analytics/summary",
            "private chat counts",
            r => new PrivateChatCounts(r.TotalConversations, r.TotalMessages), ct);

    public Task<Availability<long>> GetPollCountAsync(CancellationToken ct = default) =>
        FetchAsync<PollSummaryResponse, long>(
            ServiceClients.Poll, "api/poll-admin/analytics/summary", "poll count",
            r => r.TotalPolls, ct);

    public Task<Availability<long>> GetNotificationCountAsync(CancellationToken ct = default) =>
        FetchAsync<NotificationSummaryResponse, long>(
            ServiceClients.Notification, "api/notification-admin/analytics/summary",
            "notification count", r => r.TotalNotifications, ct);

    public Task<Availability<IReadOnlyList<DailyCountDto>>> GetSeriesAsync(
        string service, string path, int days, CancellationToken ct = default) =>
        FetchAsync<List<DailyCountDto>, IReadOnlyList<DailyCountDto>>(
            service, $"{path}?days={days}", $"series {path}", r => r, ct);

    public Task<Availability<IReadOnlyList<NamedCountDto>>> GetNamedCountsAsync(
        string service, string path, int top, CancellationToken ct = default) =>
        FetchAsync<List<NamedCountDto>, IReadOnlyList<NamedCountDto>>(
            service, $"{path}?top={top}", $"counts {path}", r => r, ct);

    public Task<Availability<IReadOnlyList<RoomActivity>>> GetRoomActivityAsync(
        int top, CancellationToken ct = default) =>
        FetchAsync<List<RoomActivity>, IReadOnlyList<RoomActivity>>(
            ServiceClients.Chat, $"api/chat-admin/analytics/top-rooms?top={top}",
            "room activity", r => r, ct);

    /// <summary>
    /// One fetch helper: short cache, explicit unavailability, and a log line that names
    /// the dependency rather than a bare catch that returns zero.
    /// </summary>
    private async Task<Availability<TOut>> FetchAsync<TIn, TOut>(
        string clientName, string path, string description,
        Func<TIn, TOut> map, CancellationToken ct)
    {
        var cacheKey = $"gw:{clientName}:{path}";

        if (_cache.TryGetValue(cacheKey, out Availability<TOut>? cached) && cached is not null)
            return cached;

        Availability<TOut> result;

        try
        {
            var client = _httpClients.CreateClient(clientName);

            if (client.BaseAddress is null)
            {
                result = Availability<TOut>.Unavailable(
                    $"The {clientName} URL is not configured.");
            }
            else
            {
                var response = await client.GetAsync(path, ct);

                if (!response.IsSuccessStatusCode)
                {
                    result = Availability<TOut>.Unavailable(
                        $"{clientName} returned {(int)response.StatusCode}.");

                    _logger.LogWarning(
                        "Could not read {Description}: {Service} returned {Status}.",
                        description, clientName, (int)response.StatusCode);
                }
                else
                {
                    var payload = await response.Content.ReadFromJsonAsync<TIn>(ct);

                    result = payload is null
                        ? Availability<TOut>.Unavailable($"{clientName} returned an empty body.")
                        : Availability<TOut>.Available(map(payload));
                }
            }
        }
        catch (Exception ex)
        {
            result = Availability<TOut>.Unavailable($"{clientName} is unreachable.");
            _logger.LogError(ex, "Could not read {Description} from {Service}.",
                description, clientName);
        }

        // Only successes are cached — a failure should be retried on the next request.
        if (result.IsAvailable)
            _cache.Set(cacheKey, result, CacheFor);

        return result;
    }
}
