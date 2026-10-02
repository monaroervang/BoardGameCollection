
using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.BoardGames.Responses;
using Shared.DTOs.Common;

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


    [Fact]
    public async Task GetBoardGames_WithSearch_ShouldReturnMatchingTitles()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        var matchingGame = await CreateBoardGameWithTitleAsync(
            $"Catan {uniqueTerm}");

        await CreateBoardGameWithTitleAsync(
            $"Chess {Guid.NewGuid():N}");

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);

        var games = result.Items;

        Assert.NotNull(games);
        Assert.Single(games);
        Assert.Equal(matchingGame.Id, games[0].Id);
    }

    [Fact]
    public async Task GetBoardGames_WithSearch_ShouldReturnEmptyListWhenNoMatch()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        await CreateBoardGameAsync();

        var unknownTerm = Guid.NewGuid().ToString("N");

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?search={unknownTerm}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);

        var games = result.Items;

        Assert.NotNull(games);
        Assert.Empty(games);
    }

    [Fact]
    public async Task GetBoardGames_WithSearch_ShouldMatchPartialTitle()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        var matchingGame = await CreateBoardGameWithTitleAsync(
            $"Ticket to Ride {uniqueTerm}");

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);

        var games = result.Items;

        Assert.NotNull(games);
        Assert.Contains(games, game => game.Id == matchingGame.Id);
    }

    [Fact]
    public async Task GetBoardGames_WithWhitespaceSearch_ShouldReturnAllGames()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var firstGame = await CreateBoardGameAsync();
        var secondGame = await CreateBoardGameAsync();

        // Act
        var response = await _client.GetAsync(
            "/api/boardgames?search=%20%20%20&page=1&pageSize=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);

        var games = result.Items;

        Assert.NotNull(games);

        // Other seeded games may exist, so verify our two games are present.
        Assert.Contains(games, game => game.Id == firstGame.Id);
        Assert.Contains(games, game => game.Id == secondGame.Id);
    }

    // Creates a board game with a specific title through the API.
    private async Task<BoardGameResponse> CreateBoardGameWithTitleAsync(
        string title)
    {
        var request = CreateValidBoardGameRequest() with
        {
            Title = title
        };

        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var boardGame = await response.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(boardGame);

        return boardGame;
    }


    [Fact]
    public async Task GetBoardGames_WithGenreFilter_ShouldReturnMatchingGames()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var uniqueGenre = $"Genre-{Guid.NewGuid():N}";

        var request = CreateValidBoardGameRequest() with
        {
            Genre = uniqueGenre
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var matchingGame = await createResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(matchingGame);

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?genre={uniqueGenre}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(matchingGame.Id, result.Items[0].Id);
        Assert.Equal(uniqueGenre, result.Items[0].Genre);
    }
    [Fact]
    public async Task GetBoardGames_WithPlayersFilter_ShouldReturnSupportedGames()
    {
        await _factory.InitializeDatabaseAsync();

        var matchingGame = await CreateBoardGameAsync();

        // The existing helper creates games for 2-4 players.
        var response = await _client.GetAsync(
            "/api/boardgames?players=4&page=1&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);

        var games = result.Items;

        Assert.NotNull(games);
        Assert.Contains(games, game => game.Id == matchingGame.Id);

        Assert.All(games, game =>
        {
            Assert.True(game.MinPlayers <= 4);
            Assert.True(game.MaxPlayers >= 4);
        });
    }

    [Fact]
    public async Task GetBoardGames_WithAgeFilter_ShouldReturnSuitableGames()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync(
            "/api/boardgames?age=12&page=1&pageSize=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);

        // Every returned game must be suitable for a 12-year-old.
        Assert.All(result.Items, game =>
            Assert.True(game.MinimumAge <= 12));
    }

    [Fact]
    public async Task GetBoardGames_WithCombinedFilters_ShouldReturnMatchingGames()
    {
        await _factory.InitializeDatabaseAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        var matchingGame = await CreateBoardGameWithTitleAsync(
            $"Catan {uniqueTerm}");

        // Combine title search, genre, players and age.
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}" +
            "&genre=Strategy&players=4&age=12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);
        var games = result.Items;

        Assert.NotNull(games);
        Assert.Single(games);
        Assert.Equal(matchingGame.Id, games[0].Id);
    }

    [Theory]
    [InlineData("players=0")]
    [InlineData("players=-1")]
    [InlineData("age=-1")]
    public async Task GetBoardGames_WithInvalidFilters_ShouldReturnBadRequest(
        string query)
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync(
            $"/api/boardgames?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardGames_WithPagination_ShouldReturnRequestedPageSize() // Test for page size and total pages
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        await CreateBoardGameWithTitleAsync($"Game A {uniqueTerm}"); //Unique titles to ensure they are included in the search results
        await CreateBoardGameWithTitleAsync($"Game B {uniqueTerm}");
        await CreateBoardGameWithTitleAsync($"Game C {uniqueTerm}");

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}&page=1&pageSize=2"); // Requesting the first page with a page size of 2

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>(); // Deserialize the response into a PagedResponse object

        Assert.NotNull(result);
        Assert.Equal(1, result.Page); // Verify that the current page is 1
        Assert.Equal(2, result.PageSize); // Verify that the page size is 2
        Assert.Equal(3, result.TotalCount); // Verify that the total count of items is 3 (the number of games created)
        Assert.Equal(2, result.TotalPages); // Verify that the total number of pages is 2 (3 items with a page size of 2 results in 2 pages)
        Assert.Equal(2, result.Items.Count); // Verify that the number of items returned in the current page is 2 (the requested page size)
    }

    [Fact]
    public async Task GetBoardGames_WithPagination_ShouldReturnSecondPage() // Test for retrieving the second page of results
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        await CreateBoardGameWithTitleAsync($"Game A {uniqueTerm}");
        await CreateBoardGameWithTitleAsync($"Game B {uniqueTerm}");
        await CreateBoardGameWithTitleAsync($"Game C {uniqueTerm}");

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}&page=2&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public async Task GetBoardGames_WithInvalidPagination_ShouldReturnBadRequest( // Test for invalid pagination parameters
        int page,
        int pageSize)
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?page={page}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }



}
