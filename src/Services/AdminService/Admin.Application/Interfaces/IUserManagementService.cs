using Admin.Application.DTOs;

namespace Admin.Application.Interfaces;

public interface IUserManagementService
{
    Task<IEnumerable<AdminUserDto>> GetUsersAsync();
    Task<IEnumerable<AdminUserDto>> SearchUsersAsync(string query);
    Task<AdminUserDto?> GetUserByIdAsync(Guid userId);
    Task DeleteUserAsync(Guid userId, string reason, Guid adminId);
    Task<PaginatedResult<AdminUserDto>> GetUsersPaginatedAsync(UserQueryParameters parameters);
}
