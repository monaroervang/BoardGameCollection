
using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.BoardGames.Responses;
using Shared.DTOs.Items.Requests;
using Shared.DTOs.Items.Responses;
using Shared.DTOs.Loans.Requests;
using Shared.DTOs.Loans.Responses;
using Shared.DTOs.Users.Requests;
using Shared.DTOs.Users.Responses;
using Shared.DTOs.ValueObjects;

namespace API.Tests;

public class LoansApiTests : IClassFixture<BoardGameApiFactory>
{
    private readonly BoardGameApiFactory _factory;
    private readonly HttpClient _client;

    public LoansApiTests(BoardGameApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLoans_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync("/api/loans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLoan_WithExistingId_ShouldReturnOk()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        // Act
        var response = await _client.GetAsync(
            $"/api/loans/{loan.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(result);
        Assert.Equal(loan.Id, result.Id);
        Assert.Equal(loan.ItemId, result.ItemId);
        Assert.Equal(loan.BorrowerId, result.BorrowerId);
    }

    [Fact]
    public async Task GetLoan_WithUnknownId_ShouldReturnNotFound()
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync(
            $"/api/loans/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnCreated()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();
        var borrower = await CreateUserAsync();

        var request = new CreateLoanRequest
        {
            LoanDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(14),
            ItemId = item.Id,
            BorrowerId = borrower.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/loans",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var loan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);
        Assert.NotEqual(Guid.Empty, loan.Id);
        Assert.Equal(item.Id, loan.ItemId);
        Assert.Equal(borrower.Id, loan.BorrowerId);
        Assert.False(loan.IsReturned);
        Assert.False(loan.IsOverdue);
        Assert.Null(loan.ReturnDate);

        // Verify that the loan was persisted.
        var getResponse = await _client.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.Equal(loan.Id, savedLoan.Id);
    }

    [Fact]
    public async Task CreateLoan_WithPastLoanDate_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();
        var borrower = await CreateUserAsync();

        var request = new CreateLoanRequest
        {
            LoanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(14),
            ItemId = item.Id,
            BorrowerId = borrower.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/loans",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WhenBorrowerIsTooYoung_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync(minimumAge: 18);

        // The user is old enough to register but too young for this game.
        var borrower = await CreateUserAsync(
            dateOfBirth: DateOnly.FromDateTime(DateTime.Today).AddYears(-13));

        var request = new CreateLoanRequest
        {
            LoanDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(14),
            ItemId = item.Id,
            BorrowerId = borrower.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/loans",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WhenItemIsAlreadyBorrowed_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        var firstBorrower = await CreateUserAsync();
        var secondBorrower = await CreateUserAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var firstRequest = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id,
            BorrowerId = firstBorrower.Id
        };

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/loans",
            firstRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondRequest = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id,
            BorrowerId = secondBorrower.Id
        };

        // Act
        var secondResponse = await _client.PostAsJsonAsync(
            "/api/loans",
            secondRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnOkAndSetReturnDate()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        var returnDate = DateOnly.FromDateTime(DateTime.Today);

        var request = new ReturnLoanRequest
        {
            ReturnDate = DateOnly.FromDateTime(DateTime.Today),
            RowVersion = loan.RowVersion
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var returnedLoan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(returnedLoan);
        Assert.True(returnedLoan.IsReturned);
        Assert.Equal(returnDate, returnedLoan.ReturnDate);

        // Verify that the return was persisted.
        var getResponse = await _client.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.True(savedLoan.IsReturned);
        Assert.Equal(returnDate, savedLoan.ReturnDate);
        Assert.Equal(8, returnedLoan.RowVersion.Length);
        Assert.False(loan.RowVersion.SequenceEqual(returnedLoan.RowVersion)); // The RowVersion should have changed after the update.
    }

    [Fact]
    public async Task ReturnLoan_WhenAlreadyReturned_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        var request = new ReturnLoanRequest
        {
            ReturnDate = DateOnly.FromDateTime(DateTime.Today),
            RowVersion = loan.RowVersion
        };

        var firstResponse = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act
        var secondResponse = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    // Creates a complete loan through the API.
    private async Task<LoanResponse> CreateLoanAsync()
    {
        var item = await CreateItemAsync();
        var borrower = await CreateUserAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var response = await _client.PostAsJsonAsync(
            "/api/loans",
            new CreateLoanRequest
            {
                LoanDate = today,
                DueDate = today.AddDays(14),
                ItemId = item.Id,
                BorrowerId = borrower.Id
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var loan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);

        return loan;
    }

    // Creates an item with its required board game and owner.
    private async Task<ItemResponse> CreateItemAsync(
        int minimumAge = 12)
    {
        var boardGame = await CreateBoardGameAsync(minimumAge);
        var owner = await CreateUserAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id,
                OwnerId = owner.Id
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await response.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);

        return item;
    }

    private async Task<BoardGameResponse> CreateBoardGameAsync(
        int minimumAge)
    {
        var request = new CreateBoardGameRequest
        {
            Title = $"Loan Test Game {Guid.NewGuid():N}",
            Description = "Board game created for loan API testing.",
            Genre = "Strategy",
            Publisher = "Test Publisher",
            Edition = null,
            ReleaseYear = 2025,
            MinPlayers = 2,
            MaxPlayers = 4,
            PlayingTimeMinutes = 60,
            MinimumAge = minimumAge
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

    private async Task<UserResponse> CreateUserAsync(
        DateOnly? dateOfBirth = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N");

        var request = new CreateUserRequest
        {
            Username = $"loan-user-{uniqueId}",

            Name = new NameDto
            {
                FirstName = "Loan",
                LastName = "Tester"
            },

            Email = new EmailDto
            {
                Value = $"loan-{uniqueId}@example.com"
            },

            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1),

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
    [Fact]
    public async Task CreateLoan_WhenRequestedConcurrently_ShouldAllowOnlyOneLoan()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();
        var firstBorrower = await CreateUserAsync();
        var secondBorrower = await CreateUserAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var firstRequest = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id,
            BorrowerId = firstBorrower.Id
        };

        var secondRequest = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id,
            BorrowerId = secondBorrower.Id
        };

        // Act
        var requests = new[]
        {
        _client.PostAsJsonAsync("/api/loans", firstRequest),
        _client.PostAsJsonAsync("/api/loans", secondRequest)
    };

        var responses = await Task.WhenAll(requests);

        // Assert
        Assert.Single(responses,
            response => response.StatusCode == HttpStatusCode.Created);

        Assert.Single(responses,
            response => response.StatusCode == HttpStatusCode.Conflict);

        // Verify the database state through the API.
        var getResponse = await _client.GetAsync("/api/loans");

        getResponse.EnsureSuccessStatusCode();

        var loans = await getResponse.Content
            .ReadFromJsonAsync<List<LoanResponse>>();

        Assert.NotNull(loans);

        Assert.Single(loans,
            loan => loan.ItemId == item.Id && !loan.IsReturned);
    }


    [Fact]
    public async Task ReturnLoan_WithoutRowVersion_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        var request = new ReturnLoanRequest
        {
            ReturnDate = DateOnly.FromDateTime(DateTime.Today),
            RowVersion = []
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify that the loan is still active.
        var getResponse = await _client.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.False(savedLoan.IsReturned);
        Assert.Null(savedLoan.ReturnDate);
    }

    [Fact]
    public async Task ReturnLoan_WithStaleRowVersion_ShouldReturnConflict()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        var firstReturnDate = DateOnly.FromDateTime(DateTime.Today);
        var secondReturnDate = firstReturnDate.AddDays(1);

        // Both requests use the original RowVersion.
        var firstRequest = new ReturnLoanRequest
        {
            ReturnDate = firstReturnDate,
            RowVersion = loan.RowVersion
        };

        var secondRequest = new ReturnLoanRequest
        {
            ReturnDate = secondReturnDate,
            RowVersion = loan.RowVersion
        };

        // Act: Return the loan successfully.
        var firstResponse = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            firstRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Attempt another return using the outdated RowVersion.
        var secondResponse = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        // Verify that the original return date was preserved.
        var getResponse = await _client.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.True(savedLoan.IsReturned);
        Assert.Equal(firstReturnDate, savedLoan.ReturnDate);
    }


}
