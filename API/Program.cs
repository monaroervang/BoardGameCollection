using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using Persistence.Seeding;
using API.ExceptionHandling;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Read the database connection string from the application's configuration.
// In Development, the connection string is stored in User Secrets.
var connectionString = builder.Configuration 
    .GetConnectionString("BoardGameDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'BoardGameDatabase' was not found.");

// Register the DbContext so it can be injected into services.
// A scoped DbContext is shared within the same request or service scope.
builder.Services.AddDbContext<BoardGameDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the seeder as scoped so it shares the same DbContext
// within the scope used during application startup.
builder.Services.AddScoped<BoardGameDbSeeder>();

builder.Services.AddControllers(); // Add services for controllers to the container.

// Allow the Blazor WebAssembly client to call the API.
builder.Services.AddCors(options => //CORS is a security feature that restricts cross-origin HTTP requests initiated from scripts running in the browser.
{
    options.AddPolicy("WebClient", policy =>
    {
        policy
            .AllowAnyOrigin() //For now, allow any origin. In production, you should restrict this to the specific origins that need access.
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Register centralized exception handling for domain validation.
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Convert handled exceptions into appropriate HTTP responses.
app.UseExceptionHandler();

// Apply migrations and generate test data only in Development.
if (app.Environment.IsDevelopment())
{
    // Create a scope to resolve scoped services during startup.
    using var scope = app.Services.CreateScope();

    {
        var context = scope.ServiceProvider
        .GetRequiredService<BoardGameDbContext>();

        await context.Database.MigrateAsync();
    
        var seeder = scope.ServiceProvider
            .GetRequiredService<BoardGameDbSeeder>();

        await seeder.SeedAsync();
    }

    // Expose the OpenAPI document and enable Swagger UI in Development.
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "BoardGameCollection API v1");
    }
    );
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS for security.

// Apply the CORS policy before controller endpoints are executed.
app.UseCors("WebClient");

app.MapControllers(); // Map controller endpoints to the request pipeline.

app.Run();

// Allow integration tests to start the API.
public partial class Program { }
