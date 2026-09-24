
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using Persistence.Seeding;

namespace Persistence.Tests;

public class BoardGameDbSeederTests
{
    [Fact]
    public async Task Seeder_CanPopulateSqlServerDatabase()
    {
        // Use a unique test database to protect development data.
        var databaseName =
            $"BoardGameCollectionTest_{Guid.NewGuid():N}";

        var connectionString =
            "Server=(localdb)\\MSSQLLocalDB;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<BoardGameDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new BoardGameDbContext(options);

        try
        {
            // Apply the actual SQL Server migrations.
            await context.Database.MigrateAsync();

            var seeder = new BoardGameDbSeeder(context);

            await seeder.SeedAsync();

            var users = await context.Users.ToDictionaryAsync(u => u.Id); // Load all users into a dictionary for easy lookup by user ID
            var items = await context.Items.ToDictionaryAsync(i => i.Id); //
            var boardGames = await context.BoardGames.ToDictionaryAsync(g => g.Id);
            var loans = await context.Loans.ToListAsync();

            foreach (var loan in loans)
            {
                var item = items[loan.ItemId];
                var borrower = users[loan.BorrowerId];
                var boardGame = boardGames[item.BoardGameId];

                // A user cannot borrow their own game.
                Assert.NotEqual(item.OwnerId, borrower.Id);

                // The borrower must meet the game's minimum age.
                Assert.True(
                    borrower.DateOfBirth <= loan.LoanDate.AddYears(-boardGame.MinimumAge));

                // Loan dates must be valid.
                Assert.True(loan.DueDate > loan.LoanDate);

                if (loan.ReturnDate.HasValue)
                {
                    Assert.True(loan.ReturnDate.Value >= loan.LoanDate);
                }
            }

            // Each item has at most one seeded loan.
            Assert.Equal(loans.Count, loans.Select(l => l.ItemId).Distinct().Count());

            Assert.Equal(100, await context.BoardGames.CountAsync());
            Assert.Equal(50, await context.Users.CountAsync());
            Assert.Equal(250, await context.Items.CountAsync());

            // Some loans may be skipped if no eligible borrower exists.
            Assert.InRange(await context.Loans.CountAsync(), 1, 150);

            // Running the seeder again must not duplicate data.
            var loanCount = await context.Loans.CountAsync();

            await seeder.SeedAsync();

            Assert.Equal(100, await context.BoardGames.CountAsync());
            Assert.Equal(50, await context.Users.CountAsync());
            Assert.Equal(250, await context.Items.CountAsync());
            Assert.Equal(loanCount, await context.Loans.CountAsync());
        }
        finally
        {
            // Only delete the uniquely named database created by this test.
            await context.Database.EnsureDeletedAsync();
        }
    }
}