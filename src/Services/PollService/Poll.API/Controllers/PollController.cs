using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poll.Application.DTOs;
using Poll.Domain.Entities;
using Poll.Infrastructure.Persistence.DbContexts;

namespace Poll.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PollController : ControllerBase
{
    private readonly PollDbContext _context;

    public PollController(
        PollDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePoll(
        CreatePollRequest request)
    {
        var poll = new Poll.Domain.Entities.Poll
        {
            Id = Guid.NewGuid(),
            Question = request.Question
        };

        foreach (var option in request.Options)
        {
            poll.Options.Add(
                new PollOption
                {
                    Id = Guid.NewGuid(),
                    OptionText = option
                });
        }

        _context.Polls.Add(poll);

        await _context.SaveChangesAsync();

        return Ok(poll);
    }

    [HttpPost("vote")]
    public async Task<IActionResult> Vote(
        VoteRequest request)
    {
        var vote = new PollVote
        {
            Id = Guid.NewGuid(),
            PollId = request.PollId,
            OptionId = request.OptionId,
            UserId = request.UserId
        };

        _context.PollVotes.Add(vote);

        var option =
            await _context.PollOptions
                .FirstOrDefaultAsync(
                    x => x.Id == request.OptionId);

        if (option != null)
        {
            option.VoteCount++;
        }

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("{pollId}")]
    public async Task<IActionResult> GetPoll(
        Guid pollId)
    {
        var poll =
            await _context.Polls
                .Include(x => x.Options)
                .FirstOrDefaultAsync(
                    x => x.Id == pollId);

        if (poll == null)
            return NotFound();

        return Ok(poll);
    }
}