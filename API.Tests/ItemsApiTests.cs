
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Shared.DTOs.Authentication.Requests;
using Shared.DTOs.Authentication.Responses;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.BoardGames.Responses;
using Shared.DTOs.Items.Requests;
using Shared.DTOs.Items.Responses;
using Shared.DTOs.Users.Requests;
using Shared.DTOs.Users.Responses;
using Shared.DTOs.ValueObjects;

namespace API.Tests;

public class ItemsApiTests : IClassFixture<BoardGameApiFactory>
{
    private readonly BoardGameApiFactory _factory;
    private readonly HttpClient _client;

    public ItemsApiTests(BoardGameApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_ShouldReturnOk()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync("/api/items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_ShouldReturnCreated()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();
        var owner = await CreateUserAsync();

        var request = new CreateItemRequest
        {
            BoardGameId = boardGame.Id,
            OwnerId = owner.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/items",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await response.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(boardGame.Id, item.BoardGameId);
        Assert.Equal(owner.Id, item.OwnerId);

        // Verify that the item was actually persisted.
        var getResponse = await _client.GetAsync(
            $"/api/items/{item.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedItem = await getResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(savedItem);
        Assert.Equal(item.Id, savedItem.Id);
        Assert.Equal(boardGame.Id, savedItem.BoardGameId);
        Assert.Equal(owner.Id, savedItem.OwnerId);
    }

    [Fact]
    public async Task CreateItem_WithUnknownBoardGame_ShouldReturnNotFound()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var owner = await CreateUserAsync();

        var request = new CreateItemRequest
        {
            BoardGameId = Guid.NewGuid(),
            OwnerId = owner.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/items",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WithUnknownOwner_ShouldReturnNotFound()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();

        var request = new CreateItemRequest
        {
            BoardGameId = boardGame.Id,
            OwnerId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/items",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_ShouldReturnNoContent()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();
        var owner = await CreateUserAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id,
                OwnerId = owner.Id
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var item = await createResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/api/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/items/{item.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // Create board game test data using a separate authenticated administrator client.
    // The main test client remains unauthenticated for the item API assertions.
    private async Task<BoardGameResponse> CreateBoardGameAsync()
    {
        using var adminClient = _factory.CreateClient();

        await AuthenticateAsAdminAsync(adminClient);

        var request = new CreateBoardGameRequest
        {
            Title = $"Item Test Game {Guid.NewGuid():N}",
            Description = "Board game created for an item API test.",
            Genre = "Strategy",
            Publisher = "Test Publisher",
            Edition = null,
            ReleaseYear = 2025,
            MinPlayers = 2,
            MaxPlayers = 4,
            PlayingTimeMinutes = 60,
            MinimumAge = 12
        };

        var response = await adminClient.PostAsJsonAsync(
            "/api/boardgames",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var boardGame = await response.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(boardGame);

        return boardGame;
    }

    // Create an administrator and authenticate the supplied test client.
    private async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var username = $"item-admin-{Guid.NewGuid():N}";
        const string password = "Test123!";

        var admin = new User(
            username: username,
            name: new Name("Item", "Admin"),
            email: new Email($"{username}@example.com"),
            dateOfBirth: new DateOnly(2000, 1, 1),
            address: new Address(
                "Test Street",
                "1",
                "5000",
                "Odense",
                "Denmark"));

        var passwordHash = passwordHasher.HashPassword(
            admin,
            password);

        admin.SetPasswordHash(passwordHash);
        admin.SetRole(UserRole.Admin);

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = username,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);
    }

    private async Task<UserResponse> CreateUserAsync()
    {
        var request = new CreateUserRequest
        {
            Username = $"item-owner-{Guid.NewGuid():N}",

            Name = new NameDto
            {
                FirstName = "Item",
                LastName = "Owner"
            },

            Email = new EmailDto
            {
                Value = $"item-owner-{Guid.NewGuid():N}@example.com"
            },

            DateOfBirth = new DateOnly(1990, 1, 1),

            Address = new AddressDto
            {
                Street = "Test Street",
                HouseNumber = "10",
                PostalCode = "5000",
                City = "Odense",
                Country = "Denmark"
            }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/users",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(user);

        return user;
    }
}
