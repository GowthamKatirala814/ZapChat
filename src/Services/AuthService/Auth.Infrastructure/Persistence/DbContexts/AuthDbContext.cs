using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence.DbContexts;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AnonymousProfile> AnonymousProfiles => Set<AnonymousProfile>();

    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();

    public DbSet<RegistrationOtp> RegistrationOtps => Set<RegistrationOtp>();

    public DbSet<GeminiUsage> GeminiUsages => Set<GeminiUsage>();
    
    public DbSet<AiHealthEvent> AiHealthEvents => Set<AiHealthEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(x => x.FullName)
            .HasMaxLength(200);

        modelBuilder.Entity<Role>()
            .Property(x => x.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<AnonymousProfile>()
            .Property(x => x.AnonymousName)
            .HasMaxLength(100);

        modelBuilder.Entity<AnonymousProfile>()
            .HasIndex(x => x.AnonymousName)
            .IsUnique();

        modelBuilder.Entity<PasswordResetOtp>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetOtp>()
            .Property(x => x.OtpCode)
            .HasMaxLength(6);

        modelBuilder.Entity<PasswordResetOtp>()
            .Property(x => x.Email)
            .HasMaxLength(256);

        modelBuilder.Entity<PasswordResetOtp>()
            .Property(x => x.ResetToken)
            .HasMaxLength(64);

        // RegistrationOtp — standalone, no FK to User (user does not exist yet)
        modelBuilder.Entity<RegistrationOtp>()
            .Property(x => x.Email)
            .HasMaxLength(256);

        modelBuilder.Entity<RegistrationOtp>()
            .Property(x => x.OtpCode)
            .HasMaxLength(6);

        modelBuilder.Entity<RegistrationOtp>()
            .Property(x => x.VerificationToken)
            .HasMaxLength(64);

        modelBuilder.Entity<RegistrationOtp>()
            .Property(x => x.FullName)
            .HasMaxLength(200);

        // ── Performance indexes ───────────────────────────────────────────────
        // RefreshToken.Token — looked up on every /refresh and /logout call
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        // RefreshToken.UserId — used when revoking all tokens for a user
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.UserId);

        // User.IsDeleted — filtered on almost every user query
        modelBuilder.Entity<User>()
            .HasIndex(x => x.IsDeleted);

        // User.CreatedAt — used in analytics ordering
        modelBuilder.Entity<User>()
            .HasIndex(x => x.CreatedAt);

        // GeminiUsage.Date — one tracker per day
        modelBuilder.Entity<GeminiUsage>()
            .HasIndex(x => x.Date)
            .IsUnique();
    }
}