using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Poll.API.Hubs;
using Poll.Application.DTOs;
using Poll.Domain.Entities;
using Poll.Infrastructure.Persistence.DbContexts;

namespace Poll.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PollController : ControllerBase
{
    private readonly PollDbContext _context;
    private readonly IHubContext<PollHub> _hubContext;

    public PollController(
        PollDbContext context,
        IHubContext<PollHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPolls([FromQuery] Guid? userId)
    {
        var polls = await _context.Polls
            .Include(p => p.Options)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var pollIds = polls.Select(p => p.Id).ToList();

        var userVotes = new Dictionary<Guid, Guid>();
        var userReactions = new Dictionary<Guid, bool>();

        if (userId.HasValue)
        {
            userVotes = await _context.PollVotes
                .Where(v => v.UserId == userId.Value && pollIds.Contains(v.PollId))
                .ToDictionaryAsync(v => v.PollId, v => v.OptionId);

            userReactions = await _context.PollReactions
                .Where(r => r.UserId == userId.Value && pollIds.Contains(r.PollId))
                .ToDictionaryAsync(r => r.PollId, r => r.IsUpvote);
        }

        var result = polls.Select(p => new
        {
            id = p.Id,
            question = p.Question,
            createdAt = p.CreatedAt,
            creatorId = p.CreatorId,
            upvotes = p.Upvotes,
            downvotes = p.Downvotes,
            userVoteOptionId = userVotes.ContainsKey(p.Id) ? (Guid?)userVotes[p.Id] : null,
            userReaction = userReactions.ContainsKey(p.Id) ? (bool?)userReactions[p.Id] : null,
            options = p.Options.Select(o => new
            {
                id = o.Id,
                optionText = o.OptionText,
                voteCount = o.VoteCount
            })
        });

        return Ok(result);
    }

    [HttpGet("{pollId}")]
    public async Task<IActionResult> GetPoll(Guid pollId, [FromQuery] Guid? userId)
    {
        var p = await _context.Polls
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == pollId);

        if (p == null)
            return NotFound();

        Guid? userVoteOptionId = null;
        bool? userReaction = null;

        if (userId.HasValue)
        {
            userVoteOptionId = await _context.PollVotes
                .Where(v => v.PollId == pollId && v.UserId == userId.Value)
                .Select(v => (Guid?)v.OptionId)
                .FirstOrDefaultAsync();

            var reaction = await _context.PollReactions
                .FirstOrDefaultAsync(r => r.PollId == pollId && r.UserId == userId.Value);
            
            if (reaction != null)
            {
                userReaction = reaction.IsUpvote;
            }
        }

        return Ok(new
        {
            id = p.Id,
            question = p.Question,
            createdAt = p.CreatedAt,
            creatorId = p.CreatorId,
            upvotes = p.Upvotes,
            downvotes = p.Downvotes,
            userVoteOptionId,
            userReaction,
            options = p.Options.Select(o => new
            {
                id = o.Id,
                optionText = o.OptionText,
                voteCount = o.VoteCount
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePoll(
        [FromBody] CreatePollRequest request)
    {
        var poll = new Poll.Domain.Entities.Poll
        {
            Id = Guid.NewGuid(),
            Question = request.Question,
            CreatedAt = DateTime.UtcNow,
            CreatorId = request.CreatorId,
            Upvotes = 0,
            Downvotes = 0
        };

        foreach (var optionText in request.Options)
        {
            poll.Options.Add(new PollOption
            {
                Id = Guid.NewGuid(),
                PollId = poll.Id,
                OptionText = optionText,
                VoteCount = 0
            });
        }

        _context.Polls.Add(poll);
        await _context.SaveChangesAsync();

        var response = new
        {
            id = poll.Id,
            question = poll.Question,
            createdAt = poll.CreatedAt,
            creatorId = poll.CreatorId,
            upvotes = poll.Upvotes,
            downvotes = poll.Downvotes,
            userVoteOptionId = (Guid?)null,
            userReaction = (bool?)null,
            options = poll.Options.Select(o => new
            {
                id = o.Id,
                optionText = o.OptionText,
                voteCount = o.VoteCount
            })
        };

        await _hubContext.Clients.All.SendAsync("PollCreated", response);

        return Ok(response);
    }

    [HttpPost("vote")]
    public async Task<IActionResult> Vote(
        [FromBody] VoteRequest request)
    {
        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == request.PollId);

        if (poll == null)
            return NotFound(new { error = "Poll not found." });

        var existingVote = await _context.PollVotes
            .FirstOrDefaultAsync(v => v.PollId == request.PollId && v.UserId == request.UserId);

        if (request.OptionId.HasValue)
        {
            var newOption = poll.Options.FirstOrDefault(o => o.Id == request.OptionId.Value);
            if (newOption == null)
                return NotFound(new { error = "Option not found." });

            if (existingVote != null)
            {
                if (existingVote.OptionId == request.OptionId.Value)
                {
                    // User clicked the same option -> remove vote
                    var oldOption = poll.Options.FirstOrDefault(o => o.Id == existingVote.OptionId);
                    if (oldOption != null) oldOption.VoteCount--;
                    _context.PollVotes.Remove(existingVote);
                }
                else
                {
                    // User changed vote -> decrement old, increment new
                    var oldOption = poll.Options.FirstOrDefault(o => o.Id == existingVote.OptionId);
                    if (oldOption != null) oldOption.VoteCount--;
                    
                    newOption.VoteCount++;
                    existingVote.OptionId = request.OptionId.Value;
                    existingVote.VotedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // New vote
                newOption.VoteCount++;
                _context.PollVotes.Add(new PollVote
                {
                    Id = Guid.NewGuid(),
                    PollId = request.PollId,
                    OptionId = request.OptionId.Value,
                    UserId = request.UserId,
                    VotedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Remove vote
            if (existingVote != null)
            {
                var oldOption = poll.Options.FirstOrDefault(o => o.Id == existingVote.OptionId);
                if (oldOption != null) oldOption.VoteCount--;
                _context.PollVotes.Remove(existingVote);
            }
        }

        await _context.SaveChangesAsync();

        var response = new
        {
            id = poll.Id,
            question = poll.Question,
            createdAt = poll.CreatedAt,
            creatorId = poll.CreatorId,
            upvotes = poll.Upvotes,
            downvotes = poll.Downvotes,
            options = poll.Options.Select(o => new
            {
                id = o.Id,
                optionText = o.OptionText,
                voteCount = o.VoteCount
            })
        };

        await _hubContext.Clients.All.SendAsync("PollUpdated", response);
        return Ok(response);
    }

    [HttpPost("react")]
    public async Task<IActionResult> React(
        [FromBody] ReactRequest request)
    {
        var poll = await _context.Polls
            .FirstOrDefaultAsync(p => p.Id == request.PollId);

        if (poll == null)
            return NotFound(new { error = "Poll not found." });

        var existingReaction = await _context.PollReactions
            .FirstOrDefaultAsync(r => r.PollId == request.PollId && r.UserId == request.UserId);

        if (request.IsUpvote.HasValue)
        {
            if (existingReaction != null)
            {
                if (existingReaction.IsUpvote == request.IsUpvote.Value)
                {
                    // User clicked the same reaction -> remove it
                    if (existingReaction.IsUpvote) poll.Upvotes--;
                    else poll.Downvotes--;
                    
                    _context.PollReactions.Remove(existingReaction);
                }
                else
                {
                    // User switched reaction
                    if (existingReaction.IsUpvote)
                    {
                        poll.Upvotes--;
                        poll.Downvotes++;
                    }
                    else
                    {
                        poll.Downvotes--;
                        poll.Upvotes++;
                    }
                    existingReaction.IsUpvote = request.IsUpvote.Value;
                    existingReaction.ReactedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // New reaction
                if (request.IsUpvote.Value) poll.Upvotes++;
                else poll.Downvotes++;

                _context.PollReactions.Add(new PollReaction
                {
                    Id = Guid.NewGuid(),
                    PollId = request.PollId,
                    UserId = request.UserId,
                    IsUpvote = request.IsUpvote.Value,
                    ReactedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Remove reaction entirely
            if (existingReaction != null)
            {
                if (existingReaction.IsUpvote) poll.Upvotes--;
                else poll.Downvotes--;
                
                _context.PollReactions.Remove(existingReaction);
            }
        }

        await _context.SaveChangesAsync();

        // Broadcast the update so counts are real-time
        // We only broadcast poll updates, not the individual user state.
        // We have to include options here since the frontend replaces the whole poll object from PollUpdated
        var pollWithOptions = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == request.PollId);

        var response = new
        {
            id = pollWithOptions!.Id,
            question = pollWithOptions.Question,
            createdAt = pollWithOptions.CreatedAt,
            creatorId = pollWithOptions.CreatorId,
            upvotes = pollWithOptions.Upvotes,
            downvotes = pollWithOptions.Downvotes,
            options = pollWithOptions.Options.Select(o => new
            {
                id = o.Id,
                optionText = o.OptionText,
                voteCount = o.VoteCount
            })
        };

        await _hubContext.Clients.All.SendAsync("PollUpdated", response);
        return Ok(response);
    }
}