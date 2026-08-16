using MongoDB.Driver;
using PrivateChat.Domain.Documents;
using ZapChat.Shared.Mongo;

namespace PrivateChat.Infrastructure.Persistence;

public sealed class PrivateChatMongoContext
{
    public const string Conversations = "conversations";
    public const string DirectMessages = "directMessages";
    public const string UserBlocks = "userBlocks";
    public const string ModerationEvents = "moderationEvents";

    private readonly IMongoDatabase _database;

    public PrivateChatMongoContext(IMongoDatabase database) => _database = database;

    public IMongoCollection<ConversationDocument> ConversationsCollection =>
        _database.GetCollection<ConversationDocument>(Conversations);

    public IMongoCollection<DirectMessageDocument> MessagesCollection =>
        _database.GetCollection<DirectMessageDocument>(DirectMessages);

    public IMongoCollection<UserBlockDocument> BlocksCollection =>
        _database.GetCollection<UserBlockDocument>(UserBlocks);

    public IMongoCollection<ModerationEventDocument> ModerationEventsCollection =>
        _database.GetCollection<ModerationEventDocument>(ModerationEvents);
}

public sealed class PrivateChatIndexes : IMongoIndexProvider
{
    public async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct)
    {
        var conversations =
            database.GetCollection<ConversationDocument>(PrivateChatMongoContext.Conversations);

        await MongoIndex.EnsureAsync(conversations,
        [
            // Makes a duplicate conversation for the same pair impossible.
            MongoIndex.Asc<ConversationDocument>(
                c => c.ParticipantKey, "ux_participantKey", unique: true),

            // "my conversations, newest first" — one indexed read for the whole list.
            MongoIndex.Compound<ConversationDocument>(
                Builders<ConversationDocument>.IndexKeys
                    .Ascending("participants.userId")
                    .Descending("lastMessage.sentAt"),
                "ix_participant_lastMessage"),
        ], ct);

        var messages =
            database.GetCollection<DirectMessageDocument>(PrivateChatMongoContext.DirectMessages);

        await MongoIndex.EnsureAsync(messages,
        [
            MongoIndex.Compound<DirectMessageDocument>(
                Builders<DirectMessageDocument>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Descending(m => m.SentAt)
                    .Descending(m => m.Id),
                "ix_conversation_sentAt_id"),

            MongoIndex.Asc<DirectMessageDocument>(m => m.Sender.UserId, "ix_sender"),
            MongoIndex.Desc<DirectMessageDocument>(m => m.SentAt, "ix_sentAt_desc"),
        ], ct);

        var blocks = database.GetCollection<UserBlockDocument>(PrivateChatMongoContext.UserBlocks);

        await MongoIndex.EnsureAsync(blocks,
        [
            MongoIndex.Compound<UserBlockDocument>(
                Builders<UserBlockDocument>.IndexKeys
                    .Ascending(b => b.BlockerId)
                    .Ascending(b => b.BlockedId),
                "ux_blocker_blocked", unique: true),

            MongoIndex.Asc<UserBlockDocument>(b => b.BlockedId, "ix_blockedId"),
        ], ct);

        var events = database
            .GetCollection<ModerationEventDocument>(PrivateChatMongoContext.ModerationEvents);

        await MongoIndex.EnsureAsync(events,
        [
            MongoIndex.Desc<ModerationEventDocument>(e => e.Timestamp, "ix_timestamp_desc"),
            MongoIndex.Asc<ModerationEventDocument>(e => e.UserId, "ix_userId"),
            MongoIndex.Asc<ModerationEventDocument>(e => e.Category, "ix_category"),
        ], ct);
    }
}
