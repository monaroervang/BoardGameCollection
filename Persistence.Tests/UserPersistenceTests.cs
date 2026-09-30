
using Domain.Models;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;

namespace Persistence.Tests;

public class UserPersistenceTests
{
    [Fact]
    public async Task User_CanBeSavedAndLoaded_WithValueObjects()
    {
        // Create an isolated SQL Server database for this test.
        var options = CreateTestDatabaseOptions();

        try
        {
            var user = new User(
                "testuser",
                new Name("Emma", "Jensen"),
                new Email("emma@example.com"),
                new DateOnly(2000, 5, 15),
                new Address(
                    "Søndergade",
                    "12",
                    "7100",
                    "Vejle",
                    "Denmark"));

            // Save the user.
            await using (var context = new BoardGameDbContext(options))
            {
                await context.Database.MigrateAsync();

                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Load the user using a new DbContext.
            await using (var context = new BoardGameDbContext(options))
            {
                var savedUser = await context.Users
                    .AsNoTracking()
                    .SingleAsync(u => u.Id == user.Id);

                Assert.Equal(user.Id, savedUser.Id);
                Assert.Equal("testuser", savedUser.Username);
                Assert.Equal("Emma", savedUser.Name.FirstName);
                Assert.Equal("Jensen", savedUser.Name.LastName);
                Assert.Equal("emma@example.com", savedUser.Email.Value);
                Assert.Equal("Søndergade", savedUser.Address.Street);
                Assert.Equal("12", savedUser.Address.HouseNumber);
                Assert.Equal("7100", savedUser.Address.PostalCode);
                Assert.Equal("Vejle", savedUser.Address.City);
                Assert.Equal("Denmark", savedUser.Address.Country);
                Assert.Equal(
                    new DateOnly(2000, 5, 15),
                    savedUser.DateOfBirth);
            }
        }
        finally
        {
            // Delete only this test's temporary database.
            await using var context = new BoardGameDbContext(options);
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task User_CannotBeSaved_WithDuplicateUsername()
    {
        var options = CreateTestDatabaseOptions();

        try
        {
            await using var context = new BoardGameDbContext(options);
            await context.Database.MigrateAsync();

            var firstUser = new User(
                "sameusername",
                new Name("Emma", "Jensen"),
                new Email("emma@example.com"),
                new DateOnly(1995, 5, 15),
                new Address(
                    "Søndergade", "12", "7100", "Vejle", "Denmark"));

            var secondUser = new User(
                "sameusername",
                new Name("Mads", "Hansen"),
                new Email("mads@example.com"),
                new DateOnly(1998, 8, 20),
                new Address(
                    "Vestergade", "8", "6000", "Kolding", "Denmark"));

            context.Users.Add(firstUser);
            await context.SaveChangesAsync();

            context.Users.Add(secondUser);

            // The database must reject a duplicate username.
            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
        }
        finally
        {
            // Delete only this test's temporary database.
            await using var context = new BoardGameDbContext(options);
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task User_CannotBeSaved_WithDuplicateEmail()
    {
        var options = CreateTestDatabaseOptions();

        try
        {
            await using var context = new BoardGameDbContext(options);
            await context.Database.MigrateAsync();

            var firstUser = new User(
                "emma",
                new Name("Emma", "Jensen"),
                new Email("shared@example.com"),
                new DateOnly(1995, 5, 15),
                new Address(
                    "Søndergade", "12", "7100", "Vejle", "Denmark"));

            var secondUser = new User(
                "mads",
                new Name("Mads", "Hansen"),
                new Email("shared@example.com"),
                new DateOnly(1998, 8, 20),
                new Address(
                    "Vestergade", "8", "6000", "Kolding", "Denmark"));

            context.Users.Add(firstUser);
            await context.SaveChangesAsync();

            context.Users.Add(secondUser);

            // The database must reject a duplicate email address.
            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
        }
        finally
        {
            // Delete only this test's temporary database.
            await using var context = new BoardGameDbContext(options);
            await context.Database.EnsureDeletedAsync();
        }
    }

    // Create a unique SQL Server LocalDB database for each test.
    private static DbContextOptions<BoardGameDbContext>
        CreateTestDatabaseOptions()
    {
        var databaseName =
            $"UserPersistence_{Guid.NewGuid():N}";

        var connectionString =
            @"Server=(localdb)\MSSQLLocalDB;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;";

        return new DbContextOptionsBuilder<BoardGameDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }
}
