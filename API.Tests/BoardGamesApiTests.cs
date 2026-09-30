
using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.BoardGames.Responses;

namespace API.Tests;

public class BoardGamesApiTests : IClassFixture<BoardGameApiFactory>
{
    private readonly BoardGameApiFactory _factory;
    private readonly HttpClient _client;

    public BoardGamesApiTests(BoardGameApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBoardGames_ShouldReturnOk()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync("/api/boardgames");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoardGame_ShouldReturnCreated()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = CreateValidBoardGameRequest();

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var boardGame = await response.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(boardGame);
        Assert.NotEqual(Guid.Empty, boardGame.Id);
        Assert.Equal(request.Title, boardGame.Title);

        // Verify that SQL Server generated a RowVersion.
        Assert.NotNull(boardGame.RowVersion);
        Assert.Equal(8, boardGame.RowVersion.Length);

        // Verify that the board game was persisted.
        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{boardGame.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedBoardGame = await getResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(savedBoardGame);
        Assert.Equal(boardGame.Id, savedBoardGame.Id);
        Assert.Equal(request.Title, savedBoardGame.Title);
        Assert.Equal(boardGame.RowVersion, savedBoardGame.RowVersion);
    }

    [Fact]
    public async Task CreateBoardGame_WithInvalidPlayerCount_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = CreateValidBoardGameRequest() with
        {
            MinPlayers = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoardGame_ShouldReturnUpdatedBoardGame()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var createdBoardGame = await CreateBoardGameAsync();

        var updateRequest = CreateUpdateRequest(
            createdBoardGame,
            "Game After Update");

        // Act
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{createdBoardGame.Id}",
            updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedBoardGame = await updateResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(updatedBoardGame);
        Assert.Equal(createdBoardGame.Id, updatedBoardGame.Id);
        Assert.Equal(updateRequest.Title, updatedBoardGame.Title);
        Assert.Equal(updateRequest.Description, updatedBoardGame.Description);
        Assert.Equal(updateRequest.Genre, updatedBoardGame.Genre);

        // An update must generate a new RowVersion.
        Assert.Equal(8, updatedBoardGame.RowVersion.Length);
        Assert.False(
            createdBoardGame.RowVersion.SequenceEqual(
                updatedBoardGame.RowVersion));

        // Verify that the updated data was persisted.
        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{createdBoardGame.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedBoardGame = await getResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(savedBoardGame);
        Assert.Equal(updateRequest.Title, savedBoardGame.Title);
        Assert.Equal(
            updatedBoardGame.RowVersion,
            savedBoardGame.RowVersion);
    }


    [Fact]
    public async Task DeleteBoardGame_ShouldReturnNoContent()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();

        using var request = CreateDeleteRequest(boardGame);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{boardGame.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }


    [Fact]
    public async Task UpdateBoardGame_WithStaleRowVersion_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var originalBoardGame = await CreateBoardGameAsync();

        // Both users initially have the same version.
        var firstUpdate = CreateUpdateRequest(
            originalBoardGame,
            "First User Update");

        var secondUpdate = CreateUpdateRequest(
            originalBoardGame,
            "Second User Update");

        // The first user saves successfully.
        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{originalBoardGame.Id}",
            firstUpdate);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        // Act: The second user submits the outdated version.
        var secondResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{originalBoardGame.Id}",
            secondUpdate);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateBoardGame_WithStaleRowVersion_ShouldPreserveFirstUpdate()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var originalBoardGame = await CreateBoardGameAsync();

        var firstUpdate = CreateUpdateRequest(
            originalBoardGame,
            "First User Update");

        var secondUpdate = CreateUpdateRequest(
            originalBoardGame,
            "Second User Update");

        // The first user saves successfully.
        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{originalBoardGame.Id}",
            firstUpdate);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstResult = await firstResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(firstResult);

        // Act: The second user attempts to overwrite the first update.
        var secondResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{originalBoardGame.Id}",
            secondUpdate);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        // Retrieve the actual database state.
        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{originalBoardGame.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedBoardGame = await getResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        // Assert: The first user's changes must remain intact.
        Assert.NotNull(savedBoardGame);
        Assert.Equal("First User Update", savedBoardGame.Title);
        Assert.NotEqual("Second User Update", savedBoardGame.Title);

        Assert.Equal(
            firstResult.RowVersion,
            savedBoardGame.RowVersion);
    }

    // Creates a valid board game with a unique title.
    private static CreateBoardGameRequest CreateValidBoardGameRequest()
    {
        return new CreateBoardGameRequest
        {
            Title = $"API Test Game {Guid.NewGuid():N}",
            Description = "Created by an API integration test.",
            Genre = "Strategy",
            Publisher = "Test Publisher",
            Edition = "First Edition",
            ReleaseYear = 2025,
            MinPlayers = 2,
            MaxPlayers = 4,
            PlayingTimeMinutes = 60,
            MinimumAge = 12
        };
    }

    // Creates and returns a board game through the API.
    private async Task<BoardGameResponse> CreateBoardGameAsync()
    {
        var request = CreateValidBoardGameRequest();

        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var boardGame = await response.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(boardGame);
        Assert.Equal(8, boardGame.RowVersion.Length);

        return boardGame;
    }

    // Creates an update request using the version originally retrieved.
    private static UpdateBoardGameRequest CreateUpdateRequest(
        BoardGameResponse boardGame,
        string title)
    {
        return new UpdateBoardGameRequest
        {
            Title = title,
            Description = "Updated description.",
            Genre = "Adventure",
            Publisher = "Updated Publisher",
            Edition = "Second Edition",
            ReleaseYear = 2025,
            MinPlayers = 1,
            MaxPlayers = 5,
            PlayingTimeMinutes = 90,
            MinimumAge = 10,
            RowVersion = boardGame.RowVersion
        };
    }

    [Fact]
    public async Task DeleteBoardGame_WithoutRowVersion_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var boardGame = await CreateBoardGameAsync();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/boardgames/{boardGame.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // The board game must still exist.
        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{boardGame.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteBoardGame_WithStaleRowVersion_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var original = await CreateBoardGameAsync();

        // Another user updates the board game.
        var updateRequest = CreateUpdateRequest(
            original,
            "Updated Before Delete");

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{original.Id}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Attempt deletion using the original, outdated version.
        using var deleteRequest = CreateDeleteRequest(original);

        // Act
        var deleteResponse = await _client.SendAsync(deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        // The newer version must remain in the database.
        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{original.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var saved = await getResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(saved);
        Assert.Equal("Updated Before Delete", saved.Title);


    }

    private static HttpRequestMessage CreateDeleteRequest(
        BoardGameResponse boardGame)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/boardgames/{boardGame.Id}");

        // Send the version originally retrieved by the client.
        var encodedVersion = Convert.ToBase64String(
            boardGame.RowVersion);

        request.Headers.TryAddWithoutValidation(
            "If-Match",
            $"\"{encodedVersion}\"");

        return request;
    }


}
