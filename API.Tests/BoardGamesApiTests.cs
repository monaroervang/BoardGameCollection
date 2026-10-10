using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Shared.DTOs.Authentication.Requests;
using Shared.DTOs.Authentication.Responses;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.BoardGames.Responses;
using Shared.DTOs.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
        await AuthenticateAsAdminAsync();

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
    public async Task CreateBoardGame_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Ensure that this request is sent without authentication.
        _client.DefaultRequestHeaders.Authorization = null;

        var request = CreateValidBoardGameRequest();

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateBoardGame_WithUserRole_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsUserAsync();

        var request = CreateValidBoardGameRequest();

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/boardgames",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateBoardGame_WithInvalidPlayerCount_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

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
        await AuthenticateAsAdminAsync();

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
    public async Task UpdateBoardGame_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var boardGame = await CreateBoardGameAsync();

        var updateRequest = CreateUpdateRequest(
            boardGame,
            "Unauthorized Update");

        // Remove authentication after creating the test data.
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/boardgames/{boardGame.Id}",
            updateRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoardGame_WithUserRole_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var boardGame = await CreateBoardGameAsync();

        var updateRequest = CreateUpdateRequest(
            boardGame,
            "Forbidden Update");

        // Replace the administrator token with a regular user token.
        await AuthenticateAsUserAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/boardgames/{boardGame.Id}",
            updateRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoardGame_WithStaleRowVersion_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var originalBoardGame = await CreateBoardGameAsync();

        // Both clients initially have the same version.
        var firstUpdate = CreateUpdateRequest(
            originalBoardGame,
            "First User Update");

        var secondUpdate = CreateUpdateRequest(
            originalBoardGame,
            "Second User Update");

        // The first update saves successfully.
        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{originalBoardGame.Id}",
            firstUpdate);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        // Act: Submit the outdated version.
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
        await AuthenticateAsAdminAsync();

        var originalBoardGame = await CreateBoardGameAsync();

        var firstUpdate = CreateUpdateRequest(
            originalBoardGame,
            "First User Update");

        var secondUpdate = CreateUpdateRequest(
            originalBoardGame,
            "Second User Update");

        // The first update saves successfully.
        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/boardgames/{originalBoardGame.Id}",
            firstUpdate);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstResult = await firstResponse.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(firstResult);

        // Act: Attempt to overwrite the first update with a stale version.
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

        // The first update must remain intact.
        Assert.NotNull(savedBoardGame);
        Assert.Equal("First User Update", savedBoardGame.Title);
        Assert.NotEqual("Second User Update", savedBoardGame.Title);

        Assert.Equal(
            firstResult.RowVersion,
            savedBoardGame.RowVersion);
    }

    [Fact]
    public async Task DeleteBoardGame_ShouldReturnNoContent()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

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
    public async Task DeleteBoardGame_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var boardGame = await CreateBoardGameAsync();

        using var request = CreateDeleteRequest(boardGame);

        // Remove authentication after creating the test data.
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoardGame_WithUserRole_ShouldReturnForbidden()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var boardGame = await CreateBoardGameAsync();

        using var request = CreateDeleteRequest(boardGame);

        // Replace the administrator token with a regular user token.
        await AuthenticateAsUserAsync();

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoardGame_WithoutRowVersion_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

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
        await AuthenticateAsAdminAsync();

        var original = await CreateBoardGameAsync();

        // Update the board game to generate a new RowVersion.
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

    [Fact]
    public async Task GetBoardGames_WithSearch_ShouldReturnMatchingTitles()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

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
        await AuthenticateAsAdminAsync();

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
        await AuthenticateAsAdminAsync();

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
        await AuthenticateAsAdminAsync();

        var firstGame = await CreateBoardGameAsync();
        var secondGame = await CreateBoardGameAsync();

        const int pageSize = 100;

        // Compare whitespace search with an unfiltered request.
        var whitespaceResponse = await _client.GetAsync(
            $"/api/boardgames?search=%20%20%20&page=1&pageSize={pageSize}");

        var unfilteredResponse = await _client.GetAsync(
            $"/api/boardgames?page=1&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.OK, whitespaceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unfilteredResponse.StatusCode);

        var whitespaceResult = await whitespaceResponse.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        var unfilteredResult = await unfilteredResponse.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(whitespaceResult);
        Assert.NotNull(unfilteredResult);

        // Whitespace must not change the total number of matching games.
        Assert.Equal(
            unfilteredResult.TotalCount,
            whitespaceResult.TotalCount);

        // Whitespace must not change the first page of results.
        Assert.Equal(
            unfilteredResult.Items.Select(game => game.Id),
            whitespaceResult.Items.Select(game => game.Id));

        // Verify that both newly created games exist in the database.
        var firstGameResponse = await _client.GetAsync(
            $"/api/boardgames/{firstGame.Id}");

        var secondGameResponse = await _client.GetAsync(
            $"/api/boardgames/{secondGame.Id}");

        Assert.Equal(HttpStatusCode.OK, firstGameResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondGameResponse.StatusCode);
    }

    [Fact]
    public async Task GetBoardGames_WithGenreFilter_ShouldReturnMatchingGames()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

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
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var matchingGame = await CreateBoardGameAsync();

        const int pageSize = 100;
        var page = 1;
        var foundMatchingGame = false;

        // Act and assert
        while (true)
        {
            var response = await _client.GetAsync(
                $"/api/boardgames?players=4&page={page}&pageSize={pageSize}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            var games = result.Items;

            // Every returned game must support four players.
            Assert.All(games, game =>
            {
                Assert.True(game.MinPlayers <= 4);
                Assert.True(game.MaxPlayers >= 4);
            });

            if (games.Any(game => game.Id == matchingGame.Id))
            {
                foundMatchingGame = true;
            }

            // Stop after the final page.
            if (games.Count < pageSize)
            {
                break;
            }

            page++;
        }

        // The game created by this test must be included in the filtered results.
        Assert.True(
            foundMatchingGame,
            "The newly created game was not found in the players=4 results.");
    }



    [Fact]
    public async Task GetBoardGames_WithAgeFilter_ShouldReturnSuitableGames()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        const int pageSize = 100;
        var page = 1;

        // Act and assert
        while (true)
        {
            var response = await _client.GetAsync(
                $"/api/boardgames?age=12&page={page}&pageSize={pageSize}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            // Every returned game must be suitable for a 12-year-old.
            Assert.All(result.Items, game =>
                Assert.True(game.MinimumAge <= 12));

            // Stop after the final page.
            if (result.Items.Count < pageSize)
            {
                break;
            }

            page++;
        }
    }


    [Fact]
    public async Task GetBoardGames_WithCombinedFilters_ShouldReturnMatchingGames()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        var matchingGame = await CreateBoardGameWithTitleAsync(
            $"Catan {uniqueTerm}");

        // Act
        // Combine title search, genre, players and age.
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}" +
            "&genre=Strategy&players=4&age=12");

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

    [Theory]
    [InlineData("players=0")]
    [InlineData("players=-1")]
    [InlineData("age=-1")]
    public async Task GetBoardGames_WithInvalidFilters_ShouldReturnBadRequest(
        string query)
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardGames_WithPagination_ShouldReturnRequestedPageSize()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

        var uniqueTerm = Guid.NewGuid().ToString("N");

        await CreateBoardGameWithTitleAsync($"Game A {uniqueTerm}");
        await CreateBoardGameWithTitleAsync($"Game B {uniqueTerm}");
        await CreateBoardGameWithTitleAsync($"Game C {uniqueTerm}");

        // Act
        var response = await _client.GetAsync(
            $"/api/boardgames?search={uniqueTerm}&page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<BoardGameResponse>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetBoardGames_WithPagination_ShouldReturnSecondPage()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        await AuthenticateAsAdminAsync();

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
    public async Task GetBoardGames_WithInvalidPagination_ShouldReturnBadRequest(
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
    // The client must already be authenticated as an administrator.
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

    // Creates a board game with a specific title through the API.
    // The client must already be authenticated as an administrator.
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

    // Creates a DELETE request containing the version originally retrieved.
    private static HttpRequestMessage CreateDeleteRequest(
        BoardGameResponse boardGame)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/boardgames/{boardGame.Id}");

        var encodedVersion = Convert.ToBase64String(
            boardGame.RowVersion);

        request.Headers.TryAddWithoutValidation(
            "If-Match",
            $"\"{encodedVersion}\"");

        return request;
    }

    // Create a user with the specified role and configure the client with a valid JWT.
    private async Task AuthenticateAsync(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var username = $"auth-{Guid.NewGuid():N}";
        const string password = "Test123!";

        var user = new User(
            username: username,
            name: new Name("API", "Test User"),
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

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);
    }

    // Authenticate the client as an administrator.
    private Task AuthenticateAsAdminAsync()
    {
        return AuthenticateAsync(UserRole.Admin);
    }

    // Authenticate the client as a regular user.
    private Task AuthenticateAsUserAsync()
    {
        return AuthenticateAsync(UserRole.User);
    }
}