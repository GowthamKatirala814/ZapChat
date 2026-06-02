using Microsoft.EntityFrameworkCore;
using PrivateChat.Domain.Entities;

namespace PrivateChat.Infrastructure.Persistence.DbContexts;

public class PrivateChatDbContext : DbContext
{
    public PrivateChatDbContext(
        DbContextOptions<PrivateChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations
        => Set<Conversation>();

    public DbSet<PrivateMessage> Messages
        => Set<PrivateMessage>();
}