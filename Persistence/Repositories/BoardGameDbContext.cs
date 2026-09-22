using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class BoardGameDbContext : DbContext //Represents the database context for the board game application
{
    public DbSet<BoardGame> BoardGames { get; set; } //Represents the collection of board games in the database
    public DbSet<Item> Items { get; set; } //Represents the collection of items in the database
    public DbSet<Loan> Loans { get; set; } //Represents the collection of loans in the database
    public DbSet<User> Users { get; set; } //Represents the collection of users in the database

    public BoardGameDbContext(
        DbContextOptions<BoardGameDbContext> options) //Represents the options for configuring the database context
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) //Configures the model for the database context
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BoardGameDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}