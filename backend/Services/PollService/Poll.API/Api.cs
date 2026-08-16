using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Poll.Application;
using ZapChat.Shared.Auth;
using ZapChat.Shared.Realtime;

namespace Poll.API;

/// <summary>
/// Poll updates. There are no client-callable methods: the old PollHub.CastVote was a
/// second, divergent voting implementation that took userId from the client, never
/// allowed a vote change, and emitted a payload missing the reaction counts. Voting
/// goes through the REST endpoint and its single code path.
/// </summary>
[Authorize]
public sealed class PollHub : Hub
{
    private readonly ILogger<PollHub> _logger;

    public PollHub(ILogger<PollHub> logger) => _logger = logger;

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("Poll hub connected: {UserId}", Context.UserIdentifier);
        return base.OnConnectedAsync();
    }
}

public sealed class PollBroadcaster : IPollBroadcaster
{
    private readonly IHubContext<PollHub> _hub;
    private readonly ILogger<PollBroadcaster> _logger;

    public PollBroadcaster(IHubContext<PollHub> hub, ILogger<PollBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    // Polls are platform-wide, so All is the correct audience here.
    public Task PollCreatedAsync(PollDto poll) =>
        Safe(() => _hub.Clients.All.SendAsync(HubEvents.PollCreated, poll));

    public Task PollUpdatedAsync(PollDto poll) =>
        Safe(() => _hub.Clients.All.SendAsync(HubEvents.PollUpdated, poll));

    public Task PollClosedAsync(Guid pollId) =>
        Safe(() => _hub.Clients.All.SendAsync(HubEvents.PollClosed, new { pollId }));

    public Task PollRemovedAsync(Guid pollId) =>
        Safe(() => _hub.Clients.All.SendAsync(HubEvents.PollDeleted, new { pollId }));

    private async Task Safe(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A poll broadcast failed.");
        }
    }
}

[ApiController]
[Route("api/polls")]
public sealed class PollsController : ControllerBase
{
    private readonly IPollService _polls;

    public PollsController(IPollService polls) => _polls = polls;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PollDto>>> List(
        [FromQuery] int limit = 50, CancellationToken ct = default)
        => Ok(await _polls.ListAsync(limit, ct));

    [HttpGet("{pollId:guid}")]
    public async Task<ActionResult<PollDto>> Get(Guid pollId, CancellationToken ct)
        => Ok(await _polls.GetAsync(pollId, ct));

    [HttpPost]
    public async Task<ActionResult<PollDto>> Create(
        [FromBody] CreatePollRequest request, CancellationToken ct)
    {
        var poll = await _polls.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { pollId = poll.Id }, poll);
    }

    /// <summary>Casts, changes, or withdraws the caller's own vote.</summary>
    [HttpPost("{pollId:guid}/vote")]
    public async Task<ActionResult<PollDto>> Vote(
        Guid pollId, [FromBody] VoteRequest request, CancellationToken ct)
        => Ok(await _polls.VoteAsync(pollId, request, ct));

    [HttpPost("{pollId:guid}/reaction")]
    public async Task<ActionResult<PollDto>> React(
        Guid pollId, [FromBody] ReactRequest request, CancellationToken ct)
        => Ok(await _polls.ReactAsync(pollId, request, ct));

    /// <summary>Creator or admin.</summary>
    [HttpPost("{pollId:guid}/close")]
    public async Task<IActionResult> Close(Guid pollId, CancellationToken ct)
    {
        await _polls.CloseAsync(pollId, ct);
        return NoContent();
    }

    /// <summary>Admin only. There was previously no way to remove a poll at all.</summary>
    [HttpDelete("{pollId:guid}")]
    [Authorize(Policy = ZapChatPolicies.AdminOnly)]
    public async Task<IActionResult> Remove(Guid pollId, CancellationToken ct)
    {
        await _polls.RemoveAsync(pollId, ct);
        return NoContent();
    }
}

/// <summary>Poll analytics for the admin dashboard.</summary>
[ApiController]
[Route("api/poll-admin")]
[Authorize(Policy = ZapChatPolicies.AdminOnly)]
public sealed class PollAdminController : ControllerBase
{
    private readonly IPollRepository _polls;

    public PollAdminController(IPollRepository polls) => _polls = polls;

    [HttpGet("analytics/summary")]
    public async Task<ActionResult<object>> Summary(CancellationToken ct)
        => Ok(new { totalPolls = await _polls.CountAsync(ct) });

    [HttpGet("analytics/polls-per-day")]
    public async Task<ActionResult<object>> PerDay(
        [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var counts = (await _polls.CountByDayAsync(days, ct))
            .ToDictionary(x => x.Day.Date, x => x.Count);

        var since = DateTime.UtcNow.Date.AddDays(-Math.Clamp(days, 1, 365));

        return Ok(Enumerable.Range(0, Math.Clamp(days, 1, 365)).Select(offset =>
        {
            var day = since.AddDays(offset);
            return new { date = day.ToString("yyyy-MM-dd"), count = counts.GetValueOrDefault(day) };
        }));
    }

    /// <summary>
    /// Top polls with a participation rate. The rate needs the active-user count, which
    /// the admin service supplies — this endpoint returns raw votes and lets the caller
    /// compute the percentage, rather than reaching across services for a denominator.
    /// </summary>
    [HttpGet("analytics/top-polls")]
    public async Task<ActionResult<object>> TopPolls(
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var polls = await _polls.TopByVotesAsync(top, ct);

        return Ok(polls.Select(p => new
        {
            pollId = p.Id,
            question = p.Question,
            totalVotes = p.TotalVotes,
            upvotes = p.Upvotes,
            downvotes = p.Downvotes,
            status = p.Status.ToString(),
            createdAt = p.CreatedAt
        }));
    }
}
