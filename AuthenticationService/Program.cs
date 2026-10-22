using AuthenticationService.Contracts;
using AuthenticationService.Data;
using AuthenticationService.Models;
using AuthenticationService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' was not found.");

// Register the password hasher for dependency injection.
builder.Services.AddScoped<
    IPasswordHasher<string>,
    PasswordHasher<string>>();

builder.Services.AddScoped<JwtTokenService>(); // Register the JwtTokenService for dependency injection.

builder.Services.AddDbContext<AuthDbContext>(options => // Register the AuthDbContext for dependency injection.
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AuthDb"))); // Use SQL Server for the AuthDbContext.

var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok("AuthenticationService is running"));

app.MapGet("/health/database", async (AuthDbContext db) =>
{
    return await db.Database.CanConnectAsync()
        ? Results.Ok("Connected to SQL Server")
        : Results.Problem("Cannot connect to SQL Server");
});

// Register a user with a hashed password.
app.MapPost("/register", async Task<IResult> (
    UserLoginRequest request,
    IPasswordHasher<string> passwordHasher,
    AuthDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(
            "Username and password are required.");
    }

    var username = request.Username.Trim();

    if (username.Length > 100)
    {
        return Results.BadRequest(
            "Username must not exceed 100 characters.");
    }

    if (request.Password.Length < 8)
    {
        return Results.BadRequest(
            "Password must contain at least 8 characters.");
    }

    var passwordHash = passwordHasher.HashPassword(
        username,
        request.Password);

    if (await db.Users.AnyAsync(user => user.Username == username))
    {
        return Results.Conflict("Username is already taken.");
    }

    var user = new AuthUser
    {
        Username = username,
        PasswordHash = passwordHash,
        Role = "User"
    };

    db.Users.Add(user);

    try
    {
        await db.SaveChangesAsync();
        return Results.Ok("User registered!");
    }
    catch (DbUpdateException ex)
        when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
              && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
    {
        return Results.Conflict("Username is already taken.");
    }
});

// Verify the user's username and password.
app.MapPost("/login", async Task<IResult> (
    UserLoginRequest request,
    IPasswordHasher<string> passwordHasher,
    AuthDbContext db,
    JwtTokenService jwtTokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(
            "Username and password are required.");
    }

    var username = request.Username.Trim();

    var user = await db.Users
.SingleOrDefaultAsync(user => user.Username == username);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    var storedHash = user.PasswordHash;

    var verification = passwordHasher.VerifyHashedPassword(
            username,
            storedHash,
            request.Password);

    if (verification == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    var token = jwtTokenService.GenerateToken(
    user.Id,
    user.Username,
    user.Role);

    return Results.Ok(new LoginResponse(
        user.Id,
        user.Username,
        user.Role,
        token));

});

app.Run();