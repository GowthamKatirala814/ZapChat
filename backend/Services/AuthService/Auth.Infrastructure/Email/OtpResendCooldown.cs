using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Email;

/// <summary>
/// How soon a mailbox may be sent another one-time code.
///
/// The gateway already limits requests per IP, which stops one client hammering the
/// endpoint. It cannot stop the other shape of abuse: many clients, or one client behind
/// changing addresses, all requesting codes for the *same* victim mailbox until it is
/// unusable. That limit has to be keyed on the address, which only the service can see,
/// so it lives here.
///
/// Derived from the stored OTP's own timestamp rather than an in-memory counter, so it
/// survives a restart and holds across every instance of the service.
/// </summary>
public sealed class OtpResendCooldown
{
    private readonly TimeSpan _window;

    public OtpResendCooldown(IOptions<EmailOptions> options)
        => _window = TimeSpan.FromSeconds(Math.Max(0, options.Value.ResendCooldownSeconds));

    public TimeSpan Window => _window;

    /// <summary>True when the previous code for this address is too recent.</summary>
    public bool IsTooSoon(DateTime lastSentUtc) =>
        _window > TimeSpan.Zero && DateTime.UtcNow - lastSentUtc < _window;

    /// <summary>Whole seconds until another code may be requested. At least one.</summary>
    public int RetryAfterSeconds(DateTime lastSentUtc)
    {
        var remaining = _window - (DateTime.UtcNow - lastSentUtc);
        return remaining <= TimeSpan.Zero ? 1 : (int)Math.Ceiling(remaining.TotalSeconds);
    }
}
