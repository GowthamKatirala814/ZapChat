using Auth.Application.Abstractions;
using Auth.Application.DTOs;
using Auth.Domain.Documents;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Configuration;
using ZapChat.Shared.Errors;

namespace Auth.Infrastructure.Services;

/// <summary>
/// The three-step, email-verified registration flow. No user document exists until
/// step 3 succeeds.
/// </summary>
public sealed class RegistrationService : IRegistrationService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _users;
    private readonly IOtpRepository _otps;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IAnonymousNameService _names;
    private readonly IEmailService _email;
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IUserRepository users,
        IOtpRepository otps,
        IPasswordHasher hasher,
        ITokenService tokens,
        IAnonymousNameService names,
        IEmailService email,
        IHttpClientFactory httpClients,
        ILogger<RegistrationService> logger)
    {
        _users = users;
        _otps = otps;
        _hasher = hasher;
        _tokens = tokens;
        _names = names;
        _email = email;
        _httpClients = httpClients;
        _logger = logger;
    }

    public async Task<StepResult> InitiateAsync(
        InitiateRegistrationRequest request, CancellationToken ct = default)
    {
        if (await _users.EmailExistsAsync(request.Email, ct))
        {
            // The address is a corporate one and registration is self-service, so
            // confirming it exists is not a meaningful disclosure and a vague error
            // here just traps real users.
            throw new ConflictException("An account with that email address already exists.");
        }

        // Supersede any earlier unfinished attempt for this address.
        await _otps.InvalidatePendingAsync(request.Email, OtpPurpose.Registration, ct);

        var code = _tokens.CreateNumericCode();

        await _otps.InsertAsync(new OtpDocument
        {
            Purpose = OtpPurpose.Registration,
            Email = request.Email,
            CodeHash = _tokens.Hash(code),
            ExpiresAt = DateTime.UtcNow.Add(OtpLifetime),
            Pending = new PendingRegistration
            {
                FullName = request.FullName.Trim(),
                Department = request.Department.Trim(),
                Branch = request.Branch.Trim()
            }
        }, ct);

        try
        {
            await _email.SendRegistrationOtpAsync(request.Email, code, request.FullName);
        }
        catch (Exception ex)
        {
            // Surfaced rather than swallowed: if the mail never leaves, the user is
            // stuck on a screen waiting for a code that will not arrive.
            _logger.LogError(ex, "Failed to send the registration code to {Email}.", request.Email);
            throw new DependencyUnavailableException(
                "We could not send the verification email. Please try again shortly.");
        }

        return new StepResult(true, "A 6-digit verification code has been sent to your email.");
    }

    public async Task<StepResult> VerifyOtpAsync(
        VerifyOtpRequest request, CancellationToken ct = default)
    {
        var otp = await _otps.GetLatestAsync(request.Email, OtpPurpose.Registration, ct);

        if (otp is null || !otp.IsUsable(DateTime.UtcNow))
            throw new ValidationException("That code is invalid or has expired. Request a new one.");

        // Count the attempt before comparing, so a wrong guess always costs an attempt.
        if (!await _otps.IncrementAttemptsAsync(otp.Id, ct))
        {
            await _otps.ConsumeAsync(otp.Id, ct);
            throw new ValidationException(
                "Too many incorrect attempts. Request a new verification code.");
        }

        if (!FixedTimeEquals(otp.CodeHash, _tokens.Hash(request.OtpCode)))
            throw new ValidationException("That code is incorrect.");

        var followUp = _tokens.CreateOpaqueToken();
        await _otps.MarkVerifiedAsync(otp.Id, _tokens.Hash(followUp), ct);

        return new StepResult(true, "Email verified. Set a password to finish.", followUp);
    }

    public async Task<StepResult> CompleteAsync(
        CompleteRegistrationRequest request, CancellationToken ct = default)
    {
        if (request.Password != request.ConfirmPassword)
            throw new ValidationException("The passwords do not match.");

        var otp = await _otps.GetByFollowUpTokenAsync(_tokens.Hash(request.VerificationToken), ct);

        if (otp is null || otp.Pending is null || !otp.IsUsable(DateTime.UtcNow))
            throw new ValidationException("This registration link is invalid or has expired.");

        // Consume first. If two requests arrive with the same token, only one wins,
        // so a duplicate submit cannot create two accounts.
        if (!await _otps.ConsumeAsync(otp.Id, ct))
            throw new ConflictException("This registration has already been completed.");

        if (await _users.EmailExistsAsync(otp.Email, ct))
            throw new ConflictException("An account with that email address already exists.");

        var user = new UserDocument
        {
            Id = Guid.NewGuid(),
            Email = otp.Email,
            FullName = otp.Pending.FullName,
            Department = otp.Pending.Department,
            Branch = otp.Pending.Branch,
            PasswordHash = _hasher.HashPassword(request.Password),
            Anonymous = new AnonymousIdentity
            {
                Name = await _names.AllocateAsync(ct),
                AssignedAt = DateTime.UtcNow
            },
            Roles = [ZapChatRoles.User],
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _users.InsertAsync(user, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            // The unique indexes on emailNormalized and anonymous.name are the real
            // guarantee; this converts the race into a clean 409.
            throw new ConflictException(
                "That account could not be created because it already exists. Try signing in.");
        }

        _logger.LogInformation(
            "Registered user {UserId} as {AnonymousName}.", user.Id, user.Anonymous.Name);

        await JoinDefaultRoomsAsync(user.Id, ct);

        return new StepResult(true, "Your account has been created. You can sign in now.");
    }

    /// <summary>
    /// Adds the new account to the default rooms. Chat owns room membership, so this
    /// is a call into Chat — and it carries a service token, which is why it works
    /// where the old unauthenticated call to Admin silently 401'd.
    /// </summary>
    private async Task JoinDefaultRoomsAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var client = _httpClients.CreateClient(ServiceClients.Chat);
            if (client.BaseAddress is null)
            {
                _logger.LogWarning(
                    "ServiceUrls:ChatService is not configured; user {UserId} was not added to default rooms.",
                    userId);
                return;
            }

            var response = await client.PostAsync(
                $"api/rooms/internal/join-defaults/{userId}", content: null, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Chat service returned {Status} when adding user {UserId} to default rooms.",
                    response.StatusCode, userId);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal: the account exists and the user can join rooms by opening them.
            _logger.LogError(ex, "Failed to add user {UserId} to default rooms.", userId);
        }
    }

    private static bool FixedTimeEquals(string a, string b) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a),
            System.Text.Encoding.UTF8.GetBytes(b));
}
