using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IWebHostEnvironment env)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    // ── Register (legacy direct endpoint) ────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (existingUser is not null)
        {
            if (existingUser.IsDeleted)
                return BadRequest("This account has been permanently removed and cannot be recreated.");
            return BadRequest("User already exists.");
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = hashedPassword,
            Department = request.Department,
            Branch = request.Branch
        };

        var anonymousProfile = new AnonymousProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AnonymousName = await GenerateUniqueAnonymousNameAsync()
        };

        _context.Users.Add(user);
        _context.AnonymousProfiles.Add(anonymousProfile);
        await _context.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateToken(
            user.Id,
            user.Email,
            anonymousProfile.AnonymousName,
            new List<string>());

        var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id);
        SetAuthCookies(accessToken, refreshToken);

        return Ok(new
        {
            UserId = user.Id,
            Email = user.Email,
            AnonymousName = anonymousProfile.AnonymousName,
            Role = "user"
        });
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user is null)
            return Unauthorized("Invalid email or password.");

        if (user.IsDeleted)
            return Unauthorized("Your account has been permanently removed.");

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            return Unauthorized("Invalid email or password.");

        var anonymousProfile = await _context.AnonymousProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var anonName = anonymousProfile?.AnonymousName ?? "Anonymous";

        // Bootstrap Admin role (idempotent)
        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            string.Equals(user.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureAdminRoleAsync(user);
        }

        var roles = user.Roles.Select(r => r.Name).ToList();
        var role = roles.Contains("Admin") ? "admin" : "user";

        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, anonName, roles);
        var refreshToken = await CreateAndSaveRefreshTokenAsync(user.Id);
        SetAuthCookies(accessToken, refreshToken);

        return Ok(new
        {
            UserId = user.Id,
            AnonymousName = anonName,
            Email = user.Email,
            Role = role
        });
    }

    // ── Token Echo (for SignalR accessTokenFactory) ───────────────────────────
    // Reads the HttpOnly access_token cookie and returns its raw JWT string.
    // Used by the frontend SignalR hubs which cannot read HttpOnly cookies directly.
    [AllowAnonymous]
    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var token = Request.Cookies["access_token"];
        if (string.IsNullOrEmpty(token))
            return Unauthorized("No access token cookie found.");

        return Content(token, "text/plain");
    }

    // ── Refresh Token ─────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshTokenValue = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            ClearAuthCookies();
            return Unauthorized("No refresh token.");
        }

        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(t =>
                t.Token == refreshTokenValue &&
                !t.IsRevoked &&
                t.ExpiryDate > DateTime.UtcNow);

        if (storedToken is null)
        {
            // Token not found or expired — clear cookies, force re-login
            ClearAuthCookies();
            return Unauthorized("Refresh token is invalid or expired.");
        }

        var user = storedToken.User;

        if (user.IsDeleted)
        {
            // Revoke token and clear
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();
            ClearAuthCookies();
            return Unauthorized("Account has been removed.");
        }

        var anonymousProfile = await _context.AnonymousProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);
        var anonName = anonymousProfile?.AnonymousName ?? "Anonymous";

        var roles = user.Roles.Select(r => r.Name).ToList();
        var role = roles.Contains("Admin") ? "admin" : "user";

        // ── Rotation: delete old token, issue new pair ──
        _context.RefreshTokens.Remove(storedToken);

        var newAccessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, anonName, roles);
        var newRefreshToken = await CreateAndSaveRefreshTokenAsync(user.Id);

        SetAuthCookies(newAccessToken, newRefreshToken);

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return Ok(new
        {
            UserId = user.Id,
            AnonymousName = anonName,
            Email = user.Email,
            Role = role
        });
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshTokenValue = Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var stored = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshTokenValue);

            if (stored is not null)
            {
                _context.RefreshTokens.Remove(stored);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Refresh token deleted on logout for user {UserId}", stored.UserId);
            }
        }

        ClearAuthCookies();
        return Ok(new { message = "Logged out successfully." });
    }

    // ── User Queries ──────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Department,
                x.Branch,
                x.CreatedAt,
                x.IsDeleted,
                AnonymousName = _context.AnonymousProfiles
                    .Where(a => a.UserId == x.Id)
                    .Select(a => a.AnonymousName)
                    .FirstOrDefault() ?? "Anonymous"
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool excludeAdmin = false,
        [FromQuery] bool excludeDeleted = false)
    {
        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (excludeAdmin && !string.IsNullOrWhiteSpace(adminEmail))
            query = query.Where(u => u.Email != adminEmail);

        if (excludeDeleted)
            query = query.Where(u => !u.IsDeleted);

        var users = await query
            .Select(x => new
            {
                x.Id,
                x.Department,
                x.Branch,
                x.CreatedAt,
                x.IsDeleted,
                AnonymousName = _context.AnonymousProfiles
                    .Where(a => a.UserId == x.Id)
                    .Select(a => a.AnonymousName)
                    .FirstOrDefault() ?? "Anonymous"
            })
            .ToListAsync();

        return Ok(users);
    }

    [Authorize]
    [HttpGet("users/paginated")]
    public async Task<IActionResult> GetUsersPaginated([FromQuery] UserQueryParameters p)
    {
        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        
        // Base query with AnonymousProfiles mapped inline
        var query = _context.Users.AsNoTracking().Select(x => new
        {
            x.Id,
            x.FullName, // Keep internally for search, don't expose if not needed
            x.Email,     // Keep internally for search
            x.Department,
            x.Branch,
            x.CreatedAt,
            x.IsDeleted,
            AnonymousName = _context.AnonymousProfiles
                .Where(a => a.UserId == x.Id)
                .Select(a => a.AnonymousName)
                .FirstOrDefault() ?? "Anonymous",
            x.DeletedAt,
            x.DeletedBy
        });

        // 1. Exclude Admin
        if (!string.IsNullOrWhiteSpace(adminEmail))
            query = query.Where(u => u.Email != adminEmail);

        // 2. Filter: Status
        if (!string.IsNullOrWhiteSpace(p.Status) && !p.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (p.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => !u.IsDeleted);
            else if (p.Status.Equals("Deleted", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.IsDeleted);
        }

        // 3. Filter: Department
        if (!string.IsNullOrWhiteSpace(p.Department))
            query = query.Where(u => u.Department == p.Department);

        // 4. Filter: Branch
        if (!string.IsNullOrWhiteSpace(p.Branch))
            query = query.Where(u => u.Branch == p.Branch);

        // 5. Search
        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            query = query.Where(u => 
                u.AnonymousName.ToLower().Contains(s) || 
                u.Email.ToLower().Contains(s) || 
                u.FullName.ToLower().Contains(s));
        }

        // 6. Sorting
        var sortBy = p.SortBy?.ToLower() ?? "";
        query = sortBy switch
        {
            "joineddate" => p.SortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            "name"       => p.SortDesc ? query.OrderByDescending(u => u.AnonymousName) : query.OrderBy(u => u.AnonymousName),
            "email"      => p.SortDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "department" => p.SortDesc ? query.OrderByDescending(u => u.Department) : query.OrderBy(u => u.Department),
            "branch"     => p.SortDesc ? query.OrderByDescending(u => u.Branch) : query.OrderBy(u => u.Branch),
            "status"     => p.SortDesc ? query.OrderByDescending(u => u.IsDeleted) : query.OrderBy(u => u.IsDeleted),
            _            => query.OrderByDescending(u => u.CreatedAt) // Default sort
        };

        // 7. Total Count
        var totalCount = await query.CountAsync();

        // 8. Pagination
        var items = await query
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        return Ok(new PaginatedResult<object>
        {
            TotalCount = totalCount,
            Page = p.Page,
            PageSize = p.PageSize,
            Items = items
        });
    }

    // Note: Consolidated into GetUser above. GetUserById kept as a distinct route
    // with a different URL pattern to resolve the routing ambiguity.
    [HttpGet("users/profile/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var anonymousProfile = await _context.AnonymousProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == id);

        return Ok(new
        {
            id = user.Id,
            anonymousName = anonymousProfile?.AnonymousName ?? "Anonymous",
            email = user.Email
        });
    }

    [HttpGet("users/by-name/{anonymousName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserByAnonymousName(string anonymousName)
    {
        var anonymousProfile = await _context.AnonymousProfiles
            .FirstOrDefaultAsync(a => a.AnonymousName == anonymousName);

        if (anonymousProfile == null)
            return NotFound();

        var user = await _context.Users.FindAsync(anonymousProfile.UserId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            id = user.Id,
            anonymousName = anonymousProfile.AnonymousName,
            email = user.Email
        });
    }

    [Authorize]
    [HttpPatch("users/{id}/soft-delete")]
    public async Task<IActionResult> SoftDeleteUser(Guid id, [FromBody] SoftDeleteRequest request)
    {
        _logger.LogInformation("SOFT DELETE ENDPOINT HIT - UserId: {UserId}, AdminId: {AdminId}", id, request?.AdminId);

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogWarning("SOFT DELETE - User not found: {UserId}", id);
            return NotFound();
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("SOFT DELETE - User already deleted: {UserId}", id);
            return BadRequest("User is already deleted.");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = request!.AdminId;

        await _context.SaveChangesAsync();
        _logger.LogInformation("SOFT DELETE - Successfully soft-deleted user {UserId}", id);

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return NotFound();

        var anonymousProfile = await _context.AnonymousProfiles
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            department = user.Department,
            branch = user.Branch,
            createdAt = user.CreatedAt,
            anonymousName = anonymousProfile?.AnonymousName ?? "Anonymous",
            roles = user.Roles.Select(r => r.Name).ToList()
        });
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Department))
            user.Department = request.Department.Trim();

        if (!string.IsNullOrWhiteSpace(request.Branch))
            user.Branch = request.Branch.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            department = user.Department,
            branch = user.Branch
        });
    }

    // ── Cookie helpers ────────────────────────────────────────────────────────

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        // Gateway is HTTPS, Frontend is HTTP -> Scheme mismatch means cross-site.
        // Therefore, we MUST use SameSite=None and Secure=true for cookies to be sent via XHR.
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Required for SameSite=None
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"])),
            Path = "/"
        });

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Required for SameSite=None
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/auth"
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Append("access_token", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UnixEpoch,
            Path = "/"
        });
        Response.Cookies.Append("refresh_token", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UnixEpoch,
            Path = "/api/auth"
        });
    }

    // ── Refresh Token DB helpers ──────────────────────────────────────────────

    private async Task<string> CreateAndSaveRefreshTokenAsync(Guid userId)
    {
        // Generate a cryptographically random token
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var token = Convert.ToBase64String(bytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return token;
    }

    // ── Admin role bootstrap ──────────────────────────────────────────────────

    private async Task EnsureAdminRoleAsync(User user)
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");

        if (adminRole is null)
        {
            adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
            await _context.Roles.AddAsync(adminRole);
        }

        if (!user.Roles.Any(r => r.Name == "Admin"))
            user.Roles.Add(adminRole);

        await _context.SaveChangesAsync();
    }

    // ── OTP / Forgot Password / Reset Password endpoints ─────────────────────
    // (Delegated to other controllers/services — these remain untouched)

    // ── Anonymous name generation ─────────────────────────────────────────────

    private async Task<string> GenerateUniqueAnonymousNameAsync()
    {
        var adjectives = new[]
        {
            "Agile", "Alert", "Ancient", "Arctic", "Ardent", "Atomic", "Astral", "Azure",
            "Blazing", "Bold", "Brave", "Bright", "Brisk", "Broad", "Bronze", "Burning",
            "Calm", "Careful", "Cerulean", "Clever", "Coastal", "Cobalt", "Cold", "Cosmic",
            "Crimson", "Crystal", "Cunning", "Cyan", "Daring", "Dark", "Dauntless", "Deep",
            "Distant", "Driven", "Eager", "Early", "Earnest", "Echo", "Electric", "Elite",
            "Emerald", "Eminent", "Endless", "Epic", "Eternal", "Exact", "Exiled", "Exotic",
            "Fabled", "Fearless", "Fierce", "Final", "Firm", "Flint", "Fluid", "Flying",
            "Focused", "Forged", "Formal", "Frosty", "Gallant", "Gentle", "Glacial", "Gleaming",
            "Glowing", "Golden", "Grand", "Grave", "Great", "Grim", "Guardian", "Hardy",
            "Hasty", "Hazy", "Hidden", "High", "Hollow", "Honest", "Honorable", "Humble",
            "Hushed", "Icy", "Idle", "Immense", "Imperial", "Infinite", "Inner", "Iron",
            "Jade", "Just", "Keen", "Kind", "Last", "Latent", "Lean", "Light", "Limber",
            "Liquid", "Lofty", "Lone", "Lost", "Loyal", "Lucid", "Lunar", "Mellow",
            "Mighty", "Misty", "Mystic", "Natural", "Nimble", "Noble", "Nordic", "Null",
            "Obsidian", "Odd", "Onyx", "Open", "Orbital", "Outer", "Oval", "Pale",
            "Phantom", "Polished", "Precise", "Prime", "Primal", "Pure", "Quiet", "Radiant",
            "Rapid", "Rare", "Remote", "Regal", "Rising", "Roaming", "Robust", "Rocky",
            "Royal", "Rugged", "Runic", "Sacred", "Sapphire", "Scarlet", "Secret", "Serene",
            "Shadow", "Sharp", "Shining", "Silent", "Silver", "Sleek", "Slim", "Smooth",
            "Solar", "Solemn", "Solid", "Speedy", "Stalwart", "Stark", "Steady", "Steel",
            "Stellar", "Stone", "Storm", "Strong", "Subtle", "Swift", "Teal", "Tenacious",
            "Titan", "Towering", "Tranquil", "True", "Twilight", "Unyielding", "Urban", "Vast",
            "Velvet", "Verdant", "Vibrant", "Vigilant", "Violet", "Vivid", "Wandering", "Warm",
            "Wild", "Wise", "Woven", "Zeal", "Zenith", "Zero", "Zonal", "Zephyr"
        };

        var animals = new[]
        {
            "Albatross", "Antelope", "Armadillo", "Badger", "Bat", "Bear", "Bison", "Boar",
            "Buffalo", "Bullfinch", "Cheetah", "Cobra", "Condor", "Crane", "Crow", "Deer",
            "Dingo", "Dolphin", "Dragon", "Eagle", "Eel", "Elephant", "Elk", "Falcon",
            "Ferret", "Finch", "Fisher", "Flamingo", "Fox", "Gecko", "Giraffe", "Gnu",
            "Gorilla", "Grizzly", "Hawk", "Hedgehog", "Heron", "Hippo", "Hornet", "Hyena",
            "Ibis", "Iguana", "Impala", "Jackal", "Jaguar", "Kestrel", "Kite", "Kodiak",
            "Komodo", "Kudu", "Lemur", "Leopard", "Liger", "Limpet", "Lion", "Lizard",
            "Lynx", "Mako", "Mamba", "Mandrill", "Mantis", "Marlin", "Mink", "Mole",
            "Mongoose", "Monitor", "Moose", "Mustang", "Narwhal", "Newt", "Ocelot", "Osprey",
            "Otter", "Owl", "Panther", "Peregrine", "Phoenix", "Puma", "Python", "Raven",
            "Rhino", "Salamander", "Scorpion", "Shark", "Sparrow", "Stallion", "Stingray",
            "Stoat", "Swift", "Talon", "Tapir", "Tiger", "Viper", "Vulture", "Walrus",
            "Weasel", "Wolf", "Wolverine", "Wombat", "Yak", "Zebra"
        };

        var random = Random.Shared;
        var adjectiveIndices = Enumerable.Range(0, adjectives.Length).OrderBy(_ => random.Next()).ToArray();
        var animalIndices = Enumerable.Range(0, animals.Length).OrderBy(_ => random.Next()).ToArray();

        foreach (var ai in adjectiveIndices)
        {
            foreach (var ni in animalIndices)
            {
                var candidate = adjectives[ai] + animals[ni];
                var exists = await _context.AnonymousProfiles.AnyAsync(x => x.AnonymousName == candidate);
                if (!exists)
                    return candidate;
            }
        }

        throw new InvalidOperationException(
            "Anonymous name pool exhausted. All 20,000 combinations are taken. " +
            "Please expand the adjective or animal lists.");
    }
}