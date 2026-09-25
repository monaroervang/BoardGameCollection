
using Domain.Models;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;

namespace Persistence.Tests;

public class LoanPersistenceTests
{
    [Fact]
    public async Task Loan_CanBeSavedAndLoaded_WithValidRelationships()
    {
        // Keep the in-memory database alive across DbContext instances.
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BoardGameDbContext>()
            .UseSqlite(connection)
            .Options;

        var owner = new User(
            "owner",
            new Name("Emma", "Jensen"),
            new Email("owner@example.com"),
            new DateOnly(1995, 5, 15),
            new Address("Søndergade", "12", "7100", "Vejle", "Denmark"));

        var borrower = new User(
            "borrower",
            new Name("Mads", "Hansen"),
            new Email("borrower@example.com"),
            new DateOnly(1998, 8, 20),
            new Address("Vestergade", "8", "6000", "Kolding", "Denmark"));

        var boardGame = new BoardGame(
            "Catan",
            "A strategy game about trading and building.",
            "Strategy",
            "Kosmos",
            null,
            1995,
            3,
            4,
            90,
            10);

        // Create a physical copy owned by the existing user.
        var item = new Item(boardGame.Id, owner.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var loan = new Loan(
            today,
            today.AddDays(14),
            item.Id,
            borrower.Id);

        // Save all related entities.
        await using (var context = new BoardGameDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            context.Users.AddRange(owner, borrower);
            context.BoardGames.Add(boardGame);
            context.Items.Add(item);
            context.Loans.Add(loan);

            await context.SaveChangesAsync();
        }

        // Load the relationships using a new DbContext.
        await using (var context = new BoardGameDbContext(options))
        {
            var savedLoan = await context.Loans
                .AsNoTracking()
                .SingleAsync(l => l.Id == loan.Id);

            var savedItem = await context.Items
                .AsNoTracking()
                .SingleAsync(i => i.Id == savedLoan.ItemId);

            Assert.Equal(item.Id, savedLoan.ItemId);
            Assert.Equal(borrower.Id, savedLoan.BorrowerId);
            Assert.Equal(boardGame.Id, savedItem.BoardGameId);
            Assert.Equal(owner.Id, savedItem.OwnerId);

            Assert.False(savedLoan.IsReturned);
            Assert.False(savedLoan.IsOverdue);
        }
    }

    [Fact]
    public async Task Item_CannotBeSaved_WhenOwnerDoesNotExist()
    {
        // Keep the in-memory database alive while testing.
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BoardGameDbContext>()
            .UseSqlite(connection)
            .Options;

        var boardGame = new BoardGame(
            "Catan",
            "A strategy game about trading and building.",
            "Strategy",
            "Kosmos",
            null,
            1995,
            3,
            4,
            90,
            10);

        // Use a non-existing owner ID to test the foreign key constraint.
        var item = new Item(
            boardGame.Id,
            Guid.CreateVersion7());

        await using var context = new BoardGameDbContext(options);

        await context.Database.EnsureCreatedAsync();

        // The board game exists, but the owner does not.
        context.BoardGames.Add(boardGame);
        context.Items.Add(item);

        // The database must reject the invalid foreign key.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }
}
