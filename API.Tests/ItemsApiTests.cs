
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
    public async Task CreateItem_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = new CreateItemRequest
        {
            BoardGameId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/items",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_ShouldReturnCreated()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();
        var ownerId = await AuthenticateAsUserAsync(_client);

        var request = new CreateItemRequest
        {
            BoardGameId = boardGame.Id
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

        // The owner must be the authenticated user.
        Assert.Equal(ownerId, item.OwnerId);

        // Verify that the item was actually persisted.
        var getResponse = await _client.GetAsync(
            $"/api/items/{item.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedItem = await getResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(savedItem);
        Assert.Equal(item.Id, savedItem.Id);
        Assert.Equal(boardGame.Id, savedItem.BoardGameId);
        Assert.Equal(ownerId, savedItem.OwnerId);
    }

    [Fact]
    public async Task CreateItem_WithUnknownBoardGame_ShouldReturnNotFound()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        await AuthenticateAsUserAsync(_client);

        var request = new CreateItemRequest
        {
            BoardGameId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/items",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/items/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_ShouldReturnNoContent()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();
        var ownerId = await AuthenticateAsUserAsync(_client);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var item = await createResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);
        Assert.Equal(ownerId, item.OwnerId);

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/api/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/items/{item.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();

        // Create the item as the first user.
        var ownerId = await AuthenticateAsUserAsync(_client);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var item = await createResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);
        Assert.Equal(ownerId, item.OwnerId);

        // Authenticate as a different user.
        var otherUserId = await AuthenticateAsUserAsync(_client);

        Assert.NotEqual(ownerId, otherUserId);

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/api/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        // Verify that the item was not deleted.
        var getResponse = await _client.GetAsync(
            $"/api/items/{item.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedItem = await getResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(savedItem);
        Assert.Equal(ownerId, savedItem.OwnerId);
    }

    [Fact]
    public async Task GetMyCollection_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync(
            "/api/items/my-collection");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyCollection_ShouldReturnOnlyAuthenticatedUsersItems()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();

        // Create a physical copy belonging to the first user.
        var firstUserId = await AuthenticateAsUserAsync(_client);

        var firstCreateResponse = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(
            HttpStatusCode.Created,
            firstCreateResponse.StatusCode);

        var firstItem = await firstCreateResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(firstItem);
        Assert.Equal(firstUserId, firstItem.OwnerId);

        // Create another physical copy belonging to a different user.
        using var secondClient = _factory.CreateClient();

        var secondUserId = await AuthenticateAsUserAsync(secondClient);

        var secondCreateResponse = await secondClient.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(
            HttpStatusCode.Created,
            secondCreateResponse.StatusCode);

        var secondItem = await secondCreateResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(secondItem);
        Assert.Equal(secondUserId, secondItem.OwnerId);

        // Act: Fetch the first user's collection.
        var response = await _client.GetAsync(
            "/api/items/my-collection");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var collection = await response.Content
            .ReadFromJsonAsync<List<CollectionItemResponse>>();

        Assert.NotNull(collection);

        // The first user's copy must be included.
        Assert.Contains(
            collection,
            item => item.ItemId == firstItem.Id);

        // The second user's copy must not be included.
        Assert.DoesNotContain(
            collection,
            item => item.ItemId == secondItem.Id);

        // Verify that the other user receives their own copy instead.
        var secondResponse = await secondClient.GetAsync(
            "/api/items/my-collection");

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondCollection = await secondResponse.Content
            .ReadFromJsonAsync<List<CollectionItemResponse>>();

        Assert.NotNull(secondCollection);

        Assert.Contains(
            secondCollection,
            item => item.ItemId == secondItem.Id);

        Assert.DoesNotContain(
            secondCollection,
            item => item.ItemId == firstItem.Id);
    }

    [Fact]
    public async Task GetItemsByOwner_OldEndpoint_ShouldReturnNotFound()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var userId = await AuthenticateAsUserAsync(_client);

        // Act
        var response = await _client.GetAsync(
            $"/api/items/owner/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Create board game test data using a separate authenticated administrator client.
    // The main test client is used for the item API assertions.
    private async Task<BoardGameResponse> CreateBoardGameAsync()
    {
        using var adminClient = _factory.CreateClient();

        await AuthenticateAsync(adminClient, UserRole.Admin);

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

    // Create and authenticate a regular user.
    private Task<Guid> AuthenticateAsUserAsync(HttpClient client)
    {
        return AuthenticateAsync(client, UserRole.User);
    }

    // Create a test user with the specified role and authenticate the client.
    private async Task<Guid> AuthenticateAsync(
        HttpClient client,
        UserRole role)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var username =
            $"item-{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";

        const string password = "Test123!";

        var user = new User(
            username: username,
            name: new Name("Item", "Tester"),
            email: new Email($"{username}@example.com"),
            dateOfBirth: new DateOnly(1990, 1, 1),
            address: new Address(
                "Test Street",
                "10",
                "5000",
                "Odense",
                "Denmark"));

        var passwordHash = passwordHasher.HashPassword(
            user,
            password);

        user.SetPasswordHash(passwordHash);
        user.SetRole(role);

        context.Users.Add(user);
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

        return user.Id;
    }

    [Fact]
    public async Task ArchiveItem_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.PutAsync(
            $"/api/items/{Guid.NewGuid()}/archive",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveItem_ShouldArchiveItemWithoutDeletingIt()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();
        await AuthenticateAsUserAsync(_client);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var item = await createResponse.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);

        // Act
        var archiveResponse = await _client.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        // The item must still exist in the database.
        var getResponse = await _client.GetAsync(
            $"/api/items/{item.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // The owner's collection must show the archived status.
        var collection = await _client.GetFromJsonAsync<
            List<CollectionItemResponse>>("/api/items/my-collection");

        Assert.NotNull(collection);

        var archivedItem = Assert.Single(
    collection, entry => entry.ItemId == item.Id);

        Assert.True(archivedItem.IsArchived);
    }

    [Fact]
    public async Task ArchiveItem_WhenAlreadyArchived_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsCurrentUserAsync();

        var firstResponse = await _client.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        // Act
        var secondResponse = await _client.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ArchiveItem_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsCurrentUserAsync();

        // Authenticate as a different user.
        await AuthenticateAsUserAsync(_client);

        // Act
        var response = await _client.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveItem_WhenItemDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsUserAsync(_client);

        // Act
        var response = await _client.PutAsync(
            $"/api/items/{Guid.NewGuid()}/archive",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReactivateItem_ShouldMakeArchivedItemActive()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsCurrentUserAsync();

        var archiveResponse = await _client.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        // Act
        var reactivateResponse = await _client.PutAsync(
            $"/api/items/{item.Id}/reactivate",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, reactivateResponse.StatusCode);

        var collection = await _client.GetFromJsonAsync<
            List<CollectionItemResponse>>("/api/items/my-collection");

        Assert.NotNull(collection);

        var reactivatedItem = Assert.Single(
            collection, entry => entry.ItemId == item.Id);

        Assert.False(reactivatedItem.IsArchived);
    }

    [Fact]
    public async Task ReactivateItem_WhenAlreadyActive_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsCurrentUserAsync();

        // Act
        var response = await _client.PutAsync(
            $"/api/items/{item.Id}/reactivate",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReactivateItem_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsCurrentUserAsync();

        var archiveResponse = await _client.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        // Authenticate as a different user.
        await AuthenticateAsUserAsync(_client);

        // Act
        var response = await _client.PutAsync(
            $"/api/items/{item.Id}/reactivate",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReactivateItem_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.PutAsync(
            $"/api/items/{Guid.NewGuid()}/reactivate",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Create a physical copy owned by the authenticated test user.
    private async Task<ItemResponse> CreateItemAsCurrentUserAsync()
    {
        var boardGame = await CreateBoardGameAsync();

        await AuthenticateAsUserAsync(_client);

        var response = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await response.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);

        return item;
    }
}
