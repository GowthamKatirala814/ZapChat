using System.Net.Http.Json;
using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Microsoft.Extensions.Options;
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

    public DashboardService(
        IHttpClientFactory httpClientFactory,
        IBlockedUserRepository blockedUserRepository,
        IRoomManagementRepository roomManagementRepository,
        IReportRepository reportRepository,
        IAuditLogRepository auditLogRepository,
        IOptions<ServiceUrlsOptions> serviceUrls)
    {
        _httpClientFactory = httpClientFactory;
        _blockedUserRepository = blockedUserRepository;
        _roomManagementRepository = roomManagementRepository;
        _reportRepository = reportRepository;
        _auditLogRepository = auditLogRepository;
        _serviceUrls = serviceUrls.Value;
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
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_serviceUrls.AuthService}/api/auth/users");
            Console.WriteLine($"[DashboardService] Auth API response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var users = await response.Content.ReadFromJsonAsync<List<AuthUserRecord>>();
                Console.WriteLine($"[DashboardService] Users fetched: {users?.Count ?? 0}");

                if (users != null)
                {
                    // Get all blocked user IDs from Admin DB
                    var blockedUsersList = await _blockedUserRepository.GetAllAsync();
                    var blockedUserIds = blockedUsersList.Select(b => b.UserId).ToHashSet();

                    Console.WriteLine($"[DashboardService] Blocked users count: {blockedUserIds.Count}");

                    foreach (var u in users)
                    {
                        var isBlocked = blockedUserIds.Contains(u.Id);
                        Console.WriteLine($"[DashboardService] User {u.Id}: IsDeleted={u.IsDeleted}, IsBlocked={isBlocked}");
                    }

                    // Active = NOT deleted AND NOT blocked
                    var activeUsers = users.Where(u => !u.IsDeleted && !blockedUserIds.Contains(u.Id)).Count();
                    var deletedUsers = users.Where(u => u.IsDeleted).Count();
                    var totalUsers = users.Count;
                    var blockedUsers = blockedUserIds.Count;

                    Console.WriteLine($"[DashboardService] Calculated: Total={totalUsers}, Active={activeUsers}, Deleted={deletedUsers}, Blocked={blockedUsers}");
                    return (totalUsers, activeUsers, deletedUsers, blockedUsers);
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DashboardService] Auth API error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DashboardService] Exception in GetUserStatsAsync: {ex.Message}");
        }
        return (0, 0, 0, 0);
    }

    /// <summary>
    /// Gets total room count from ChatService (source of truth for rooms).
    /// This ensures we count all rooms including those created before AdminService sync.
    /// </summary>
    private async Task<int> GetTotalRoomsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_serviceUrls.ChatService}/api/admin/rooms";
            Console.WriteLine($"[DashboardService] Fetching rooms from: {url}");

            var response = await client.GetAsync(url);
            Console.WriteLine($"[DashboardService] Room API response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var rooms = await response.Content.ReadFromJsonAsync<List<ChatRoomRecord>>();
                Console.WriteLine($"[DashboardService] Rooms fetched: {rooms?.Count ?? 0}");
                return rooms?.Count ?? 0;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DashboardService] Room API error: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DashboardService] Error fetching rooms: {ex.Message}");
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
