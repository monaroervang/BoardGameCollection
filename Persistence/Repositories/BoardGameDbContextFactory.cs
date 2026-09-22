using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Persistence.Repositories;

public class BoardGameDbContextFactory
    : IDesignTimeDbContextFactory<BoardGameDbContext> //Provides a factory for creating instances of the BoardGameDbContext at design time
{
    public BoardGameDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder() //Create a new instance of the ConfigurationBuilder
            .AddUserSecrets<BoardGameDbContextFactory>() //Add user secrets to the configuration, using the BoardGameDbContextFactory type to locate the secrets
            .Build();

        var connectionString =
            configuration.GetConnectionString("BoardGameDatabase") //Get the connection string from the configuration, and if it's null, throw an exception
            ?? throw new InvalidOperationException(
                "Connection string 'BoardGameDatabase' was not found.");

        var optionsBuilder =
            new DbContextOptionsBuilder<BoardGameDbContext>();

        optionsBuilder.UseSqlServer(connectionString); //Configure the DbContextOptionsBuilder to use SQL Server with the specified connection string

        return new BoardGameDbContext(optionsBuilder.Options);
    }
}