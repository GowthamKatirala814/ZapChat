using MongoDB.Bson.Serialization.Attributes;

namespace Auth.Domain.Documents;

/// <summary>
/// The user aggregate — collection "users".
///
/// Three former SQL tables collapse into this one document, because all three are
/// 1:1 or bounded and are always read together with the user:
///
///   Users              -> the root
///   AnonymousProfiles  -> <see cref="Anonymous"/>   (was a 1:1 table + join)
///   RoleUser + Roles   -> <see cref="Roles"/>       (was a many-to-many join table)
///
/// A login previously needed three queries plus an Include; it is now a single
/// indexed document read.
/// </summary>
public sealed class UserDocument
{
    [BsonId]
    public Guid Id { get; set; }

    /// <summary>Address as the user typed it. Never returned to other users.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Lower-cased email — the field the unique index and every lookup uses.</summary>
    public string EmailNormalized { get; set; } = string.Empty;

    /// <summary>Real name. Never returned to other users.</summary>
    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    /// <summary>Office branch. Gates branch-room access, so it is admin-managed.</summary>
    public string Branch { get; set; } = string.Empty;

    /// <summary>The identity other users see. Embedded: exactly one per user.</summary>
    public AnonymousIdentity Anonymous { get; set; } = new();

    /// <summary>Role names. Bounded and always needed when issuing a token.</summary>
    public List<string> Roles { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public string? DeletionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Login throttling state. Embedded because it changes with the user.</summary>
    public LoginSecurity Security { get; set; } = new();

    public bool CanSignIn => IsActive && !IsDeleted;
}

public sealed class AnonymousIdentity
{
    /// <summary>Adjective+Animal, unique across the platform.</summary>
    public string Name { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Previous names, so historic messages can still be attributed.</summary>
    public List<string> PreviousNames { get; set; } = [];
}

public sealed class LoginSecurity
{
    public int FailedAttempts { get; set; }
    public DateTime? LastFailedAt { get; set; }

    /// <summary>Set when too many failures occur. Login is refused until this passes.</summary>
    public DateTime? LockedUntil { get; set; }

    public bool IsLockedOut => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
}
