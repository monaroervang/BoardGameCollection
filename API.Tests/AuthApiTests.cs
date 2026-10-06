using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Shared.DTOs.Authentication.Requests;
using Shared.DTOs.Authentication.Responses;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace API.Tests;

public class AuthApiTests : IClassFixture<BoardGameApiFactory>
{
    private readonly BoardGameApiFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(BoardGameApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkAndToken()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var username = $"auth-{Guid.NewGuid():N}"; // Unique username to avoid conflicts with existing users
        const string password = "Test123!"; // Use a strong password for testing

        var user = await CreateUserAsync(
            username,
            password);

        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(username, result.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = new LoginRequest
        {
            Username = "user0001",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = new LoginRequest
        {
            Username = $"unknown-{Guid.NewGuid():N}",
            Password = "Test123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    // Create a user with a real password hash for authentication tests.
    private async Task<User> CreateUserAsync(
        string username,
        string password)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var user = new User(
            username: username,
            name: new Name("Auth", "Test"),
            email: new Email($"{username}@example.com"),
            dateOfBirth: new DateOnly(2000, 1, 1),
            address: new Address(
                "Test Street",
                "1",
                "7100",
                "Test City",
                "Denmark"));

        var passwordHash = passwordHasher.HashPassword(
            user,
            password);

        user.SetPasswordHash(passwordHash);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        // Send the request without an authentication token.
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturnCurrentUser()
    {
        await _factory.InitializeDatabaseAsync();

        var username = $"auth-{Guid.NewGuid():N}";
        const string password = "Test123!";

        var user = await CreateUserAsync(
            username,
            password);

        // Log in to obtain a valid JWT access token.
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = username,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(
            string.IsNullOrWhiteSpace(loginResult.Token));

        // Send the JWT as a Bearer token to the protected endpoint.
        using var request = new HttpRequestMessage( // Create a new HttpRequestMessage for the GET request so it doesn't conflict with other requests.
            HttpMethod.Get,
            "/api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(username, result.Username);
    }
}