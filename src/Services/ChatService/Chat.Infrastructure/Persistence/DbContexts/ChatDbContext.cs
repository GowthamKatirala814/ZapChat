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
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<ModerationAuditLog> ModerationAuditLogs => Set<ModerationAuditLog>();
    public DbSet<ChatRoomReadState> ChatRoomReadStates => Set<ChatRoomReadState>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatRoom>()
            .Property(x => x.Name)
            .HasMaxLength(100);
            
        modelBuilder.Entity<ChatRoomReadState>()
            .HasOne(x => x.ChatRoom)
            .WithMany()
            .HasForeignKey(x => x.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // ── ModerationAuditLog ────────────────────────────────────────────────
        modelBuilder.Entity<ModerationAuditLog>(entity =>
        {
            entity.Property(x => x.AnonymousName).HasMaxLength(100);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.RoomName).HasMaxLength(100);
            entity.Property(x => x.MessageSnippet).HasMaxLength(200);
            entity.Property(x => x.Category).HasMaxLength(50);
            entity.Property(x => x.Explanation).HasMaxLength(500);

            // Indexes for Admin Dashboard queries and analytics
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.RoomId);
            entity.HasIndex(x => x.Category);
            entity.HasIndex(x => x.WasAllowed);
        });
    }
}