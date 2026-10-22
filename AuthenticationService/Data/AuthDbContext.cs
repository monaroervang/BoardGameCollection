using AuthenticationService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuthUser> Users => Set<AuthUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.Role)
                .IsRequired()
                .HasMaxLength(30);
        });
    }
}