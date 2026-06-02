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
    }
}