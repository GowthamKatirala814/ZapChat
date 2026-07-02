# ZapChat Messaging Services Architecture

This document provides a comprehensive overview of how the messaging systems work in the ZapChat application. The messaging architecture is split into two distinct microservices to ensure separation of concerns, scalability, and specialized logic for different types of communication.

---

## 1. High-Level Overview

ZapChat utilizes two separate microservices for handling real-time communication:

1. **ChatService (Room Chat)**: Handles public/group communication where multiple users join a specific topic or "Room".
2. **PrivateChatService (1-on-1 Chat)**: Handles direct, private conversations between two specific users.

Both services are built using:
* **ASP.NET Core Web API** for HTTP endpoints (REST).
* **SignalR** for real-time, bidirectional WebSocket communication.
* **Entity Framework Core (SQL Server)** for data persistence.
* **JWT Authentication** for securing hubs and endpoints.

All client requests (HTTP and WebSockets) route through the **Gateway.API**, which acts as a reverse proxy.

---

## 2. ChatService (Room Chat)

### Core Concepts
* **ChatRooms**: The central entity. Users do not chat globally; they must join a specific `ChatRoom`.
* **Anonymity**: Instead of strict User IDs, the Chat Service relies heavily on `AnonymousName` claims from the JWT token.
* **Broadcasting**: Messages are sent to SignalR **Groups**, where the group name corresponds to the Room name.

### Message Flow
1. **Connection**: A user's browser connects to `wss://[gateway]/hubs/chat` using their JWT token.
2. **Joining a Room**: The client invokes the `JoinRoom(roomName)` method on the SignalR `ChatHub`. The hub adds the connection to a SignalR Group named after the room.
3. **Sending a Message**: The client invokes `SendMessage(roomId, content)`.
4. **Persistence**: The hub saves the message to the `ChatDbContext` (SQL Server).
5. **Real-time Broadcast**: The hub broadcasts `ReceiveMessage` to all clients currently connected to that Room's Group.

---

## 3. PrivateChatService (1-on-1 Chat)

### Core Concepts
* **Conversations**: A unique mapping between two users (`User1Id` and `User2Id`).
* **Direct Targeting**: Instead of broadcasting to a room, messages are routed directly to specific **User IDs** using SignalR's built-in User mapping.
* **Read Receipts**: Unlike Room Chat, Private Chat tracks whether a message has been read (`IsRead` flag).

### Message Flow
1. **Connection**: A user connects to `wss://[gateway]/hubs/privatechat`. SignalR automatically maps the connection to their `NameIdentifier` (User ID) from the JWT token.
2. **Initiating Chat**: When a user clicks on a profile to chat, the frontend calls the REST API `POST /api/privatechat/conversation` to fetch or create a unique `ConversationId` for the two users.
3. **Sending a Message**: The client invokes `SendPrivateMessage(conversationId, receiverId, content)` on the `PrivateChatHub`.
4. **Persistence**: The message is saved to `PrivateChatDbContext` with the `SenderId`.
5. **Real-time Broadcast**: The hub calls `Clients.User(receiverId).SendAsync("ReceivePrivateMessage", ...)` to push the message directly to the recipient, and also sends a confirmation back to the sender.

---

## 4. Shared Mechanics (Moderation & Deletion)

To maintain a safe environment, both services share identical moderation and deletion architectures.

### Message Deletion (User Action)
When a user deletes their own message:
1. They call `DELETE /api/messages/{id}` (or private equivalent).
2. The backend verifies ownership, sets `IsDeleted = true`, and sets `DeletedBy = "User"`.
3. A SignalR `MessageDeleted` event is fired to all relevant clients.
4. The React frontend catches this event and replaces the message UI with a placeholder: *🗑️ You deleted this message.*

### Message Removal (Moderation Action)
ZapChat has an **AdminService** and automated moderation systems that monitor chat health.
1. If a message violates rules, a request is sent to `POST /api/moderation/auto-remove`.
2. The backend sets `IsRemoved = true` and `DeletedBy = "Moderation"`.
3. A SignalR `MessageDeleted` event is instantly fired with `deletedBy: "Moderation"`.
4. The React frontend instantly updates the UI to show: *🛡️ Message removed by moderation.*

### Reporting
Users can report toxic messages. The `ReportMessage` endpoint acts as a proxy, gathering the message context and forwarding it to the **AdminService** via an internal HTTP Client request.

---

## 5. Technology Stack summary
* **Controllers**: Handle HTTP GET requests for message history, RESTful deletions, and moderation webhooks.
* **Hubs**: Handle real-time WebSocket traffic (Sending messages, Typing indicators).
* **DTOs**: Data Transfer Objects used to sanitize what is sent to the frontend (e.g., hiding raw database IDs where unnecessary).
* **EF Core**: Code-first migrations manage the schema.

---

*This document was auto-generated to provide architectural clarity for ZapChat.*
