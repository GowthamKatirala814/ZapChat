using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Poll.Infrastructure.Persistence.DbContexts;

namespace Poll.API.Hubs;

[Authorize]
public class PollHub : Hub
{
    private readonly PollDbContext _context;

    public PollHub(PollDbContext context)
    {
        _context = context;
    }

    // Cast a vote and broadcast updated results to all clients
    public async Task CastVote(
        string pollId,
        string optionId,
        string userId)
    {
        var pollGuid = Guid.Parse(pollId);
        var optionGuid = Guid.Parse(optionId);
        var userGuid = Guid.Parse(userId);

        // Prevent duplicate votes from same user on same poll
        var alreadyVoted = await _context.PollVotes
            .AnyAsync(v =>
                v.PollId == pollGuid &&
                v.UserId == userGuid);

        if (alreadyVoted)
        {
            await Clients.Caller.SendAsync(
                "VoteError",
                "You have already voted on this poll.");
            return;
        }

        var option = await _context.PollOptions
            .FirstOrDefaultAsync(o => o.Id == optionGuid);

        if (option == null)
        {
            await Clients.Caller.SendAsync(
                "VoteError",
                "Invalid option.");
            return;
        }

        // Persist vote
        _context.PollVotes.Add(new Poll.Domain.Entities.PollVote
        {
            Id = Guid.NewGuid(),
            PollId = pollGuid,
            OptionId = optionGuid,
            UserId = userGuid,
            VotedAt = DateTime.UtcNow
        });

        option.VoteCount++;
        await _context.SaveChangesAsync();

        // Load full updated poll and broadcast to all
        var updatedPoll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollGuid);

        await Clients.All.SendAsync("PollUpdated", new
        {
            id = updatedPoll!.Id,
            question = updatedPoll.Question,
            createdAt = updatedPoll.CreatedAt,
            options = updatedPoll.Options.Select(o => new
            {
                id = o.Id,
                optionText = o.OptionText,
                voteCount = o.VoteCount
            })
        });
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(
            $"[PollHub] User connected: {Context.UserIdentifier}");
        await base.OnConnectedAsync();
    }
}
