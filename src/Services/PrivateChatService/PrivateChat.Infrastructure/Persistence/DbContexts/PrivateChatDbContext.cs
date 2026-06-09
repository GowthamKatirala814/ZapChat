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

    public DbSet<PrivateMessageReaction> MessageReactions
        => Set<PrivateMessageReaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PrivateMessage>()
            .Property(x => x.Content)
            .HasMaxLength(2000);

        modelBuilder.Entity<PrivateMessage>()
            .HasOne(x => x.ParentMessage)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}