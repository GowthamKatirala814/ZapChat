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

    public AuthController(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (existingUser is not null)
        {
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
            AnonymousName = GenerateAnonymousName()
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
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(x => new
            {
                x.Id,
                AnonymousName = _context.AnonymousProfiles
                    .Where(a => a.UserId == x.Id)
                    .Select(a => a.AnonymousName)
                    .FirstOrDefault() ?? "Anonymous"
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user is null)
        {
            return Unauthorized("Invalid email or password.");
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

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id,
            user.Email,
            anonName,
            new List<string>());

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
    public IActionResult Me()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var email =
            User.FindFirstValue(ClaimTypes.Email);

        var roles =
            User.FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList();

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Roles = roles
        });
    }

    private string GenerateAnonymousName()
    {
        var adjectives = new[]
        {
            "Shadow",
            "Silent",
            "Dark",
            "Swift",
            "Hidden"
        };

        var animals = new[]
        {
            "Tiger",
            "Fox",
            "Wolf",
            "Eagle",
            "Panther"
        };
//creating anonymous Profiles by mapping adjectives and animals 
//Creating anonymous Profiles by mapping them to any disney characters 
//Users can choose their own profile names   


        var random = new Random();

        return adjectives[random.Next(adjectives.Length)] +
               animals[random.Next(animals.Length)] +
               random.Next(100, 999);
    }
}