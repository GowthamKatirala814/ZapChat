using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Persistence.DbContexts;

public class ChatDbContext : DbContext
{
    public ChatDbContext(
        DbContextOptions<ChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions
    => Set<MessageReaction>();
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatRoom>()
            .Property(x => x.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<Message>()
            .Property(x => x.Content)
            .HasMaxLength(2000);

        modelBuilder.Entity<Message>()
            .Property(x => x.AnonymousName)
            .HasMaxLength(100);
        modelBuilder.Entity<Message>()
    .HasOne(x => x.ParentMessage)
    .WithMany(x => x.Replies)
    .HasForeignKey(x => x.ParentMessageId)
    .OnDelete(DeleteBehavior.Restrict);
    }

}