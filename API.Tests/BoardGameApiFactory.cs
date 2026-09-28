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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
            Environment.SetEnvironmentVariable(
        "ConnectionStrings__BoardGameDatabase",
        TestConnectionString);
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
        // Create a scope to access the test DbContext.
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        // Apply the real EF Core migrations to the test database.
        await context.Database.MigrateAsync();
    }
}