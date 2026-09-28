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
        _client = factory.CreateClient(); // Create an HttpClient to send requests to the test server.
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

        var request = new CreateBoardGameRequest
        {
            Title = "API Test Game",
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

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var boardGame =
            await response.Content.ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(boardGame);
        Assert.NotEqual(Guid.Empty, boardGame.Id); // Ensure the ID is not empty.
        Assert.Equal(request.Title, boardGame.Title);

        // Act
        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{boardGame.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedBoardGame =
            await getResponse.Content.ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(savedBoardGame);
        Assert.Equal(boardGame.Id, savedBoardGame.Id);
        Assert.Equal(request.Title, savedBoardGame.Title);
    }

    [Fact]
    public async Task CreateBoardGame_WithInvalidPlayerCount_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = new CreateBoardGameRequest
        {
            Title = "Invalid Game",
            Description = "This game has an invalid player count.",
            Genre = "Strategy",
            Publisher = "Test Publisher",
            Edition = null,
            ReleaseYear = 2025,
            MinPlayers = 0, // Invalid minimum players
            MaxPlayers = 4,
            PlayingTimeMinutes = 60,
            MinimumAge = 12
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

        var createRequest = new CreateBoardGameRequest
        {
            Title = "Game Before Update",
            Description = "Original description.",
            Genre = "Strategy",
            Publisher = "Test Publisher",
            Edition = null,
            ReleaseYear = 2024,
            MinPlayers = 2,
            MaxPlayers = 4,
            PlayingTimeMinutes = 60,
            MinimumAge = 12
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/boardgames",
            createRequest);

        var createdBoardGame =
            await createResponse.Content.ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(createdBoardGame);

        var updateRequest = new UpdateBoardGameRequest
        {
            Title = "Game After Update",
            Description = "Updated description.",
            Genre = "Adventure",
            Publisher = "Updated Publisher",
            Edition = "Second Edition",
            ReleaseYear = 2025,
            MinPlayers = 1,
            MaxPlayers = 5,
            PlayingTimeMinutes = 90,
            MinimumAge = 10
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync( //PutAsJsonAsync sends a PUT request with JSON content to the specified URI.
            $"/api/boardgames/{createdBoardGame.Id}",
            updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedBoardGame =
            await updateResponse.Content.ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(updatedBoardGame);
        Assert.Equal(createdBoardGame.Id, updatedBoardGame.Id);
        Assert.Equal(updateRequest.Title, updatedBoardGame.Title);
        Assert.Equal(updateRequest.Description, updatedBoardGame.Description);
        Assert.Equal(updateRequest.Genre, updatedBoardGame.Genre);
    }

    [Fact]
    public async Task DeleteBoardGame_ShouldReturnNoContent()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = new CreateBoardGameRequest
        {
            Title = "Game To Delete",
            Description = "This game will be deleted.",
            Genre = "Strategy",
            Publisher = "Test Publisher",
            Edition = null,
            ReleaseYear = 2025,
            MinPlayers = 2,
            MaxPlayers = 4,
            PlayingTimeMinutes = 60,
            MinimumAge = 12
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        var createdBoardGame =
            await createResponse.Content.ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(createdBoardGame);

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/api/boardgames/{createdBoardGame.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode); //NoContent indicates that the request was successful, but there is no content to return.

        var getResponse = await _client.GetAsync(
            $"/api/boardgames/{createdBoardGame.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode); //NotFound indicates that the requested resource could not be found.
    }
}