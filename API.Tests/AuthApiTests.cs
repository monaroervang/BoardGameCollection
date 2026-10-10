using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Shared.DTOs.Authentication.Requests;
using Shared.DTOs.Authentication.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

        var username = $"auth-{Guid.NewGuid():N}";
        const string password = "Test123!";

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

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync(
            "/api/auth/me");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturnCurrentUser()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var username = $"auth-{Guid.NewGuid():N}";
        const string password = "Test123!";

        var user = await CreateUserAsync(
            username,
            password);

        var token = await LoginAsync(
            username,
            password);

        using var request = CreateAuthorizedGetRequest(
            "/api/auth/me",
            token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(username, result.Username);
        Assert.Equal("User", result.Role); // Assuming the default role is "User"
    }

    [Fact]
    public async Task GetAdminAccess_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync(
            "/api/auth/admin");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAdminAccess_WithUserRole_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var username = $"user-{Guid.NewGuid():N}";
        const string password = "Test123!";

        await CreateUserAsync(
            username,
            password,
            UserRole.User);

        var token = await LoginAsync(
            username,
            password);

        using var request = CreateAuthorizedGetRequest(
            "/api/auth/admin",
            token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAdminAccess_WithAdminRole_ShouldReturnOk()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var username = $"admin-{Guid.NewGuid():N}";
        const string password = "Test123!";

        await CreateUserAsync(
            username,
            password,
            UserRole.Admin);

        var token = await LoginAsync(
            username,
            password);

        using var request = CreateAuthorizedGetRequest(
            "/api/auth/admin",
            token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    // Create a user with a real password hash for authentication tests.
    private async Task<User> CreateUserAsync(
        string username,
        string password,
        UserRole role = UserRole.User)
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
        user.SetRole(role);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        return user;
    }

    // Log in and return the JWT issued by the API.
    private async Task<string> LoginAsync(
        string username,
        string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = username,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(
            string.IsNullOrWhiteSpace(result.Token));

        return result.Token;
    }

    // Create a GET request containing the JWT Bearer token.
    private static HttpRequestMessage CreateAuthorizedGetRequest(
        string requestUri,
        string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }
}