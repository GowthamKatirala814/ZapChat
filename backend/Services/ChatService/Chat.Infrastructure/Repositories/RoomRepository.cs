using Chat.Application.Abstractions;
using Chat.Domain.Documents;
using Chat.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Chat.Infrastructure.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly IMongoCollection<RoomDocument> _rooms;

    public RoomRepository(ChatMongoContext context) => _rooms = context.RoomsCollection;

    private static readonly FilterDefinitionBuilder<RoomDocument> F =
        Builders<RoomDocument>.Filter;

    private static readonly UpdateDefinitionBuilder<RoomDocument> U =
        Builders<RoomDocument>.Update;

    public Task<RoomDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _rooms.Find(r => r.Id == id).FirstOrDefaultAsync(ct)!;

    public Task<RoomDocument?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _rooms.Find(r => r.Slug == RoomDocument.ToSlug(slug)).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<RoomDocument>> ListAsync(
        bool includeArchived, CancellationToken ct = default)
    {
        var filter = includeArchived ? F.Empty : F.Eq(r => r.IsArchived, false);

        return await _rooms.Find(filter)
            .Sort(Builders<RoomDocument>.Sort
                .Descending(r => r.LastMessage!.SentAt)
                .Ascending(r => r.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RoomDocument>> GetManyAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await _rooms.Find(F.In(r => r.Id, ids)).ToListAsync(ct);
    }

    public Task InsertAsync(RoomDocument room, CancellationToken ct = default)
    {
        room.Slug = RoomDocument.ToSlug(room.Name);
        return _rooms.InsertOneAsync(room, cancellationToken: ct);
    }

    public async Task<bool> UpdateAsync(
        Guid id, string name, string description, CancellationToken ct = default)
    {
        var result = await _rooms.UpdateOneAsync(
            F.And(F.Eq(r => r.Id, id), F.Eq(r => r.IsArchived, false)),
            U.Set(r => r.Name, name.Trim())
                .Set(r => r.Slug, RoomDocument.ToSlug(name))
                .Set(r => r.Description, description)
                .Set(r => r.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Archive, never delete. The old flow soft-deleted in Admin and hard-deleted in
    /// Chat, so a "recoverable" room lost every message permanently.
    /// </summary>
    public async Task<bool> ArchiveAsync(Guid id, Guid archivedBy, CancellationToken ct = default)
    {
        var result = await _rooms.UpdateOneAsync(
            F.And(
                F.Eq(r => r.Id, id),
                F.Eq(r => r.IsArchived, false),
                // System rooms (General, HR, branches) must always exist.
                F.Eq(r => r.IsSystemRoom, false)),
            U.Set(r => r.IsArchived, true)
                .Set(r => r.ArchivedAt, DateTime.UtcNow)
                .Set(r => r.ArchivedBy, archivedBy),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _rooms.UpdateOneAsync(
            F.Eq(r => r.Id, id),
            U.Set(r => r.IsArchived, false)
                .Unset(r => r.ArchivedAt)
                .Unset(r => r.ArchivedBy),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Preview and count move together in one atomic update, so the sidebar can never
    /// show a stale preview alongside a fresh count.
    /// </summary>
    public Task SetLastMessageAsync(
        Guid roomId, LastMessageSummary summary, CancellationToken ct = default) =>
        _rooms.UpdateOneAsync(
            F.Eq(r => r.Id, roomId),
            U.Set(r => r.LastMessage, summary).Inc(r => r.MessageCount, 1),
            cancellationToken: ct);

    public Task ClearLastMessageAsync(
        Guid roomId, LastMessageSummary? replacement, CancellationToken ct = default) =>
        _rooms.UpdateOneAsync(
            F.Eq(r => r.Id, roomId),
            U.Set(r => r.LastMessage, replacement),
            cancellationToken: ct);

    public Task AdjustMemberCountAsync(Guid roomId, int delta, CancellationToken ct = default) =>
        _rooms.UpdateOneAsync(
            F.Eq(r => r.Id, roomId),
            U.Inc(r => r.MemberCount, delta),
            cancellationToken: ct);

    public Task<long> CountAsync(bool includeArchived, CancellationToken ct = default) =>
        _rooms.CountDocumentsAsync(
            includeArchived ? F.Empty : F.Eq(r => r.IsArchived, false),
            cancellationToken: ct);
}
