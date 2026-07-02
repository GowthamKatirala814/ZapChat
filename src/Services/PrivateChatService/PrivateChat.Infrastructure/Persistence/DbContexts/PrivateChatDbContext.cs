using Microsoft.EntityFrameworkCore;
using PrivateChat.Domain.Entities;
using PrivateChat.Domain;

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

    public DbSet<PrivateModerationAuditLog> PrivateModerationAuditLogs
        => Set<PrivateModerationAuditLog>();

    public DbSet<UserBlock> UserBlocks
        => Set<UserBlock>();


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

        // Index to make ORDER BY LastMessageAt DESC fast
        modelBuilder.Entity<Conversation>()
            .HasIndex(x => x.LastMessageAt);

        // ── PrivateModerationAuditLog ─────────────────────────────────────────
        modelBuilder.Entity<PrivateModerationAuditLog>(entity =>
        {
            entity.Property(x => x.AnonymousName).HasMaxLength(100);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.MessageSnippet).HasMaxLength(200);
            entity.Property(x => x.Category).HasMaxLength(50);
            entity.Property(x => x.Explanation).HasMaxLength(500);

            // Indexes for Admin Dashboard queries and analytics
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ConversationId);
            entity.HasIndex(x => x.Category);
            entity.HasIndex(x => x.WasAllowed);
        });

        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasIndex(x => new { x.BlockerId, x.BlockedId }).IsUnique();
        });
    }
}