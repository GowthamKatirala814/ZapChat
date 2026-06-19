using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

    public AuthController(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (existingUser is not null)
        {
            if (existingUser.IsDeleted)
            {
                return BadRequest("This account has been permanently removed and cannot be recreated.");
            }
            return BadRequest("User already exists.");
        }

        var hashedPassword =
            _passwordHasher.HashPassword(request.Password);

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

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id,
            user.Email,
            anonymousProfile.AnonymousName,
            new List<string>());

        var response = new
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            AnonymousName = anonymousProfile.AnonymousName
        };

        return Ok(response);
    }
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _context.Users
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

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool excludeAdmin = false,
        [FromQuery] bool excludeDeleted = false)
    {
        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        var query = _context.Users.AsQueryable();

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

    [HttpGet("users/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var anonymousProfile = await _context.AnonymousProfiles
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
        user.DeletedBy = request.AdminId;

        await _context.SaveChangesAsync();
        _logger.LogInformation("SOFT DELETE - Successfully soft-deleted user {UserId}", id);

        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Load user WITH roles so they can be included in the JWT claim
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user is null)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Check if user is soft deleted
        if (user.IsDeleted)
        {
            return Unauthorized("Your account has been permanently removed.");
        }

        var isPasswordValid =
            _passwordHasher.VerifyPassword(
                request.Password,
                user.PasswordHash);

        if (!isPasswordValid)
        {
            return Unauthorized("Invalid email or password.");
        }

        var anonymousProfile = await _context.AnonymousProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var anonName = anonymousProfile?.AnonymousName ?? "Anonymous";

        // Bootstrap Admin role when the configured admin email logs in.
        // Idempotent: safe to call on every login — creates role/assignment only if missing.
        // Regular users are completely unaffected.
        var adminEmail = _configuration["AdminSettings:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            string.Equals(user.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureAdminRoleAsync(user);
        }

        // Pass actual roles — empty list for regular users, ["Admin"] for admin
        var roles = user.Roles.Select(r => r.Name).ToList();

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id,
            user.Email,
            anonName,
            roles);

        var response = new
        {
            Token = token,
            UserId = user.Id,
            AnonymousName = anonName
        };

        return Ok(response);
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return NotFound();

        var anonymousName = await _context.AnonymousProfiles
            .Where(a => a.UserId == userId)
            .Select(a => a.AnonymousName)
            .FirstOrDefaultAsync() ?? "Anonymous";

        return Ok(new
        {
            userId        = user.Id,
            email         = user.Email,
            fullName      = user.FullName,
            department    = user.Department,
            branch        = user.Branch,
            createdAt     = user.CreatedAt,
            anonymousName = anonymousName,
            roles         = user.Roles.Select(r => r.Name).ToList()
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
            branch     = user.Branch
        });
    }

    /// <summary>
    /// Ensures the "Admin" role exists in the database and is assigned to the given user.
    /// Idempotent — creates the role and/or the assignment only if they do not already exist.
    /// Never called for regular users — only triggered when AdminSettings:AdminEmail matches.
    /// </summary>
    private async Task EnsureAdminRoleAsync(User user)
    {
        // Step 1: Find or create the "Admin" role (no duplicate roles)
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Admin");

        if (adminRole is null)
        {
            adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
            await _context.Roles.AddAsync(adminRole);
        }

        // Step 2: Assign the role to this user if not already assigned
        if (!user.Roles.Any(r => r.Name == "Admin"))
        {
            user.Roles.Add(adminRole);
        }

        // Single SaveChangesAsync covers both role creation and assignment
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Generates a unique anonymous name in PascalCase Adjective+Animal format (e.g., SwiftFox).
    /// Pool: 200 adjectives × 100 animals = 20,000 combinations.
    /// Retries until a name not already in the database is found.
    /// Throws if the pool is exhausted (safety guard — should never happen at current scale).
    /// </summary>
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

        // Shuffle indices for random, non-repeating traversal
        var random = Random.Shared;
        var adjectiveIndices = Enumerable.Range(0, adjectives.Length)
            .OrderBy(_ => random.Next()).ToArray();
        var animalIndices = Enumerable.Range(0, animals.Length)
            .OrderBy(_ => random.Next()).ToArray();

        foreach (var ai in adjectiveIndices)
        {
            foreach (var ni in animalIndices)
            {
                var candidate = adjectives[ai] + animals[ni];
                var exists = await _context.AnonymousProfiles
                    .AnyAsync(x => x.AnonymousName == candidate);

                if (!exists)
                    return candidate;
            }
        }

        // Pool exhausted — should never happen at current scale (20,000 combinations)
        throw new InvalidOperationException(
            "Anonymous name pool exhausted. All 20,000 combinations are taken. " +
            "Please expand the adjective or animal lists.");
    }
}
