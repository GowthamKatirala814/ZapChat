namespace ZapChat.Shared.Realtime;

/// <summary>
/// The reactions the platform accepts, defined exactly once.
///
/// This list previously existed as two private copies — one in Chat's MessageService, one
/// in PrivateChat's ConversationService — plus a third, different hardcoded list in the
/// React reaction picker. The picker offered two emoji the services rejected, so two of
/// its six buttons failed with "That is not an available reaction", and omitted four the
/// services did accept.
///
/// Both services now validate against this, and the API publishes it so the client
/// renders whatever is here rather than its own guess. Adding a reaction is a one-line
/// change with no way to forget the other places.
/// </summary>
public static class ReactionCatalogue
{
    /// <summary>One entry per available reaction.</summary>
    public sealed record Reaction(string Emoji, string Name, string Label);

    /// <summary>
    /// Order is the order the picker shows them in, so it is deliberate: the reactions
    /// people reach for most often come first.
    ///
    /// `Name` is a stable ASCII identifier. It exists so the value can be logged,
    /// analysed and asserted in tests without depending on emoji surviving a terminal,
    /// a log file or a shell argument intact — which, in this codebase, they frequently
    /// do not.
    /// </summary>
    public static readonly IReadOnlyList<Reaction> All =
    [
        new("\U0001F44D", "thumbs_up", "Thumbs up"),
        new("\u2764\uFE0F", "heart", "Heart"),
        new("\U0001F602", "joy", "Laughing"),
        new("\U0001F389", "party", "Celebrate"),
        new("\U0001F525", "fire", "Fire"),
        new("\U0001F62E", "surprised", "Surprised"),
        new("\U0001F622", "sad", "Sad"),
        new("\U0001F64F", "thanks", "Thank you"),
    ];

    private static readonly HashSet<string> Allowed = All.Select(r => r.Emoji).ToHashSet();

    /// <summary>
    /// Whether this emoji may be stored as a reaction.
    ///
    /// The allowlist is what stops the reactions array becoming a dumping ground for
    /// arbitrary strings: the field is client-supplied, and without this a caller could
    /// store any 8 characters against any message.
    /// </summary>
    public static bool IsAllowed(string emoji) => Allowed.Contains(emoji);
}
