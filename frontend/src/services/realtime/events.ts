import type {
  AppNotification, DeletionKind, DirectMessage, LastMessage, Message, Poll, Reaction, RoomMember,
} from "../../types/api";

/**
 * Server → client event names and payload shapes.
 *
 * Mirrors `ZapChat.Shared.Realtime.HubEvents` on the backend one-for-one. There was no
 * shared definition before, which is how the backend came to emit
 * `RoomMessageRead { roomName, userId, lastReadAt }` while the React handler read
 * `data.messageId` — read receipts silently never worked, and nothing could catch it.
 */
export const HubEvent = {
  // Room chat
  MessageReceived: "ReceiveMessage",
  MessageEdited: "MessageEdited",
  MessageDeleted: "MessageDeleted",
  MessageBlocked: "MessageBlocked",
  ReactionsChanged: "ReactionsChanged",
  UserTyping: "UserTyping",
  UserStoppedTyping: "UserStoppedTyping",
  RoomPresenceChanged: "RoomPresenceChanged",
  UserJoined: "UserJoined",
  UserLeft: "UserLeft",
  RoomUpdated: "RoomUpdated",
  RoomRead: "RoomRead",

  // Direct messages
  PrivateMessageReceived: "ReceivePrivateMessage",
  PrivateMessageBlocked: "PrivateMessageBlocked",
  ConversationUpdated: "ConversationUpdated",
  PrivateMessageRead: "MessageRead",

  // Polls
  PollCreated: "PollCreated",
  PollUpdated: "PollUpdated",
  PollClosed: "PollClosed",
  PollDeleted: "PollDeleted",

  // Notifications
  NotificationReceived: "ReceiveNotification",
  NotificationDeleted: "NotificationDeleted",
} as const;

// ── Payloads ──────────────────────────────────────────────────────────────────

/**
 * A broadcast message. `isMine` is always false here: one payload goes to a whole
 * group, so it cannot carry a correct per-recipient value. A client identifies its own
 * message from the value returned by the send call.
 */
export type MessageReceived = Message;
export type MessageEdited = Message;

export interface MessageDeleted {
  roomId?: string;
  conversationId?: string;
  messageId: string;
  /** "User" or "Moderation" — the UI shows a different placeholder for each. */
  deletedBy: DeletionKind;
  deletedAt: string;
}

export interface ReactionsChanged {
  roomId?: string;
  conversationId?: string;
  messageId: string;
  /** The full resulting list, never a delta the client must apply itself. */
  reactions: Reaction[];
}

export interface TypingEvent {
  /** Carried so a client in several rooms routes the indicator correctly. */
  roomId?: string;
  conversationId?: string;
  anonymousName: string;
}

export interface RoomPresenceChanged {
  roomId: string;
  members: RoomMember[];
}

export interface UserJoinedOrLeft {
  roomId: string;
  anonymousName: string;
}

/** Per-user sidebar update carrying the server's authoritative unread count. */
export interface RoomUpdated {
  roomId: string;
  roomName: string;
  unreadCount: number;
  lastMessage?: LastMessage;
}

export interface RoomRead {
  roomId: string;
  anonymousName: string;
  readAt: string;
}

export type PrivateMessageReceived = DirectMessage;

export interface ConversationUpdated {
  conversationId: string;
  unreadCount: number;
  lastMessage?: {
    messageId: string;
    preview: string;
    senderName: string;
    sentByMe: boolean;
    sentAt: string;
  };
}

/** Tells the sender their messages were read so the tick can update. */
export interface PrivateMessageRead {
  conversationId: string;
  messageIds: string[];
  readAt: string;
}

/** Sent only to the author when moderation rejects their content. */
export interface MessageBlocked {
  category: string;
  reason: string;
}

export type PollEvent = Poll;

export interface PollClosedOrDeleted {
  pollId: string;
}

export type NotificationReceived = AppNotification;

export interface NotificationDeleted {
  id: string;
}

/**
 * Maps each hub's events to their payloads, so `on()` infers the handler argument.
 *
 * Keyed by hub rather than being one flat table, because four event names — MessageEdited,
 * MessageDeleted, ReactionsChanged and the typing pair — are emitted by both the chat and
 * private-chat hubs with *different* payloads. A flat map would type a direct-message
 * edit as a room `Message`, which is exactly the class of mismatch this file exists to
 * prevent.
 */
export interface HubPayloadMap {
  chat: {
    [HubEvent.MessageReceived]: MessageReceived;
    [HubEvent.MessageEdited]: MessageEdited;
    [HubEvent.MessageDeleted]: MessageDeleted;
    [HubEvent.MessageBlocked]: MessageBlocked;
    [HubEvent.ReactionsChanged]: ReactionsChanged;
    [HubEvent.UserTyping]: TypingEvent;
    [HubEvent.UserStoppedTyping]: TypingEvent;
    [HubEvent.RoomPresenceChanged]: RoomPresenceChanged;
    [HubEvent.UserJoined]: UserJoinedOrLeft;
    [HubEvent.UserLeft]: UserJoinedOrLeft;
    [HubEvent.RoomUpdated]: RoomUpdated;
    [HubEvent.RoomRead]: RoomRead;
  };

  privateChat: {
    [HubEvent.PrivateMessageReceived]: PrivateMessageReceived;
    /** A DirectMessage, not a room Message — same event name, different shape. */
    [HubEvent.MessageEdited]: DirectMessage;
    [HubEvent.MessageDeleted]: MessageDeleted;
    [HubEvent.ReactionsChanged]: ReactionsChanged;
    [HubEvent.PrivateMessageBlocked]: MessageBlocked;
    [HubEvent.ConversationUpdated]: ConversationUpdated;
    [HubEvent.PrivateMessageRead]: PrivateMessageRead;
    [HubEvent.UserTyping]: TypingEvent;
    [HubEvent.UserStoppedTyping]: TypingEvent;
  };

  polls: {
    [HubEvent.PollCreated]: PollEvent;
    [HubEvent.PollUpdated]: PollEvent;
    [HubEvent.PollClosed]: PollClosedOrDeleted;
    [HubEvent.PollDeleted]: PollClosedOrDeleted;
  };

  notifications: {
    [HubEvent.NotificationReceived]: NotificationReceived;
    [HubEvent.NotificationDeleted]: NotificationDeleted;
  };
}
