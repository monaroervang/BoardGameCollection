using API.Authentication;
using API.ExceptionHandling;
using API.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence.Repositories;
using Persistence.Seeding;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    // Add JWT Bearer authentication to the OpenAPI document.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddOpenApi(options =>
{
    // Add JWT Bearer authentication to the OpenAPI document.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

    // Mark authorized endpoints as requiring a Bearer token.
    options.AddOperationTransformer<AuthOperationTransformer>();
});

// Read the database connection string from the application's configuration.
// In Development, the connection string is stored in User Secrets.
var connectionString = builder.Configuration
    .GetConnectionString("BoardGameDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'BoardGameDatabase' was not found.");

// Read JWT configuration.
// The signing key is stored in User Secrets during development.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key was not found.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not found.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not found.");

// Register the DbContext so it can be injected into services.
// A scoped DbContext is shared within the same request or service scope.
builder.Services.AddDbContext<BoardGameDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the seeder as scoped so it shares the same DbContext
// within the scope used during application startup.
builder.Services.AddScoped<BoardGameDbSeeder>();

// Register development authentication seeding.
builder.Services.AddScoped<DevelopmentAuthenticationSeeder>();

// Add services for controllers to the container.
builder.Services.AddControllers();

// Register ASP.NET Core's password hasher for user authentication.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Configure JWT bearer authentication.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            // Tokens expire exactly at their expiration time.
            ClockSkew = TimeSpan.Zero
        };
    });
// Register the service responsible for creating JWT access tokens.
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthorization();

// Allow the Blazor WebAssembly client to call the API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        // Development policy.
        // Restrict this to the specific client origin in production.
        policy
            .AllowAnyOrigin()
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

    var context = scope.ServiceProvider
        .GetRequiredService<BoardGameDbContext>();

    await context.Database.MigrateAsync();

    var seeder = scope.ServiceProvider
        .GetRequiredService<BoardGameDbSeeder>();

    await seeder.SeedAsync();

    // Give seeded development users hashed passwords.
    var authenticationSeeder = scope.ServiceProvider
        .GetRequiredService<DevelopmentAuthenticationSeeder>();

    await authenticationSeeder.SeedAsync();

    // Expose the OpenAPI document and enable Swagger UI in Development.
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "BoardGameCollection API v1");
    });
}

app.UseHttpsRedirection();

// Enable CORS for the Blazor WebAssembly client.
app.UseCors("WebClient");

// Validate authentication before authorization.
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes to the endpoints defined in the controllers.
app.MapControllers();

app.Run();

// Allow integration tests to start the API.
public partial class Program { }