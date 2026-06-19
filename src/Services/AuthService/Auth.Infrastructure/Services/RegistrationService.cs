using System.Net.Http;
using System.Security.Cryptography;
using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Services;

public class RegistrationService : IRegistrationService
{
    private readonly AuthDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        AuthDbContext context,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        IHttpClientFactory httpClientFactory,
        ILogger<RegistrationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ── Step 1 ───────────────────────────────────────────────────────────────

    public async Task<InitiateRegistrationResponseDto> InitiateRegistrationAsync(
        InitiateRegistrationRequestDto dto)
    {
        // Check if email already exists in Users table
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser is not null)
        {
            if (existingUser.IsDeleted)
            {
                return new InitiateRegistrationResponseDto
                {
                    Success = false,
                    Message = "This account has been permanently removed and cannot be recreated."
                };
            }

            return new InitiateRegistrationResponseDto
            {
                Success = false,
                Message = "An account with this email already exists."
            };
        }

        // Check if there is already a verified pending registration for this email
        var verifiedPending = await _context.RegistrationOtps
            .FirstOrDefaultAsync(o => o.Email == dto.Email && o.IsVerified);

        if (verifiedPending is not null)
        {
            return new InitiateRegistrationResponseDto
            {
                Success = false,
                Message = "Email already verified. Please complete your registration by setting a password."
            };
        }

        // Delete any existing unverified RegistrationOtp records for this email (cleanup old attempts)
        var oldOtps = await _context.RegistrationOtps
            .Where(o => o.Email == dto.Email && !o.IsVerified)
            .ToListAsync();

        if (oldOtps.Any())
        {
            _context.RegistrationOtps.RemoveRange(oldOtps);
        }

        // Generate 6-digit OTP (padded with leading zeros: "D6" format)
        var otpCode = Random.Shared.Next(0, 999999).ToString("D6");

        var otp = new RegistrationOtp
        {
            Id                = Guid.NewGuid(),
            Email             = dto.Email,
            FullName          = dto.FullName,
            Department        = dto.Department,
            Branch            = dto.Branch,
            OtpCode           = otpCode,
            CreatedAt         = DateTime.UtcNow,
            ExpiresAt         = DateTime.UtcNow.AddMinutes(10),
            IsVerified        = false,
            VerificationToken = null
        };

        _context.RegistrationOtps.Add(otp);
        await _context.SaveChangesAsync();

        // Send OTP email
        await _emailService.SendRegistrationOtpEmailAsync(dto.Email, otpCode, dto.FullName);

        return new InitiateRegistrationResponseDto
        {
            Success = true,
            Message = "Verification code sent to your email."
        };
    }

    // ── Step 2 ───────────────────────────────────────────────────────────────

    public async Task<VerifyRegistrationOtpResponseDto> VerifyRegistrationOtpAsync(
        VerifyRegistrationOtpRequestDto dto)
    {
        // Find a valid, unverified, unexpired OTP record with matching code
        var otp = await _context.RegistrationOtps
            .FirstOrDefaultAsync(o =>
                o.Email      == dto.Email   &&
                o.OtpCode    == dto.OtpCode &&
                !o.IsVerified               &&
                o.ExpiresAt  > DateTime.UtcNow);

        if (otp is null)
        {
            return new VerifyRegistrationOtpResponseDto
            {
                Success = false,
                Message = "Invalid or expired verification code."
            };
        }

        // Generate one-time verification token
        var verificationToken = Guid.NewGuid().ToString("N");

        otp.IsVerified        = true;
        otp.VerificationToken = verificationToken;

        await _context.SaveChangesAsync();

        return new VerifyRegistrationOtpResponseDto
        {
            Success           = true,
            Message           = "Email verified successfully.",
            VerificationToken = verificationToken
        };
    }

    // ── Step 3 ───────────────────────────────────────────────────────────────

    public async Task<CompleteRegistrationResponseDto> CompleteRegistrationAsync(
        CompleteRegistrationRequestDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            return new CompleteRegistrationResponseDto
            {
                Success = false,
                Message = "Passwords do not match."
            };
        }

        // Allow up to 30 minutes after OTP expiry for the user to set their password
        var cutoff = DateTime.UtcNow.AddMinutes(-30);

        var otp = await _context.RegistrationOtps
            .FirstOrDefaultAsync(o =>
                o.VerificationToken == dto.VerificationToken &&
                o.IsVerified                                  &&
                o.ExpiresAt         > cutoff);

        if (otp is null)
        {
            return new CompleteRegistrationResponseDto
            {
                Success = false,
                Message = "Session expired. Please register again."
            };
        }

        // Edge case: check if email was registered by someone else in the window
        var emailTaken = await _context.Users
            .AnyAsync(u => u.Email == otp.Email);

        if (emailTaken)
        {
            // Clean up the pending OTP as it can never be completed
            _context.RegistrationOtps.Remove(otp);
            await _context.SaveChangesAsync();

            return new CompleteRegistrationResponseDto
            {
                Success = false,
                Message = "An account with this email already exists."
            };
        }

        // Create User exactly the same way the existing Register endpoint does
        var hashedPassword = _passwordHasher.HashPassword(dto.Password);

        var user = new User
        {
            Id           = Guid.NewGuid(),
            FullName     = otp.FullName,
            Email        = otp.Email,
            PasswordHash = hashedPassword,
            Department   = otp.Department,
            Branch       = otp.Branch
        };

        // Create AnonymousProfile exactly the same way as the existing Register endpoint
        var anonymousProfile = new AnonymousProfile
        {
            Id            = Guid.NewGuid(),
            UserId        = user.Id,
            AnonymousName = await GenerateUniqueAnonymousNameAsync()
        };

        _context.Users.Add(user);
        _context.AnonymousProfiles.Add(anonymousProfile);

        // Delete the RegistrationOtp — no longer needed
        _context.RegistrationOtps.Remove(otp);

        await _context.SaveChangesAsync();

        // Inform AdminService to sync this new user into default rooms
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"http://localhost:5002/api/admin/rooms/sync-user/{user.Id}", null);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to sync new user {UserId} to Admin rooms. Status: {Status}", user.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing new user {UserId} to Admin rooms.", user.Id);
        }

        return new CompleteRegistrationResponseDto
        {
            Success = true,
            Message = "Account created successfully. You can now login."
        };
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Generates a unique anonymous name in PascalCase Adjective+Animal format (e.g., SwiftFox).
    /// Duplicated from AuthController so that RegistrationService is self-contained.
    /// Pool: 200 adjectives × 100 animals = 20,000 combinations.
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

        var random          = Random.Shared;
        var adjectiveIndices = Enumerable.Range(0, adjectives.Length).OrderBy(_ => random.Next()).ToArray();
        var animalIndices    = Enumerable.Range(0, animals.Length).OrderBy(_ => random.Next()).ToArray();

        foreach (var ai in adjectiveIndices)
        {
            foreach (var ni in animalIndices)
            {
                var candidate = adjectives[ai] + animals[ni];
                var exists    = await _context.AnonymousProfiles.AnyAsync(x => x.AnonymousName == candidate);
                if (!exists)
                    return candidate;
            }
        }

        throw new InvalidOperationException(
            "Anonymous name pool exhausted. All 20,000 combinations are taken. " +
            "Please expand the adjective or animal lists.");
    }
}
