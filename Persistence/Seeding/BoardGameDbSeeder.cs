
using Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Seeding.Fakers;
using Bogus;
using Domain.Models;

namespace Persistence.Seeding;

public class BoardGameDbSeeder
{
    private readonly BoardGameDbContext _context;

    public BoardGameDbSeeder(BoardGameDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Seed entities in dependency order.
        await SeedBoardGamesAsync();
        await SeedUsersAsync();
        await SeedItemsAsync();
        await SeedLoansAsync();
    }


    private async Task SeedBoardGamesAsync()
    {
        // Avoid inserting the same development data twice.
        if (await _context.BoardGames.AnyAsync())
            return;

        var faker = new Faker("en"); //Create a new instance of the Faker class, specifying the locale as "en" (English) to generate data in English format
        faker.Random = new Randomizer(12345); //Set the Random property of the Faker instance to a new Randomizer with a seed value of 12345, ensuring that the generated data is deterministic and reproducible

        var boardGames = new List<BoardGame>();

        for (var i = 0; i < 100; i++)
        {
            var minPlayers = faker.Random.Int(1, 4);
            var maxPlayers = faker.Random.Int(minPlayers, 8);

            var boardGame = new BoardGame(
                title: faker.Commerce.ProductName(),
                description: faker.Lorem.Sentences(2),
                genre: faker.PickRandom(
                    "Strategy",
                    "Family",
                    "Cooperative",
                    "Party",
                    "Abstract",
                    "Card game"),
                publisher: faker.Company.CompanyName(),
                edition: null,
                releaseYear: faker.Random.Int(1990, DateTime.Today.Year),
                minPlayers: minPlayers,
                maxPlayers: maxPlayers,
                playingTimeMinutes: faker.Random.Int(1, 8) * 15,
                minimumAge: faker.PickRandom(6, 8, 10, 12, 14, 16, 18)
            );

            boardGames.Add(boardGame);
        }

        await _context.BoardGames.AddRangeAsync(boardGames); //Add the generated board games to the DbContext for insertion into the database
        await _context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        // Avoid inserting duplicate users when seeding again.
        if (await _context.Users.AnyAsync())
            return;

        var users = UserFaker.Generate(50);

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
    }


    private async Task SeedItemsAsync()
    {
        // Avoid inserting duplicate development data.
        if (await _context.Items.AnyAsync())
            return;

        var users = await _context.Users.ToListAsync();
        var boardGames = await _context.BoardGames.ToListAsync();

        if (users.Count == 0 || boardGames.Count == 0)
            throw new InvalidOperationException(
                "Users and board games must be seeded before items.");

        var faker = new Faker("en");
        faker.Random = new Randomizer(54321); //Different seed value to ensure that the generated data is deterministic and reproducible, but different from the previous faker instance

        var items = new List<Item>();

        // Generate 250 physical copies owned by different users.
        for (var i = 0; i < 250; i++)
        {
            var owner = faker.PickRandom(users);
            var boardGame = faker.PickRandom(boardGames);

            var item = new Item
            {
                OwnerId = owner.Id,
                BoardGameId = boardGame.Id
            };

            items.Add(item);
        }

        await _context.Items.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }


    private async Task SeedLoansAsync()
    {
        // Avoid inserting duplicate development data.
        if (await _context.Loans.AnyAsync())
            return;

        var users = await _context.Users.ToListAsync();
        var items = await _context.Items.ToListAsync();
        var boardGames = await _context.BoardGames.ToListAsync();

        if (users.Count < 2 || items.Count < 150)
            throw new InvalidOperationException(
                "At least two users and 150 items are required.");

        var faker = new Faker("en");
        faker.Random = new Randomizer(98765);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var loans = new List<Loan>();

        // Each item is used once, preventing overlapping loans.
        var selectedItems = faker.Random
            .Shuffle(items)
            .Take(150)
            .ToList();

        foreach (var item in selectedItems)
        {
            var boardGame = boardGames.Single(
                game => game.Id == item.BoardGameId);

            // A borrower must not own the item and must meet
            // the board game's minimum age requirement.
            var eligibleBorrowers = users
                .Where(user =>
                    user.Id != item.OwnerId &&
                    user.DateOfBirth <= today.AddYears(-boardGame.MinimumAge))
                .ToList();

            if (eligibleBorrowers.Count == 0)
                continue;

            var borrower = faker.PickRandom(eligibleBorrowers);
            var status = faker.Random.Int(1, 100);

            Loan loan;
            // Seed data uses the parameterless constructor to create historical loans.
            // The regular constructor rejects loan dates in the past.
            // This is intentional and only used for development data.

            if (status <= 65)
            {
                // Returned historical loan.
                var daysAgo = faker.Random.Int(30, 365);
                var loanDate = today.AddDays(-daysAgo);
                var dueDate = loanDate.AddDays(14);
                var returnDate = loanDate.AddDays(
                    faker.Random.Int(1, 21));

                loan = new Loan
                {
                    LoanDate = loanDate,
                    DueDate = dueDate,
                    ItemId = item.Id,
                    BorrowerId = borrower.Id
                };

                loan.ReturnLoan(returnDate);
            }
            else if (status <= 85)
            {
                // Active loan that is not overdue.
                var loanDate = today.AddDays(
                    -faker.Random.Int(0, 10));

                loan = new Loan
                {
                    LoanDate = loanDate,
                    DueDate = today.AddDays(
                        faker.Random.Int(1, 14)),
                    ItemId = item.Id,
                    BorrowerId = borrower.Id
                };
            }
            else
            {
                // Active overdue loan.
                var loanDate = today.AddDays(
                    -faker.Random.Int(20, 60));

                loan = new Loan
                {
                    LoanDate = loanDate,
                    DueDate = loanDate.AddDays(14),
                    ItemId = item.Id,
                    BorrowerId = borrower.Id
                };
            }

            loans.Add(loan);
        }

        await _context.Loans.AddRangeAsync(loans);
        await _context.SaveChangesAsync();
    }
}