namespace ZapChat.Shared.Realtime;

/// <summary>
/// Canonical names of every server -> client SignalR event.
///
/// These exist because the old code had a backend sending
/// RoomMessageRead { roomName, userId, lastReadAt } while the React handler read
/// data.messageId — a mismatch nothing could catch. Names live here once, and the
/// matching TypeScript constants are generated from this file's contract in
/// frontend/src/shared/realtime/events.ts.
/// </summary>
public static class HubEvents
{
    // ── Room chat ────────────────────────────────────────────────────────────
    public const string MessageReceived = "ReceiveMessage";
    public const string MessageEdited = "MessageEdited";
    public const string MessageDeleted = "MessageDeleted";
    public const string MessageBlocked = "MessageBlocked";

    /// <summary>Full reaction list for a message after a server-side toggle.</summary>
    public const string ReactionsChanged = "ReactionsChanged";

    public const string UserTyping = "UserTyping";
    public const string UserStoppedTyping = "UserStoppedTyping";
    public const string RoomPresenceChanged = "RoomPresenceChanged";
    public const string UserJoined = "UserJoined";
    public const string UserLeft = "UserLeft";

    /// <summary>Per-user unread + preview update for the sidebar.</summary>
    public const string RoomUpdated = "RoomUpdated";

    /// <summary>Another member read up to a point in time.</summary>
    public const string RoomRead = "RoomRead";

    // ── Direct messages ──────────────────────────────────────────────────────
    public const string PrivateMessageReceived = "ReceivePrivateMessage";
    public const string PrivateMessageBlocked = "PrivateMessageBlocked";
    public const string ConversationUpdated = "ConversationUpdated";
    public const string PrivateMessageRead = "MessageRead";

    // ── Polls ────────────────────────────────────────────────────────────────
    public const string PollCreated = "PollCreated";
    public const string PollUpdated = "PollUpdated";
    public const string PollClosed = "PollClosed";
    public const string PollDeleted = "PollDeleted";

    // ── Notifications ────────────────────────────────────────────────────────
    public const string NotificationReceived = "ReceiveNotification";
    public const string NotificationDeleted = "NotificationDeleted";
}

/// <summary>SignalR group naming, so a typo cannot silently route to nowhere.</summary>
public static class HubGroups
{
    /// <summary>
    /// Rooms are grouped by id, not by name. Renaming a room previously orphaned
    /// every connection in its group.
    /// </summary>
    public static string Room(Guid roomId) => $"room:{roomId}";

    public static string Conversation(Guid conversationId) => $"conv:{conversationId}";
}
