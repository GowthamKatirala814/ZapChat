using Chat.Application.Abstractions;
using Chat.Domain.Documents;
using Chat.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Chat.Infrastructure.Repositories;

public sealed class RoomMemberRepository : IRoomMemberRepository
{
    private readonly IMongoCollection<RoomMemberDocument> _members;

    public RoomMemberRepository(ChatMongoContext context) => _members = context.MembersCollection;

    private static readonly FilterDefinitionBuilder<RoomMemberDocument> F =
        Builders<RoomMemberDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<RoomMemberDocument> U =
        Builders<RoomMemberDocument>.Update;

    public Task<RoomMemberDocument?> GetAsync(
        Guid roomId, Guid userId, CancellationToken ct = default) =>
        _members.Find(m => m.RoomId == roomId && m.UserId == userId).FirstOrDefaultAsync(ct)!;

    public async Task<bool> IsActiveMemberAsync(
        Guid roomId, Guid userId, CancellationToken ct = default) =>
        await _members
            .Find(m => m.RoomId == roomId && m.UserId == userId && m.IsActive)
            .Project(m => m.Id)
            .AnyAsync(ct);

    /// <summary>
    /// Upsert, so joining is idempotent and a previously-left member is reactivated
    /// rather than duplicated. The unique index on (roomId, userId) is the guarantee.
    /// </summary>
    public async Task<bool> JoinAsync(
        Guid roomId, Guid userId, string anonymousName, CancellationToken ct = default)
    {
        var result = await _members.UpdateOneAsync(
            F.And(F.Eq(m => m.RoomId, roomId), F.Eq(m => m.UserId, userId)),
            U.SetOnInsert(m => m.Id, Guid.NewGuid())
                .SetOnInsert(m => m.RoomId, roomId)
                .SetOnInsert(m => m.UserId, userId)
                .SetOnInsert(m => m.JoinedAt, DateTime.UtcNow)
                .SetOnInsert(m => m.UnreadCount, 0)
                .SetOnInsert(m => m.LastReadAt, DateTime.UtcNow)
                .Set(m => m.IsActive, true)
                .Set(m => m.AnonymousName, anonymousName),
            new UpdateOptions { IsUpsert = true },
            ct);

        // UpsertedId set means a new membership; otherwise it already existed.
        return result.UpsertedId is not null;
    }

    public async Task<bool> LeaveAsync(Guid roomId, Guid userId, CancellationToken ct = default)
    {
        var result = await _members.UpdateOneAsync(
            F.And(
                F.Eq(m => m.RoomId, roomId),
                F.Eq(m => m.UserId, userId),
                F.Eq(m => m.IsActive, true)),
            U.Set(m => m.IsActive, false),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<IReadOnlyList<RoomMemberDocument>> ListForRoomAsync(
        Guid roomId, CancellationToken ct = default) =>
        await _members.Find(m => m.RoomId == roomId && m.IsActive)
            .SortBy(m => m.AnonymousName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RoomMemberDocument>> ListForUserAsync(
        Guid userId, CancellationToken ct = default) =>
        await _members.Find(m => m.UserId == userId && m.IsActive).ToListAsync(ct);

    /// <summary>
    /// Bumps every other active member's unread count in ONE command, then reads the
    /// updated rows so the hub can push each member their exact persisted count.
    ///
    /// The old code fetched the member list from another service over HTTP (which
    /// 401'd), looped in memory, and issued a write per member.
    /// </summary>
    public async Task<IReadOnlyList<RoomMemberDocument>> IncrementUnreadExceptAsync(
        Guid roomId, Guid senderUserId, CancellationToken ct = default)
    {
        var filter = F.And(
            F.Eq(m => m.RoomId, roomId),
            F.Eq(m => m.IsActive, true),
            F.Ne(m => m.UserId, senderUserId));

        await _members.UpdateManyAsync(filter, U.Inc(m => m.UnreadCount, 1), cancellationToken: ct);

        return await _members.Find(filter).ToListAsync(ct);
    }

    public async Task<bool> MarkReadAsync(Guid roomId, Guid userId, CancellationToken ct = default)
    {
        var result = await _members.UpdateOneAsync(
            F.And(F.Eq(m => m.RoomId, roomId), F.Eq(m => m.UserId, userId)),
            U.Set(m => m.UnreadCount, 0).Set(m => m.LastReadAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.MatchedCount > 0;
    }

    public Task RefreshAnonymousNameAsync(
        Guid userId, string anonymousName, CancellationToken ct = default) =>
        _members.UpdateManyAsync(
            F.And(F.Eq(m => m.UserId, userId), F.Ne(m => m.AnonymousName, anonymousName)),
            U.Set(m => m.AnonymousName, anonymousName),
            cancellationToken: ct);

    public async Task<long> DeactivateAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _members.UpdateManyAsync(
            F.And(F.Eq(m => m.UserId, userId), F.Eq(m => m.IsActive, true)),
            U.Set(m => m.IsActive, false),
            cancellationToken: ct);

        return result.ModifiedCount;
    }
}
