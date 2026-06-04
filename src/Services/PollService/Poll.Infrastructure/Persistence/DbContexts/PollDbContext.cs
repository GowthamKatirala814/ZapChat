using Microsoft.EntityFrameworkCore;
using Poll.Domain.Entities;

namespace Poll.Infrastructure.Persistence.DbContexts;

public class PollDbContext : DbContext
{
    public PollDbContext(
        DbContextOptions<PollDbContext> options)
        : base(options)
    {
    }

    public DbSet<Poll.Domain.Entities.Poll> Polls
        => Set<Poll.Domain.Entities.Poll>();

    public DbSet<PollOption> PollOptions
        => Set<PollOption>();

    public DbSet<PollVote> PollVotes
        => Set<PollVote>();
}