using AuthenticationService.Contracts;
using Microsoft.AspNetCore.Identity;
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

var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok("AuthenticationService is running"));

app.MapGet("/health/database", async () =>
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    return Results.Ok("Connected to MySQL");
});

// Register a user with a hashed password.
app.MapPost("/register", async Task<IResult> (
    UserLoginRequest request,
    IPasswordHasher<string> passwordHasher) =>
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

    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    using var command = new MySqlCommand(
        """
        INSERT INTO Users (Username, PasswordHash)
        VALUES (@username, @passwordHash);
        """,
        connection);

    command.Parameters.AddWithValue("@username", username);
    command.Parameters.AddWithValue("@passwordHash", passwordHash);

    try
    {
        await command.ExecuteNonQueryAsync();

        return Results.Ok("User registered!");
    }
    catch (MySqlException exception) when (exception.Number == 1062)
    {
        return Results.Conflict("Username is already taken.");
    }
});

// Verify the user's username and password.
app.MapPost("/login", async Task<IResult> (
        UserLoginRequest request,
        IPasswordHasher<string> passwordHasher) =>
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(
                "Username and password are required.");
        }

        var username = request.Username.Trim();

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand(
            """
        SELECT PasswordHash
        FROM Users
        WHERE Username = @username;
        """,
            connection);

        command.Parameters.AddWithValue("@username", username);

        var result = await command.ExecuteScalarAsync();

        if (result is not string storedHash)
        {
            return Results.Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(
            username,
            storedHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        return Results.Ok("Login successful!");
    
});

app.Run();