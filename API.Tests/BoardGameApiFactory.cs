using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence.Repositories;

namespace API.Tests;

public class BoardGameApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=BoardGameCollectionApiTests;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly SemaphoreSlim _databaseLock = new(1, 1);
    private bool _databaseInitialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure values required by the API test host.
        builder.UseSetting(
            "ConnectionStrings:BoardGameDatabase",
            TestConnectionString);

        builder.UseSetting(
            "Jwt:Key",
            "BoardGameCollection-IntegrationTests-JWT-Key-2026!");

        builder.UseSetting(
            "Jwt:Issuer",
            "BoardGameCollection.API");

        builder.UseSetting(
            "Jwt:Audience",
            "BoardGameCollection.Web");

        // Use Production so development seeding does not run during API tests.
        builder.UseEnvironment("Production");

        builder.ConfigureTestServices(services =>
        {
            // Remove the DbContext configuration registered by the API.
            services.RemoveAll<DbContextOptions<BoardGameDbContext>>();

            // Register the DbContext with a separate test database.
            services.AddDbContext<BoardGameDbContext>(options =>
                options.UseSqlServer(TestConnectionString));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await _databaseLock.WaitAsync();

        try
        {
            // The database only needs to be migrated once per test factory.
            if (_databaseInitialized)
                return;

            using var scope = Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<BoardGameDbContext>();

            // Apply the real EF Core migrations to the test database.
            await context.Database.MigrateAsync();

            _databaseInitialized = true;
        }
        finally
        {
            _databaseLock.Release();
        }
    }
}