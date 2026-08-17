namespace ZapChat.Shared.Errors;

/// <summary>
/// Base for errors that carry an intended HTTP status and a message that is safe
/// to return to the caller. Anything that is NOT one of these becomes a generic
/// 500 with no detail, so internals are never leaked by accident.
/// </summary>
public abstract class ZapChatException : Exception
{
    public abstract int StatusCode { get; }
    public abstract string ErrorCode { get; }

    protected ZapChatException(string message) : base(message) { }
}

/// <summary>400 — the request is malformed or fails a business rule.</summary>
public sealed class ValidationException : ZapChatException
{
    public override int StatusCode => 400;
    public override string ErrorCode => "validation_failed";
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message) => Errors = errors;
}

/// <summary>401 — no valid credentials.</summary>
public sealed class UnauthorizedException : ZapChatException
{
    public override int StatusCode => 401;
    public override string ErrorCode => "unauthorized";
    public UnauthorizedException(string message = "Authentication is required.") : base(message) { }
}

/// <summary>403 — authenticated, but not allowed to touch this resource.</summary>
public sealed class ForbiddenException : ZapChatException
{
    public override int StatusCode => 403;
    public override string ErrorCode => "forbidden";
    public ForbiddenException(string message = "You do not have access to this resource.") : base(message) { }
}

/// <summary>404 — not found, or deliberately indistinguishable from not-found.</summary>
public sealed class NotFoundException : ZapChatException
{
    public override int StatusCode => 404;
    public override string ErrorCode => "not_found";
    public NotFoundException(string message = "The requested resource was not found.") : base(message) { }
}

/// <summary>409 — the request conflicts with current state.</summary>
public sealed class ConflictException : ZapChatException
{
    public override int StatusCode => 409;
    public override string ErrorCode => "conflict";
    public ConflictException(string message) : base(message) { }
}

/// <summary>422 — well-formed but semantically rejected (e.g. blocked by moderation).</summary>
public sealed class RejectedException : ZapChatException
{
    public override int StatusCode => 422;
    public override string ErrorCode => "rejected";
    public string? Category { get; }

    public RejectedException(string message, string? category = null) : base(message)
        => Category = category;
}

/// <summary>
/// 429 — refused because the caller is asking too often.
///
/// Distinct from the gateway's limiter, which partitions by IP and knows nothing about
/// the resource being requested. This is for limits the service itself has to enforce,
/// such as "one verification code per mailbox per minute" — a rule the gateway cannot
/// express because the mailbox is in the request body.
///
/// The error code matches the gateway's, so a client handles both the same way.
/// </summary>
public sealed class RateLimitedException : ZapChatException
{
    public override int StatusCode => 429;
    public override string ErrorCode => "rate_limited";

    /// <summary>Seconds until the caller may retry, surfaced as Retry-After.</summary>
    public int? RetryAfterSeconds { get; }

    public RateLimitedException(string message, int? retryAfterSeconds = null) : base(message)
        => RetryAfterSeconds = retryAfterSeconds;
}

/// <summary>503 — a dependency this operation needs is unavailable.</summary>
public sealed class DependencyUnavailableException : ZapChatException
{
    public override int StatusCode => 503;
    public override string ErrorCode => "dependency_unavailable";
    public DependencyUnavailableException(string message) : base(message) { }
}
