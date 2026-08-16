using Chat.Domain.Documents;
using MongoDB.Driver;
using ZapChat.Shared.Mongo;

namespace Chat.Infrastructure.Persistence;

public sealed class ChatIndexes : IMongoIndexProvider
{
    public async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken ct)
    {
        var rooms = database.GetCollection<RoomDocument>(ChatMongoContext.Rooms);
        await MongoIndex.EnsureAsync(rooms,
        [
            MongoIndex.Asc<RoomDocument>(r => r.Slug, "ux_slug", unique: true),
            MongoIndex.Asc<RoomDocument>(r => r.IsArchived, "ix_isArchived"),
            MongoIndex.Compound<RoomDocument>(
                Builders<RoomDocument>.IndexKeys
                    .Ascending(r => r.Type)
                    .Ascending(r => r.Branch),
                "ix_type_branch"),
            // Sidebar ordering: most recently active room first.
            MongoIndex.Desc<RoomDocument>(r => r.LastMessage!.SentAt, "ix_lastMessageAt_desc"),
        ], ct);

        var members = database.GetCollection<RoomMemberDocument>(ChatMongoContext.RoomMembers);
        await MongoIndex.EnsureAsync(members,
        [
            // Makes double-joining impossible and powers the per-user lookup.
            MongoIndex.Compound<RoomMemberDocument>(
                Builders<RoomMemberDocument>.IndexKeys
                    .Ascending(m => m.RoomId)
                    .Ascending(m => m.UserId),
                "ux_room_user", unique: true),

            // "every room this user belongs to" — one query for the whole sidebar.
            MongoIndex.Compound<RoomMemberDocument>(
                Builders<RoomMemberDocument>.IndexKeys
                    .Ascending(m => m.UserId)
                    .Ascending(m => m.IsActive),
                "ix_user_active"),

            // Fan-out on send, and @mention resolution without calling Auth.
            MongoIndex.Compound<RoomMemberDocument>(
                Builders<RoomMemberDocument>.IndexKeys
                    .Ascending(m => m.RoomId)
                    .Ascending(m => m.IsActive),
                "ix_room_active"),

            MongoIndex.Asc<RoomMemberDocument>(m => m.AnonymousName, "ix_anonName"),
        ], ct);

        var messages = database.GetCollection<MessageDocument>(ChatMongoContext.Messages);
        await MongoIndex.EnsureAsync(messages,
        [
            // The history query. Descending on (roomId, sentAt, _id) so cursor
            // pagination is a pure index scan and ties break deterministically.
            MongoIndex.Compound<MessageDocument>(
                Builders<MessageDocument>.IndexKeys
                    .Ascending(m => m.RoomId)
                    .Descending(m => m.SentAt)
                    .Descending(m => m.Id),
                "ix_room_sentAt_id"),

            // Author lookups: "remove everything this user posted".
            MongoIndex.Asc<MessageDocument>(m => m.Author.UserId, "ix_author"),

            // Analytics: message volume over time.
            MongoIndex.Desc<MessageDocument>(m => m.SentAt, "ix_sentAt_desc"),

            MongoIndex.Asc<MessageDocument>(m => m.State.Deletion.Kind, "ix_deletionKind"),
        ], ct);

        var events = database.GetCollection<ModerationEventDocument>(ChatMongoContext.ModerationEvents);
        await MongoIndex.EnsureAsync(events,
        [
            MongoIndex.Desc<ModerationEventDocument>(e => e.Timestamp, "ix_timestamp_desc"),
            MongoIndex.Asc<ModerationEventDocument>(e => e.UserId, "ix_userId"),
            MongoIndex.Asc<ModerationEventDocument>(e => e.Category, "ix_category"),
            MongoIndex.Asc<ModerationEventDocument>(e => e.WasAllowed, "ix_wasAllowed"),
        ], ct);

        var files = database.GetCollection<FileDocument>(ChatMongoContext.Files);
        await MongoIndex.EnsureAsync(files,
        [
            MongoIndex.Asc<FileDocument>(f => f.OwnerUserId, "ix_owner"),
            MongoIndex.Asc<FileDocument>(f => f.MessageId, "ix_messageId"),
            MongoIndex.Asc<FileDocument>(f => f.Sha256, "ix_sha256"),
        ], ct);

        var presence = database.GetCollection<PresenceDocument>(ChatMongoContext.Presence);
        await MongoIndex.EnsureAsync(presence,
        [
            MongoIndex.Asc<PresenceDocument>(p => p.UserId, "ix_userId"),
            MongoIndex.Asc<PresenceDocument>(p => p.RoomIds, "ix_roomIds"),

            // Reaps connections lost without a clean disconnect. The in-memory
            // tracker leaked those entries until the process restarted.
            MongoIndex.Ttl<PresenceDocument>(
                p => p.LastSeenAt, TimeSpan.FromMinutes(5), "ttl_lastSeenAt"),
        ], ct);
    }
}
