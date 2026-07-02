using System.Net.Http.Json;
using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Admin.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Admin.Infrastructure.Configuration;

namespace Admin.Infrastructure.Services;

public class RoomManagementService : IRoomManagementService
{
    private readonly IRoomManagementRepository _repository;
    private readonly IRoomMembershipRepository _membershipRepository;
    private readonly IBlockedUserRepository _blockedUserRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceUrlsOptions _serviceUrls;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<RoomManagementService> _logger;

    public RoomManagementService(
        IRoomManagementRepository repository,
        IRoomMembershipRepository membershipRepository,
        IBlockedUserRepository blockedUserRepository,
        IReportRepository reportRepository,
        IAuditLogService auditLogService,
        IHttpClientFactory httpClientFactory,
        IOptions<ServiceUrlsOptions> serviceUrls,
        IDashboardService dashboardService,
        ILogger<RoomManagementService> logger)
    {
        _repository = repository;
        _membershipRepository = membershipRepository;
        _blockedUserRepository = blockedUserRepository;
        _reportRepository = reportRepository;
        _auditLogService = auditLogService;
        _httpClientFactory = httpClientFactory;
        _serviceUrls = serviceUrls.Value;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public async Task<IEnumerable<RoomDto>> GetRoomsAsync(bool includeDeleted = false)
    {
        var rooms = await _repository.GetAllAsync(includeDeleted);
        var roomDtos = new List<RoomDto>();

        foreach (var room in rooms)
        {
            var dto = await MapToDtoWithMembersAsync(room);
            roomDtos.Add(dto);
        }

        return roomDtos;
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid roomId)
    {
        var room = await _repository.GetByIdAsync(roomId);
        return room is null ? null : await MapToDtoWithMembersAsync(room);
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomRequest request, Guid adminId)
    {
        var room = new RoomManagement
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdmin = adminId,
            IsDeleted = false
        };

        await _repository.AddAsync(room);

        // Sync room creation with ChatService
        await SyncRoomCreationWithChatServiceAsync(room);

        // Add all existing users as members of the new room
        await AddAllUsersToRoomAsync(room.Id);

        await _auditLogService.LogAsync("RoomCreated", "Room", room.Id.ToString(), adminId);

        return await MapToDtoWithMembersAsync(room);
    }

    private async Task SyncRoomCreationWithChatServiceAsync(RoomManagement room)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ChatService");
            var payload = new
            {
                Id = room.Id,
                Name = room.Name,
                RoomType = "Public"
            };

