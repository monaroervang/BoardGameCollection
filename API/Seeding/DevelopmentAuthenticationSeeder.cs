using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;

namespace API.Seeding;

public sealed class DevelopmentAuthenticationSeeder
{
    private readonly BoardGameDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DevelopmentAuthenticationSeeder(
        BoardGameDbContext context,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // Set a known development password for seeded users.
    public async Task SeedAsync()
    {
        var users = await _context.Users
            .Where(user => user.PasswordHash == "")
            .ToListAsync();

        foreach (var user in users)
        {
            var passwordHash = _passwordHasher.HashPassword(
                user,
                "Test123!");

            user.SetPasswordHash(passwordHash);
        }

        await _context.SaveChangesAsync();
    }
}