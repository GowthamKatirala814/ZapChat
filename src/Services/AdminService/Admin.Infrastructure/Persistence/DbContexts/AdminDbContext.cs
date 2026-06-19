using Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admin.Infrastructure.Persistence.DbContexts;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<ModerationSettings> ModerationSettings => Set<ModerationSettings>();
    // public DbSet<ReportedMessage> ReportedMessages => Set<ReportedMessage>();
    public DbSet<RoomManagement> RoomManagements => Set<RoomManagement>();
    public DbSet<RoomMembership> RoomMemberships => Set<RoomMembership>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AuditLog — append-only, no update. All strings indexed for search performance.
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.PerformedBy);
            entity.HasIndex(x => x.Timestamp);
        });

        // BlockedUser — unique on UserId (one block record per user)
        modelBuilder.Entity<BlockedUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.EmailHash);
            entity.Property(x => x.EmailHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        });

        // ModerationSettings — singleton record
        modelBuilder.Entity<ModerationSettings>(entity =>
        {
            entity.HasKey(x => x.Id);
        });

        // ReportedMessage has been deprecated in favor of Report

        // RoomManagement — soft delete pattern
        modelBuilder.Entity<RoomManagement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.IsDeleted);
        });

        // Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MessageType).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => x.MessageId);
            entity.HasIndex(x => x.MessageAuthorId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.MessageId, x.ReportedByUserId }).IsUnique();
        });

        // RoomMembership — tracks user-room memberships
        modelBuilder.Entity<RoomMembership>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
            entity.HasIndex(x => x.RoomId);
            entity.HasIndex(x => x.UserId);
        });
    }
}
