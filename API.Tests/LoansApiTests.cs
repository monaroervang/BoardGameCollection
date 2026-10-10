
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Enums;
using Domain.Models;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repositories;
using Shared.DTOs.Authentication.Requests;
using Shared.DTOs.Authentication.Responses;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.BoardGames.Responses;
using Shared.DTOs.Items.Requests;
using Shared.DTOs.Items.Responses;
using Shared.DTOs.Loans.Requests;
using Shared.DTOs.Loans.Responses;

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

        using var adminClient = _factory.CreateClient();
        await AuthenticateAsync(adminClient, UserRole.Admin);

        var response = await adminClient.GetAsync("/api/loans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLoans_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync("/api/loans");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLoans_WithUserRole_ShouldReturnForbidden()
    {
        await _factory.InitializeDatabaseAsync();

        using var userClient = _factory.CreateClient();
        await AuthenticateAsync(userClient, UserRole.User);

        var response = await userClient.GetAsync("/api/loans");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLoan_WithExistingId_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, loan.Id);

        var response = await borrowerClient.GetAsync(
            $"/api/loans/{loan.Id}");

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

        using var userClient = _factory.CreateClient();
        await AuthenticateAsync(userClient, UserRole.User);

        var response = await userClient.GetAsync(
            $"/api/loans/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLoan_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        var response = await _client.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLoan_WhenUserIsNotBorrowerOrOwner_ShouldReturnForbidden()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient, UserRole.User);

        var response = await otherClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLoan_WhenUserIsOwner_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsOwnerAsync(ownerClient, loan.ItemId);

        var response = await ownerClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(result);
        Assert.Equal(loan.Id, result.Id);
        Assert.Equal(loan.BorrowerId, result.BorrowerId);
    }

    [Fact]
    public async Task GetLoan_WhenUserIsAdmin_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var adminClient = _factory.CreateClient();
        await AuthenticateAsync(adminClient, UserRole.Admin);

        var response = await adminClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var response = await _client.PostAsJsonAsync(
            "/api/loans",
            new CreateLoanRequest
            {
                LoanDate = today,
                DueDate = today.AddDays(14),
                ItemId = item.Id
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnCreated()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        using var borrowerClient = _factory.CreateClient();
        var borrowerId = await AuthenticateAsync(
            borrowerClient,
            UserRole.User);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var request = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id
        };

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/loans",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var loan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);
        Assert.NotEqual(Guid.Empty, loan.Id);
        Assert.Equal(item.Id, loan.ItemId);
        Assert.Equal(borrowerId, loan.BorrowerId);
        Assert.False(loan.IsReturned);
        Assert.False(loan.IsOverdue);
        Assert.Null(loan.ReturnDate);

        // The borrower can retrieve their own loan.
        var getResponse = await borrowerClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.Equal(loan.Id, savedLoan.Id);
        Assert.Equal(borrowerId, savedLoan.BorrowerId);
    }

    [Fact]
    public async Task CreateLoan_WithPastLoanDate_ShouldReturnBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsync(borrowerClient, UserRole.User);

        var request = new CreateLoanRequest
        {
            LoanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(14),
            ItemId = item.Id
        };

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/loans",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WhenBorrowerIsTooYoung_ShouldReturnBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync(minimumAge: 18);

        using var borrowerClient = _factory.CreateClient();

        await AuthenticateAsync(
            borrowerClient,
            UserRole.User,
            dateOfBirth: DateOnly.FromDateTime(DateTime.Today)
                .AddYears(-13));

        var today = DateOnly.FromDateTime(DateTime.Today);

        var request = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id
        };

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/loans",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLoan_WhenItemIsAlreadyBorrowed_ShouldReturnConflict()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        using var firstBorrowerClient = _factory.CreateClient();
        using var secondBorrowerClient = _factory.CreateClient();

        await AuthenticateAsync(firstBorrowerClient, UserRole.User);
        await AuthenticateAsync(secondBorrowerClient, UserRole.User);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var request = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id
        };

        var firstResponse = await firstBorrowerClient.PostAsJsonAsync(
            "/api/loans",
            request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await secondBorrowerClient.PostAsJsonAsync(
            "/api/loans",
            request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnOkAndSetReturnDate()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, loan.Id);

        var returnDate = DateOnly.FromDateTime(DateTime.Today);

        var request = new ReturnLoanRequest
        {
            ReturnDate = returnDate,
            RowVersion = loan.RowVersion
        };

        var response = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var returnedLoan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(returnedLoan);
        Assert.True(returnedLoan.IsReturned);
        Assert.Equal(returnDate, returnedLoan.ReturnDate);

        var getResponse = await borrowerClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.True(savedLoan.IsReturned);
        Assert.Equal(returnDate, savedLoan.ReturnDate);
        Assert.Equal(8, returnedLoan.RowVersion.Length);

        Assert.False(
            loan.RowVersion.SequenceEqual(returnedLoan.RowVersion));
    }

    [Fact]
    public async Task ReturnLoan_WhenAlreadyReturned_ShouldReturnConflict()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, loan.Id);

        var request = new ReturnLoanRequest
        {
            ReturnDate = DateOnly.FromDateTime(DateTime.Today),
            RowVersion = loan.RowVersion
        };

        var firstResponse = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            new ReturnLoanRequest
            {
                ReturnDate = DateOnly.FromDateTime(DateTime.Today),
                RowVersion = loan.RowVersion
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReturnLoan_WhenUserIsNotBorrowerOrOwner_ShouldReturnForbidden()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient, UserRole.User);

        var response = await otherClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            new ReturnLoanRequest
            {
                ReturnDate = DateOnly.FromDateTime(DateTime.Today),
                RowVersion = loan.RowVersion
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // An unauthorized return must not change the loan.
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var savedLoan = await context.Loans
            .AsNoTracking()
            .FirstAsync(saved => saved.Id == loan.Id);

        Assert.False(savedLoan.IsReturned);
    }

    [Fact]
    public async Task ReturnLoan_WhenUserIsOwner_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsOwnerAsync(ownerClient, loan.ItemId);

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            new ReturnLoanRequest
            {
                ReturnDate = DateOnly.FromDateTime(DateTime.Today),
                RowVersion = loan.RowVersion
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var returnedLoan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(returnedLoan);
        Assert.True(returnedLoan.IsReturned);
    }

    [Fact]
    public async Task ReturnLoan_WhenUserIsAdmin_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var adminClient = _factory.CreateClient();
        await AuthenticateAsync(adminClient, UserRole.Admin);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            new ReturnLoanRequest
            {
                ReturnDate = DateOnly.FromDateTime(DateTime.Today),
                RowVersion = loan.RowVersion
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var returnedLoan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(returnedLoan);
        Assert.True(returnedLoan.IsReturned);
    }

    [Fact]
    public async Task CreateLoan_WhenRequestedConcurrently_ShouldAllowOnlyOneLoan()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        using var firstBorrowerClient = _factory.CreateClient();
        using var secondBorrowerClient = _factory.CreateClient();

        await AuthenticateAsync(firstBorrowerClient, UserRole.User);
        await AuthenticateAsync(secondBorrowerClient, UserRole.User);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var request = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id
        };

        var requests = new[]
        {
            firstBorrowerClient.PostAsJsonAsync("/api/loans", request),
            secondBorrowerClient.PostAsJsonAsync("/api/loans", request)
        };

        var responses = await Task.WhenAll(requests);

        Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Created);

        Assert.Single(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);

        // Only administrators can retrieve the complete loan list.
        using var adminClient = _factory.CreateClient();
        await AuthenticateAsync(adminClient, UserRole.Admin);

        var getResponse = await adminClient.GetAsync("/api/loans");

        getResponse.EnsureSuccessStatusCode();

        var loans = await getResponse.Content
            .ReadFromJsonAsync<List<LoanResponse>>();

        Assert.NotNull(loans);

        Assert.Single(
            loans,
            loan => loan.ItemId == item.Id && !loan.IsReturned);
    }

    [Fact]
    public async Task ReturnLoan_WithoutRowVersion_ShouldReturnBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, loan.Id);

        var request = new ReturnLoanRequest
        {
            ReturnDate = DateOnly.FromDateTime(DateTime.Today),
            RowVersion = []
        };

        var response = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var getResponse = await borrowerClient.GetAsync(
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
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, loan.Id);

        var firstReturnDate = DateOnly.FromDateTime(DateTime.Today);
        var secondReturnDate = firstReturnDate.AddDays(1);

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

        var firstResponse = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            firstRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // The outdated version must not overwrite the first return.
        var secondResponse = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var getResponse = await borrowerClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedLoan = await getResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.True(savedLoan.IsReturned);
        Assert.Equal(firstReturnDate, savedLoan.ReturnDate);
    }

    [Fact]
    public async Task CreateLoan_WhenItemIsArchived_ShouldReturnConflict()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsExistingUserAsync(ownerClient, item.OwnerId);

        var archiveResponse = await ownerClient.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsync(borrowerClient, UserRole.User);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var request = new CreateLoanRequest
        {
            LoanDate = today,
            DueDate = today.AddDays(14),
            ItemId = item.Id
        };

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/loans",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        Assert.False(
            await context.Loans.AnyAsync(
                loan => loan.ItemId == item.Id));
    }

    [Fact]
    public async Task ArchiveItem_WhenItemHasActiveLoan_ShouldReturnConflict()
    {
        await _factory.InitializeDatabaseAsync();

        var item = await CreateItemAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsync(borrowerClient, UserRole.User);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var loanResponse = await borrowerClient.PostAsJsonAsync(
            "/api/loans",
            new CreateLoanRequest
            {
                LoanDate = today,
                DueDate = today.AddDays(14),
                ItemId = item.Id
            });

        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);

        var loan = await loanResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);

        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsExistingUserAsync(ownerClient, item.OwnerId);

        var archiveResponse = await ownerClient.PutAsync(
            $"/api/items/{item.Id}/archive",
            null);

        Assert.Equal(HttpStatusCode.Conflict, archiveResponse.StatusCode);

        var collection = await ownerClient.GetFromJsonAsync<
            List<CollectionItemResponse>>("/api/items/my-collection");

        Assert.NotNull(collection);

        var savedItem = Assert.Single(
            collection,
            entry => entry.ItemId == item.Id);

        Assert.False(savedItem.IsArchived);

        // The borrower can still view the unchanged active loan.
        var getLoanResponse = await borrowerClient.GetAsync(
            $"/api/loans/{loan.Id}");

        Assert.Equal(HttpStatusCode.OK, getLoanResponse.StatusCode);

        var savedLoan = await getLoanResponse.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(savedLoan);
        Assert.False(savedLoan.IsReturned);
        Assert.Null(savedLoan.ReturnDate);
    }


    [Fact]
    public async Task GetMyLoans_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync("/api/loans/my-loans");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLendings_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync("/api/loans/my-lendings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLoans_ShouldReturnOnlyAuthenticatedBorrowersLoans()
    {
        await _factory.InitializeDatabaseAsync();

        var ownLoan = await CreateLoanAsync();
        var otherLoan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, ownLoan.Id);

        var response = await borrowerClient.GetAsync("/api/loans/my-loans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loans = await response.Content
            .ReadFromJsonAsync<List<LoanResponse>>();

        Assert.NotNull(loans);
        Assert.Contains(loans, loan => loan.Id == ownLoan.Id);
        Assert.DoesNotContain(loans, loan => loan.Id == otherLoan.Id);

        // No loans belonging to another borrower may be exposed.
        Assert.All(
            loans,
            loan => Assert.Equal(ownLoan.BorrowerId, loan.BorrowerId));
    }


    [Fact]
    public async Task GetMyLendings_ShouldReturnOnlyOwnedItemsLoans()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var ownLoan = await CreateLoanAsync();
        var otherLoan = await CreateLoanAsync();

        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsOwnerAsync(ownerClient, ownLoan.ItemId);

        // Find the owner of the physical copy.
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var ownerId = await context.Items
            .AsNoTracking()
            .Where(item => item.Id == ownLoan.ItemId)
            .Select(item => item.OwnerId)
            .SingleAsync();

        // Act
        var response = await ownerClient.GetAsync(
            "/api/loans/my-lendings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loans = await response.Content
            .ReadFromJsonAsync<List<LoanResponse>>();

        Assert.NotNull(loans);

        // The owner's loan must be included.
        Assert.Contains(loans, loan => loan.Id == ownLoan.Id);

        // Another owner's loan must not be included.
        Assert.DoesNotContain(loans, loan => loan.Id == otherLoan.Id);

        // Every returned loan must concern an item owned by this user.
        var returnedItemIds = loans
            .Select(loan => loan.ItemId)
            .Distinct()
            .ToList();

        var ownedItemIds = await context.Items
            .AsNoTracking()
            .Where(item =>
                item.OwnerId == ownerId &&
                returnedItemIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync();

        Assert.Equal(
            returnedItemIds.Count,
            ownedItemIds.Count);
    }


    [Fact]
    public async Task GetMyLoans_ShouldIncludeReturnedLoans()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var borrowerClient = _factory.CreateClient();
        await AuthenticateAsBorrowerAsync(borrowerClient, loan.Id);

        var returnResponse = await borrowerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            new ReturnLoanRequest
            {
                ReturnDate = DateOnly.FromDateTime(DateTime.Today),
                RowVersion = loan.RowVersion
            });

        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var loans = await borrowerClient.GetFromJsonAsync<List<LoanResponse>>(
            "/api/loans/my-loans");

        Assert.NotNull(loans);

        var returnedLoan = Assert.Single(
            loans,
            entry => entry.Id == loan.Id);

        Assert.True(returnedLoan.IsReturned);
        Assert.NotNull(returnedLoan.ReturnDate);
    }

    [Fact]
    public async Task GetMyLendings_ShouldIncludeReturnedLoans()
    {
        await _factory.InitializeDatabaseAsync();

        var loan = await CreateLoanAsync();

        using var ownerClient = _factory.CreateClient();
        await AuthenticateAsOwnerAsync(ownerClient, loan.ItemId);

        var returnResponse = await ownerClient.PostAsJsonAsync(
            $"/api/loans/{loan.Id}/return",
            new ReturnLoanRequest
            {
                ReturnDate = DateOnly.FromDateTime(DateTime.Today),
                RowVersion = loan.RowVersion
            });

        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var loans = await ownerClient.GetFromJsonAsync<List<LoanResponse>>(
            "/api/loans/my-lendings");

        Assert.NotNull(loans);

        var returnedLoan = Assert.Single(
            loans,
            entry => entry.Id == loan.Id);

        Assert.True(returnedLoan.IsReturned);
        Assert.NotNull(returnedLoan.ReturnDate);
    }


    // Creates a complete loan through the API.
    private async Task<LoanResponse> CreateLoanAsync()
    {
        var item = await CreateItemAsync();

        using var borrowerClient = _factory.CreateClient();
        var borrowerId = await AuthenticateAsync(
            borrowerClient,
            UserRole.User);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var response = await borrowerClient.PostAsJsonAsync(
            "/api/loans",
            new CreateLoanRequest
            {
                LoanDate = today,
                DueDate = today.AddDays(14),
                ItemId = item.Id
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var loan = await response.Content
            .ReadFromJsonAsync<LoanResponse>();

        Assert.NotNull(loan);
        Assert.Equal(borrowerId, loan.BorrowerId);

        return loan;
    }

    // Creates an item with its required board game and authenticated owner.
    private async Task<ItemResponse> CreateItemAsync(
        int minimumAge = 12)
    {
        var boardGame = await CreateBoardGameAsync(minimumAge);

        using var ownerClient = _factory.CreateClient();

        var ownerId = await AuthenticateAsync(
            ownerClient,
            UserRole.User);

        var response = await ownerClient.PostAsJsonAsync(
            "/api/items",
            new CreateItemRequest
            {
                BoardGameId = boardGame.Id
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await response.Content
            .ReadFromJsonAsync<ItemResponse>();

        Assert.NotNull(item);
        Assert.Equal(ownerId, item.OwnerId);

        return item;
    }

    // Creates board game test data using an administrator client.
    private async Task<BoardGameResponse> CreateBoardGameAsync(
        int minimumAge)
    {
        using var adminClient = _factory.CreateClient();

        await AuthenticateAsync(adminClient, UserRole.Admin);

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

        var response = await adminClient.PostAsJsonAsync(
            "/api/boardgames",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var boardGame = await response.Content
            .ReadFromJsonAsync<BoardGameResponse>();

        Assert.NotNull(boardGame);

        return boardGame;
    }

    // Creates a test user with the specified role and authenticates the client.
    private async Task<Guid> AuthenticateAsync(
        HttpClient client,
        UserRole role,
        DateOnly? dateOfBirth = null)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var username =
            $"loan-{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";

        const string password = "Test123!";

        var user = new User(
            username: username,
            name: new Name("Loan", "Tester"),
            email: new Email($"{username}@example.com"),
            dateOfBirth: dateOfBirth ?? new DateOnly(1990, 1, 1),
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

    // Authenticates as a previously created test user.
    private async Task AuthenticateAsExistingUserAsync(
        HttpClient client,
        Guid userId)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var user = await context.Users
            .FirstAsync(user => user.Id == userId);

        const string password = "Test123!";

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Username = user.Username,
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

    // Authenticates as the borrower associated with a specific loan.
    private async Task AuthenticateAsBorrowerAsync(
        HttpClient client,
        Guid loanId)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var loan = await context.Loans
            .AsNoTracking()
            .FirstAsync(loan => loan.Id == loanId);

        await AuthenticateAsExistingUserAsync(
            client,
            loan.BorrowerId);
    }

    // Authenticates as the owner of a physical board game copy.
    private async Task AuthenticateAsOwnerAsync(
        HttpClient client,
        Guid itemId)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<BoardGameDbContext>();

        var item = await context.Items
            .AsNoTracking()
            .FirstAsync(item => item.Id == itemId);

        await AuthenticateAsExistingUserAsync(
            client,
            item.OwnerId);
    }
}
