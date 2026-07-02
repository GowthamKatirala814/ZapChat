using Admin.Application.DTOs;
using Admin.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Admin.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(IUserManagementService userManagementService, ILogger<UserManagementController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all users fetched from Auth Service.
    /// Only AnonymousName is shown — real email and full name are NEVER exposed.
    /// Soft-delete status is fetched from Auth Service.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManagementService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("paginated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersPaginated([FromQuery] UserQueryParameters parameters)
    {
        var result = await _userManagementService.GetUsersPaginatedAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    /// Searches users by AnonymousName, Department, or Branch.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchUsers([FromQuery] string q = "")
    {
        var users = await _userManagementService.SearchUsersAsync(q);
        return Ok(users);
    }

    /// <summary>
    /// Returns a single user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userManagementService.GetUserByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Soft-deletes a user via Auth Service.
    /// The user will be marked as deleted and cannot log in.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid id, [FromBody] DeleteUserRequest request)
    {
        _logger.LogInformation("DELETE USER ENDPOINT HIT - UserId: {UserId}", id);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("DELETE USER - Model state invalid: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
            return BadRequest(ModelState);
        }

        var adminId = GetAdminId();
        _logger.LogInformation("DELETE USER - AdminId: {AdminId}, Reason: {Reason}", adminId, request?.Reason ?? "null");
        
        try
        {
            await _userManagementService.DeleteUserAsync(id, request.Reason, adminId);
            _logger.LogInformation("DELETE USER - Successfully deleted user {UserId}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "DELETE USER - Invalid operation: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE USER - Failed to delete user {UserId}", id);
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    private Guid GetAdminId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
