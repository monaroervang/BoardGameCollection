using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;

namespace API.Seeding;

public sealed class DevelopmentAuthenticationSeeder
{
    private const string DevelopmentPassword = "Test123!";
    private const string AdminUsername = "user0001";

    private readonly BoardGameDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DevelopmentAuthenticationSeeder(
        BoardGameDbContext context,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // Configure authentication and authorization for seeded development users.
    public async Task SeedAsync()
    {
        var users = await _context.Users
            .ToListAsync();

        foreach (var user in users)
        {
            // Set a known development password if the user does not have one.
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                var passwordHash = _passwordHasher.HashPassword(
                    user,
                    DevelopmentPassword);

                user.SetPasswordHash(passwordHash);
            }

            // Assign the administrator role explicitly.
            if (user.Username == AdminUsername &&
                user.Role != UserRole.Admin)
            {
                user.SetRole(UserRole.Admin);
            }
        }

        await _context.SaveChangesAsync();
    }
}