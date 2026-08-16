using Chat.Application.Abstractions;
using Chat.Application.DTOs;
using Chat.Domain.Documents;
using Microsoft.Extensions.Logging;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Errors;

namespace Chat.Application.Services;

public sealed class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;
    private readonly IRoomMemberRepository _members;
    private readonly IMessageRepository _messages;
    private readonly IPresenceRepository _presence;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        IRoomRepository rooms,
        IRoomMemberRepository members,
        IMessageRepository messages,
        IPresenceRepository presence,
        ICurrentUser currentUser,
        ILogger<RoomService> logger)
    {
        _rooms = rooms;
        _members = members;
        _messages = messages;
        _presence = presence;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// The rooms that must always exist, created on startup if missing.
    ///
    /// Defined in code rather than in a seed script so a fresh clone with an empty
    /// database is immediately usable: without this the sidebar renders correctly but
    /// completely empty, which reads as a broken install rather than a missing step.
    /// Adding a branch here means adding the matching office to the registration form.
    /// </summary>
    private static readonly (string Name, RoomType Type, string? Branch, string Description)[]
        SystemRooms =
        [
            ("General Chat", RoomType.General, null,
                "Company-wide announcements and discussion."),
            ("HR Issues", RoomType.Hr, null,
                "Raise HR and policy matters. Content checks are enforced here."),
            ("Hyderabad", RoomType.Branch, "Hyderabad",
                "Hyderabad office channel."),
            ("Bangalore", RoomType.Branch, "Bangalore",
                "Bangalore office channel.")
        ];

    // ── Access control ──────────────────────────────────────────────────────────

    /// <summary>
    /// The single rule for who may read a room. Enforced identically on the REST and
    /// hub paths. The old system had no rule at all: every room, including HR Issues
    /// and both branch channels, was readable by anyone — unauthenticated.
    /// </summary>
    public async Task<RoomDocument> RequireReadAccessAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, ct)
                   ?? throw new NotFoundException("That room does not exist.");

        if (room.IsArchived && !_currentUser.IsAdmin)
            throw new NotFoundException("That room does not exist.");

        // Admins can read everything for moderation.
        if (_currentUser.IsAdmin) return room;

        switch (room.Type)
        {
            case RoomType.General:
            case RoomType.Hr:
                return room;

            case RoomType.Branch:
                // The branch claim is issued from the stored, admin-managed value, so
                // a user cannot grant themselves access by editing their profile.
                if (!string.Equals(_currentUser.Branch, room.Branch, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ForbiddenException(
                        $"This channel is limited to the {room.Branch} office.");
                }
                return room;

            case RoomType.Custom:
                var userId = _currentUser.RequireUserId();
                if (!await _members.IsActiveMemberAsync(roomId, userId, ct))
                    throw new ForbiddenException("You are not a member of this room.");
                return room;

            default:
                throw new ForbiddenException("You do not have access to this room.");
        }
    }

    private bool CanSee(RoomDocument room) =>
        _currentUser.IsAdmin
        || room.Type != RoomType.Branch
        || string.Equals(_currentUser.Branch, room.Branch, StringComparison.OrdinalIgnoreCase);

    // ── Queries ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RoomDto>> GetVisibleRoomsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        var rooms = await _rooms.ListAsync(includeArchived: false, ct);

        // One query for every membership this user has, instead of a per-room lookup.
        var memberships = (await _members.ListForUserAsync(userId, ct))
            .ToDictionary(m => m.RoomId);

        return rooms
            .Where(CanSee)
            .Select(r =>
            {
                memberships.TryGetValue(r.Id, out var membership);
                return ToDto(r, membership);
            })
            .ToList();
    }

    public async Task<RoomDto> GetRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await RequireReadAccessAsync(roomId, ct);
        var membership = await _members.GetAsync(roomId, _currentUser.RequireUserId(), ct);
        return ToDto(room, membership);
    }

    public async Task<IReadOnlyList<RoomMemberDto>> GetMembersAsync(
        Guid roomId, CancellationToken ct = default)
    {
        await RequireReadAccessAsync(roomId, ct);

        var members = await _members.ListForRoomAsync(roomId, ct);
        var online = (await _presence.GetOnlineUserIdsAsync(roomId, ct)).ToHashSet();

        // Only anonymous names cross the wire.
        return members
            .Select(m => new RoomMemberDto(m.UserId, m.AnonymousName, online.Contains(m.UserId)))
            .ToList();
    }

    /// <summary>
    /// Who has read up to at least this message. Previously always returned an empty
    /// list because the member list came from a call that 401'd.
    /// </summary>
    public async Task<IReadOnlyList<ReadReceiptDto>> GetReadReceiptsAsync(
        Guid messageId, CancellationToken ct = default)
    {
        var message = await _messages.GetByIdAsync(messageId, ct)
                      ?? throw new NotFoundException("That message does not exist.");

        await RequireReadAccessAsync(message.RoomId, ct);

        var members = await _members.ListForRoomAsync(message.RoomId, ct);
        var me = _currentUser.RequireUserId();

        return members
            .Where(m => m.UserId != me && m.LastReadAt >= message.SentAt)
            .Select(m => new ReadReceiptDto(m.AnonymousName, m.LastReadAt))
            .ToList();
    }

    // ── Membership ──────────────────────────────────────────────────────────────

    public async Task<RoomDto> JoinAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await RequireReadAccessAsync(roomId, ct);
        var userId = _currentUser.RequireUserId();

        var added = await _members.JoinAsync(roomId, userId, _currentUser.AnonymousName, ct);

        if (added)
        {
            await _rooms.AdjustMemberCountAsync(roomId, +1, ct);
            _logger.LogInformation("User {UserId} joined room {RoomId}.", userId, roomId);

            // Re-read so the returned MemberCount reflects the increment just applied
            // rather than the value read before it.
            room = await _rooms.GetByIdAsync(roomId, ct) ?? room;
        }

        var membership = await _members.GetAsync(roomId, userId, ct);
        return ToDto(room, membership);
    }

    public async Task LeaveAsync(Guid roomId, CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();

        if (await _members.LeaveAsync(roomId, userId, ct))
            await _rooms.AdjustMemberCountAsync(roomId, -1, ct);
    }

    public async Task MarkReadAsync(Guid roomId, CancellationToken ct = default)
    {
        await RequireReadAccessAsync(roomId, ct);

        // The user id comes from the token. The old endpoint took it from the query
        // string, so anyone could clear anyone else's unread count.
        await _members.MarkReadAsync(roomId, _currentUser.RequireUserId(), ct);
    }

    // ── Administration ──────────────────────────────────────────────────────────

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _rooms.GetBySlugAsync(name, ct) is not null)
            throw new ConflictException($"A room named '{name}' already exists.");

        if (request.Type == RoomType.Branch && string.IsNullOrWhiteSpace(request.Branch))
            throw new ValidationException("A branch room must specify which branch it belongs to.");

        var room = new RoomDocument
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = request.Type,
            Branch = request.Type == RoomType.Branch ? request.Branch!.Trim() : null,
            Description = request.Description.Trim(),
            CreatedBy = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _rooms.InsertAsync(room, ct);
        _logger.LogInformation(
            "Admin {AdminId} created room {RoomId} ({Name}).", _currentUser.UserId, room.Id, name);

        return ToDto(room, membership: null);
    }

    public async Task<RoomDto> UpdateAsync(
        Guid roomId, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var existing = await _rooms.GetBySlugAsync(request.Name, ct);
        if (existing is not null && existing.Id != roomId)
            throw new ConflictException($"A room named '{request.Name}' already exists.");

        if (!await _rooms.UpdateAsync(roomId, request.Name, request.Description, ct))
            throw new NotFoundException("That room does not exist, or it is archived.");

        var room = await _rooms.GetByIdAsync(roomId, ct)!;
        return ToDto(room!, membership: null);
    }

    public async Task ArchiveAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, ct)
                   ?? throw new NotFoundException("That room does not exist.");

        if (room.IsSystemRoom)
            throw new ValidationException($"'{room.Name}' is a system room and cannot be archived.");

        if (!await _rooms.ArchiveAsync(roomId, _currentUser.RequireUserId(), ct))
            throw new ConflictException("That room is already archived.");

        _logger.LogWarning(
            "Admin {AdminId} archived room {RoomId}. Messages are retained.",
            _currentUser.UserId, roomId);
    }

    public async Task RestoreAsync(Guid roomId, CancellationToken ct = default)
    {
        if (!await _rooms.RestoreAsync(roomId, ct))
            throw new NotFoundException("That room does not exist.");
    }

    // ── Bootstrap ───────────────────────────────────────────────────────────────

    public async Task JoinDefaultRoomsAsync(Guid userId, CancellationToken ct = default)
    {
        var rooms = await _rooms.ListAsync(includeArchived: false, ct);

        foreach (var room in rooms.Where(r => r.IsSystemRoom && r.Type != RoomType.Custom))
        {
            if (await _members.JoinAsync(room.Id, userId, anonymousName: string.Empty, ct))
                await _rooms.AdjustMemberCountAsync(room.Id, +1, ct);
        }
    }

    public async Task EnsureSystemRoomsAsync(CancellationToken ct = default)
    {
        foreach (var (name, type, branch, description) in SystemRooms)
        {
            if (await _rooms.GetBySlugAsync(name, ct) is not null) continue;

            await _rooms.InsertAsync(new RoomDocument
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = type,
                Branch = branch,
                Description = description,
                IsSystemRoom = true,
                CreatedAt = DateTime.UtcNow
            }, ct);

            _logger.LogInformation("Created system room '{Name}'.", name);
        }
    }

    private static RoomDto ToDto(RoomDocument r, RoomMemberDocument? membership) => new(
        r.Id, r.Name, r.Type, r.Branch, r.Description,
        r.MemberCount, r.MessageCount, r.IsArchived, r.CreatedAt,
        r.LastMessage is null
            ? null
            : new LastMessageDto(
                r.LastMessage.MessageId, r.LastMessage.Preview,
                r.LastMessage.AuthorName, r.LastMessage.SentAt),
        membership?.UnreadCount ?? 0,
        membership?.IsActive ?? false);
}