            var response = await client.PostAsJsonAsync($"{_serviceUrls.ChatService}/api/admin/rooms", payload);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[RoomManagementService] Room '{RoomName}' synced to ChatService", room.Name);
            }
            else
            {
                _logger.LogWarning("[RoomManagementService] Failed to sync room to ChatService: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomManagementService] Error syncing room '{RoomName}' to ChatService", room.Name);
            // Don't fail room creation if sync fails - room will still work in AdminService
        }
    }

    private async Task AddAllUsersToRoomAsync(Guid roomId)
    {
        try
        {
            // Fetch all users from Auth Service
            var client = _httpClientFactory.CreateClient();
            var users = await client.GetFromJsonAsync<List<AuthUserRecord>>($"{_serviceUrls.AuthService}/api/auth/users");
            
            if (users is null || users.Count == 0) return;

            // Create memberships for all non-deleted users
            var memberships = users
                .Where(u => !u.IsDeleted)
                .Select(u => new RoomMembership
                {
                    RoomId = roomId,
                    UserId = u.Id,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                })
                .ToList();

            if (memberships.Count > 0)
            {
                await _membershipRepository.AddRangeAsync(memberships);
            }
        }
        catch
        {
            // Log error but don't fail room creation
            // Memberships can be added later
        }
    }

    public async Task<RoomDto> UpdateRoomAsync(Guid roomId, UpdateRoomRequest request, Guid adminId)
    {
        var room = await _repository.GetByIdAsync(roomId);

        if (room is null)
            throw new KeyNotFoundException($"Room {roomId} not found.");

        if (room.IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted room.");

        room.Name = request.Name;
        room.Description = request.Description;
        room.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(room);
        await _auditLogService.LogAsync("RoomUpdated", "Room", roomId.ToString(), adminId);

        return MapToDto(room);
    }

    public async Task DeleteRoomAsync(Guid roomId, Guid adminId)
    {
        var room = await _repository.GetByIdAsync(roomId);

        if (room is null)
        {
            // Room not in Admin DB - might be an old room only in ChatService
            // Try to delete from ChatService directly
            _logger.LogWarning("[RoomManagementService] Room {RoomId} not found in Admin DB, attempting ChatService deletion", roomId);
            await SyncRoomDeletionWithChatServiceAsync(roomId);

            // Log the deletion even if room wasn't in our DB
            await _auditLogService.LogAsync("RoomDeleted", "Room", roomId.ToString(), adminId);
            return;
        }

        if (room.IsDeleted)
            return; // idempotent

        await _repository.SoftDeleteAsync(roomId, DateTime.UtcNow);

        // Remove all memberships for this room
        await _membershipRepository.RemoveAllForRoomAsync(roomId);

        await _auditLogService.LogAsync("RoomDeleted", "Room", roomId.ToString(), adminId);

        // Sync room deletion with ChatService
        await SyncRoomDeletionWithChatServiceAsync(roomId);
    }

    private async Task SyncRoomDeletionWithChatServiceAsync(Guid roomId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ChatService");
            var response = await client.DeleteAsync($"{_serviceUrls.ChatService}/api/admin/rooms/{roomId}");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[RoomManagementService] Room '{RoomId}' deleted from ChatService", roomId);
            }
            else
            {
                _logger.LogWarning("[RoomManagementService] Failed to delete room from ChatService: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomManagementService] Error deleting room {RoomId} from ChatService", roomId);
            // Don't fail room deletion if sync fails - room is already soft-deleted in AdminService
        }
    }

    public async Task AddUserToAllRoomsAsync(Guid userId)
    {
        // Get all active rooms
        var rooms = await _repository.GetAllAsync(includeDeleted: false);
        
        foreach (var room in rooms)
        {
            if (!await _membershipRepository.IsMemberAsync(room.Id, userId))
            {
                await _membershipRepository.AddAsync(new RoomMembership
                {
                    RoomId = room.Id,
                    UserId = userId,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }
    }

    public async Task<IEnumerable<RoomMemberDto>> GetMembersAsync(Guid roomId)
    {
        var memberships = await _membershipRepository.GetByRoomIdAsync(roomId);
        var activeUserIds = memberships.Where(m => m.IsActive).Select(m => m.UserId).ToHashSet();

        if (activeUserIds.Count == 0) return Array.Empty<RoomMemberDto>();

        try
        {
            var client = _httpClientFactory.CreateClient();
            var users = await client.GetFromJsonAsync<List<AuthUserRecord>>($"{_serviceUrls.AuthService}/api/auth/users");
            if (users != null)
            {
                return users
                    .Where(u => activeUserIds.Contains(u.Id))
                    .Select(u => new RoomMemberDto(u.Id, u.AnonymousName))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomManagementService] Error fetching users for room members mapping");
        }
        
        return activeUserIds.Select(id => new RoomMemberDto(id, "Anonymous"));
    }

    public async Task<RoomStatsDto> GetRoomStatsAsync(Guid roomId)
    {
        var room = await _repository.GetByIdAsync(roomId);

        if (room is null)
            throw new KeyNotFoundException($"Room {roomId} not found.");

        // Reports for this specific room's messages
        var allRoomReportCounts = await _reportRepository.GetReportCountsByRoomAsync();
        var roomReportCount = allRoomReportCounts
            .FirstOrDefault(r => r.RoomId == roomId)
            .ReportCount;

        return new RoomStatsDto
        {
            RoomId = roomId,
            RoomName = room.Name,
            ReportsCount = roomReportCount,
            // Integration points: require ChatService to expose per-room message/active-user counts
            MessagesCount = 0,
            ActiveUsers = 0
        };
    }

    private static RoomDto MapToDto(RoomManagement r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        IsDeleted = r.IsDeleted,
        DeletedAt = r.DeletedAt,
        CreatedByAdmin = r.CreatedByAdmin,
        CreatedByAdminName = "", // Will be populated by caller if needed
        MemberCount = 0 // Will be populated by async method
    };

    private async Task<RoomDto> MapToDtoWithMembersAsync(RoomManagement r)
    {
        // Centralized logic: use the exact same Active Users count from the Dashboard
        // to guarantee 100% synchronization and a single source of truth across the UI.
        var memberCount = await _dashboardService.GetActiveUserCountAsync();

        return new RoomDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsDeleted = r.IsDeleted,
            DeletedAt = r.DeletedAt,
            CreatedByAdmin = r.CreatedByAdmin,
            CreatedByAdminName = "", // Could fetch admin name if needed
            MemberCount = memberCount
        };
    }

    private sealed record AuthUserRecord(
        Guid Id, 
        string AnonymousName, 
        string Department, 
        string Branch, 
        DateTime CreatedAt, 
        bool IsDeleted,
        DateTime? DeletedAt,
        Guid? DeletedBy);
}
