using Chat.Domain.Documents;
using MongoDB.Driver;

namespace Chat.Infrastructure.Persistence;

public sealed class ChatMongoContext
{
    public const string Rooms = "rooms";
    public const string RoomMembers = "roomMembers";
    public const string Messages = "messages";
    public const string ModerationEvents = "moderationEvents";
    public const string Files = "files";
    public const string Presence = "presence";

    private readonly IMongoDatabase _database;

    public ChatMongoContext(IMongoDatabase database) => _database = database;

    public IMongoCollection<RoomDocument> RoomsCollection =>
        _database.GetCollection<RoomDocument>(Rooms);

    public IMongoCollection<RoomMemberDocument> MembersCollection =>
        _database.GetCollection<RoomMemberDocument>(RoomMembers);

    public IMongoCollection<MessageDocument> MessagesCollection =>
        _database.GetCollection<MessageDocument>(Messages);

    public IMongoCollection<ModerationEventDocument> ModerationEventsCollection =>
        _database.GetCollection<ModerationEventDocument>(ModerationEvents);

    public IMongoCollection<FileDocument> FilesCollection =>
        _database.GetCollection<FileDocument>(Files);

    public IMongoCollection<PresenceDocument> PresenceCollection =>
        _database.GetCollection<PresenceDocument>(Presence);
}
