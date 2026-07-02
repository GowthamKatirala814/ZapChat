using System.Net.Http.Json;
using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Admin.Infrastructure.Configuration;

namespace Admin.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBlockedUserRepository _blockedUserRepository;
    private readonly IRoomManagementRepository _roomManagementRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ServiceUrlsOptions _serviceUrls;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IHttpClientFactory httpClientFactory,
        IBlockedUserRepository blockedUserRepository,
        IRoomManagementRepository roomManagementRepository,
        IReportRepository reportRepository,
        IAuditLogRepository auditLogRepository,
        IOptions<ServiceUrlsOptions> serviceUrls,
        IMemoryCache cache,
        ILogger<DashboardService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _blockedUserRepository = blockedUserRepository;
        _roomManagementRepository = roomManagementRepository;
        _reportRepository = reportRepository;
        _auditLogRepository = auditLogRepository;
        _serviceUrls = serviceUrls.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var (totalUsers, activeUsers, deletedUsers, blockedUsers) = await GetUserStatsAsync();
        var totalRooms = await GetTotalRoomsAsync();
        var totalMessages = await GetTotalMessagesFromChatAsync();
        var totalPrivateConversations = await GetTotalConversationsFromPrivateChatAsync();
        var totalPolls = await GetTotalPollsFromPollServiceAsync();
        var totalNotifications = await GetTotalNotificationsFromNotificationServiceAsync();
        var totalReports = await _reportRepository.GetTotalCountAsync();
        var pendingReports = await _reportRepository.GetPendingCountAsync();

        return new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            DeletedUsers = deletedUsers,
            BlockedUsers = blockedUsers,
            TotalChatRooms = totalRooms,
            TotalPrivateConversations = totalPrivateConversations,
            TotalMessages = totalMessages,
            TotalPolls = totalPolls,
            TotalNotifications = totalNotifications,
            TotalReports = totalReports,
            PendingReports = pendingReports
        };
    }

    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 20)
    {
        var logs = await _auditLogRepository.GetAllAsync(page: 1, pageSize: count);

        return logs.Select(log => new RecentActivityDto
        {
            Id = log.Id,
            ActivityType = log.Action,
            Description = $"{log.Action} on {log.EntityType} [{log.EntityId}]",
            TargetId = log.EntityId,
            TargetType = log.EntityType,
            Timestamp = log.Timestamp
        });
    }

    private async Task<(int totalUsers, int activeUsers, int deletedUsers, int blockedUsers)> GetUserStatsAsync()
    {
        const string cacheKey = "dashboard_user_stats";
        if (_cache.TryGetValue(cacheKey, out (int t, int a, int d, int b) cached))
            return cached;

        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            // excludeAdmin=true ensures the administrator account is never counted
            // in any user statistic — admin is a system account, not a platform user.
            var response = await client.GetAsync($"{_serviceUrls.AuthService}/api/auth/users?excludeAdmin=true");
            _logger.LogInformation("[DashboardService] Auth API response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var users = await response.Content.ReadFromJsonAsync<List<AuthUserRecord>>();
                _logger.LogInformation("[DashboardService] Users fetched (admin excluded): {Count}", users?.Count ?? 0);

                if (users != null)
                {
                    // Get all blocked user IDs from Admin DB
                    var blockedUsersList = await _blockedUserRepository.GetAllAsync();
                    var blockedUserIds = blockedUsersList.Select(b => b.UserId).ToHashSet();

                    _logger.LogDebug("[DashboardService] Blocked users count: {Count}", blockedUserIds.Count);

                    // TotalUsers  = all non-admin accounts (active + deleted)
                    // ActiveUsers = non-deleted (blocked are a subset of active — they can't log in but are not erased)
                    // DeletedUsers = soft-deleted accounts
                    var totalUsers   = users.Count;
                    var activeUsers  = users.Count(u => !u.IsDeleted);
                    var deletedUsers = users.Count(u => u.IsDeleted);
                    var blockedUsers = blockedUserIds.Count;

                    _logger.LogDebug("[DashboardService] Stats: Total={Total}, Active={Active}, Deleted={Deleted}, Blocked={Blocked}",
                        totalUsers, activeUsers, deletedUsers, blockedUsers);

                    var result = (totalUsers, activeUsers, deletedUsers, blockedUsers);
                    _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
                    return result;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[DashboardService] Auth API error: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardService] Exception in GetUserStatsAsync");
        }
        return (0, 0, 0, 0);
    }

    public async Task<int> GetActiveUserCountAsync()
    {
        var (_, activeUsers, _, _) = await GetUserStatsAsync();
        return activeUsers;
    }

    /// <summary>
    /// Gets total room count from ChatService (source of truth for rooms).
    /// This ensures we count all rooms including those created before AdminService sync.
    /// </summary>
    private async Task<int> GetTotalRoomsAsync()
    {
        const string cacheKey = "dashboard_room_count";
        if (_cache.TryGetValue(cacheKey, out int cachedCount))
            return cachedCount;

        try
        {
            var client = _httpClientFactory.CreateClient("ChatService");
            var url = $"{_serviceUrls.ChatService}/api/admin/rooms";
            _logger.LogInformation("[DashboardService] Fetching rooms from: {Url}", url);

            var response = await client.GetAsync(url);
            _logger.LogInformation("[DashboardService] Room API response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var rooms = await response.Content.ReadFromJsonAsync<List<ChatRoomRecord>>();
                var count = rooms?.Count ?? 0;
                _logger.LogDebug("[DashboardService] Rooms fetched: {Count}", count);
                _cache.Set(cacheKey, count, TimeSpan.FromSeconds(30));
                return count;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[DashboardService] Room API error: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DashboardService] Error fetching rooms");
        }
        return 0;
    }

    private sealed record ChatRoomRecord(Guid Id, string Name, string? RoomType, DateTime CreatedAt);

    private async Task<int> GetTotalMessagesFromChatAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_serviceUrls.ChatService}/api/admin/messages/summary");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<MessageSummaryDto>();
                return data?.totalMessages ?? 0;
            }
        }
        catch
        {
            // Chat Service unavailable — dashboard degrades gracefully
        }
        return 0;
    }

    private async Task<int> GetTotalConversationsFromPrivateChatAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_serviceUrls.PrivateChatService}/api/admin/conversations/summary");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<ConversationSummaryDto>();
                return data?.totalPrivateConversations ?? 0;
            }
        }
        catch
        {
            // PrivateChat Service unavailable — dashboard degrades gracefully
        }
        return 0;
    }

    private async Task<int> GetTotalPollsFromPollServiceAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_serviceUrls.PollService}/api/admin/polls/summary");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PollSummaryDto>();
                return data?.totalPolls ?? 0;
            }
        }
        catch
        {
            // Poll Service unavailable — dashboard degrades gracefully
        }
        return 0;
    }

    private async Task<int> GetTotalNotificationsFromNotificationServiceAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_serviceUrls.NotificationService}/api/admin/notifications/summary");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<NotificationSummaryDto>();
                return data?.totalNotifications ?? 0;
            }
        }
        catch
        {
            // Notification Service unavailable — dashboard degrades gracefully
        }
        return 0;
    }

    private sealed record RoomSummaryDto(int totalRooms);
    private sealed record MessageSummaryDto(int totalMessages);
    private sealed record ConversationSummaryDto(int totalPrivateConversations);
    private sealed record PollSummaryDto(int totalPolls);
    private sealed record NotificationSummaryDto(int totalNotifications);

private class AuthUserRecord
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
}
}
