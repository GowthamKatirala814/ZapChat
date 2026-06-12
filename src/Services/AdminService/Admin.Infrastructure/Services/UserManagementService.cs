using System.Net.Http.Json;
using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Admin.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IHttpClientFactory httpClientFactory,
        IAuditLogService auditLogService,
        ILogger<UserManagementService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<IEnumerable<AdminUserDto>> GetUsersAsync()
    {
        var authUsers = await FetchAuthUsersAsync();
        return authUsers.Select(MapToAdminUserDto);
    }

    public async Task<IEnumerable<AdminUserDto>> SearchUsersAsync(string query)
    {
        var allUsers = await GetUsersAsync();

        if (string.IsNullOrWhiteSpace(query))
            return allUsers;

        var lowerQuery = query.ToLowerInvariant();

        return allUsers.Where(u =>
            u.AnonymousName.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
            u.Department.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
            u.Branch.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var user = await client.GetFromJsonAsync<AuthUserRecord>($"api/auth/users/{userId}");

            if (user is null) return null;

            return MapToAdminUserDto(user);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteUserAsync(Guid userId, string reason, Guid adminId)
    {
        _logger.LogInformation("DeleteUserAsync called - UserId: {UserId}, AdminId: {AdminId}, Reason: {Reason}", userId, adminId, reason);
        
        // Call Auth Service to soft-delete the user
        try
        {
            _logger.LogInformation("Calling Auth Service soft-delete endpoint for user {UserId}", userId);
            var client = _httpClientFactory.CreateClient("AuthService");
            var response = await client.PatchAsync($"api/auth/users/{userId}/soft-delete", 
                JsonContent.Create(new { adminId, reason }));
            
            _logger.LogInformation("Auth Service response status: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Auth Service soft-delete successful for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user in Auth Service - UserId: {UserId}", userId);
            throw new InvalidOperationException($"Failed to delete user in Auth Service: {ex.Message}", ex);
        }

        await _auditLogService.LogAsync("UserDeleted", "User", userId.ToString(), adminId);
        _logger.LogInformation("DeleteUserAsync completed successfully for user {UserId}", userId);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task<List<AuthUserRecord>> FetchAuthUsersAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var users = await client.GetFromJsonAsync<List<AuthUserRecord>>("api/auth/users");
            return users ?? new List<AuthUserRecord>();
        }
        catch
        {
            return new List<AuthUserRecord>();
        }
    }

    private static AdminUserDto MapToAdminUserDto(AuthUserRecord u)
    {
        return new AdminUserDto
        {
            Id = u.Id,
            AnonymousName = u.AnonymousName,
            Department = u.Department,
            Branch = u.Branch,
            CreatedAt = u.CreatedAt,
            IsDeleted = u.IsDeleted,
            DeletedAt = u.DeletedAt,
            DeletedBy = u.DeletedBy
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
