namespace ZapChat.Shared.Auth;

/// <summary>
/// The claim names ZapChat issues. Referenced by name in exactly one place so a
/// rename cannot silently break a consumer.
/// </summary>
public static class ZapChatClaims
{
    /// <summary>Anonymous display name. Safe to show to other users.</summary>
    public const string AnonymousName = "anon_name";

    /// <summary>Office branch. Gates access to branch rooms.</summary>
    public const string Branch = "branch";

    /// <summary>Department. Informational.</summary>
    public const string Department = "department";
}

public static class ZapChatRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class ZapChatPolicies
{
    /// <summary>Requires the Admin role. Every admin endpoint uses this.</summary>
    public const string AdminOnly = "AdminOnly";
}
