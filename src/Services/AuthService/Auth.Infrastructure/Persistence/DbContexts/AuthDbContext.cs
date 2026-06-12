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
    }
}