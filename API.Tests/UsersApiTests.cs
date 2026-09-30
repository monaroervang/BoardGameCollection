
using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.Users.Requests;
using Shared.DTOs.Users.Responses;
using Shared.DTOs.ValueObjects;

namespace API.Tests;

public class UsersApiTests : IClassFixture<BoardGameApiFactory>
{
    private readonly BoardGameApiFactory _factory;
    private readonly HttpClient _client;

    public UsersApiTests(BoardGameApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOk()
    {
        await _factory.InitializeDatabaseAsync();

        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated()
    {
        await _factory.InitializeDatabaseAsync();

        var request = CreateValidUserRequest();

        var response = await _client.PostAsJsonAsync(
            "/api/users",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Name.FirstName, user.Name.FirstName);
        Assert.Equal(request.Name.LastName, user.Name.LastName);
        Assert.Equal(request.Email.Value, user.Email.Value);
        Assert.Equal(request.Address.City, user.Address.City);

        // SQL Server must generate the initial RowVersion.
        Assert.Equal(8, user.RowVersion.Length);

        var getResponse = await _client.GetAsync(
            $"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedUser = await getResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(savedUser);
        Assert.Equal(user.Id, savedUser.Id);
        Assert.Equal(request.Username, savedUser.Username);
        Assert.Equal(user.RowVersion, savedUser.RowVersion);
    }

    [Fact]
    public async Task CreateUser_WhenUserIsTooYoung_ShouldReturnBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        var request = CreateValidUserRequest() with
        {
            DateOfBirth = DateOnly
                .FromDateTime(DateTime.Today)
                .AddYears(-10)
        };

        var response = await _client.PostAsJsonAsync(
            "/api/users",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser()
    {
        await _factory.InitializeDatabaseAsync();

        var createdUser = await CreateUserAsync();

        var updateRequest = CreateUpdateRequest(
            createdUser,
            $"updated-{Guid.NewGuid():N}");

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/users/{createdUser.Id}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedUser = await updateResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(updatedUser);
        Assert.Equal(createdUser.Id, updatedUser.Id);
        Assert.Equal(updateRequest.Username, updatedUser.Username);
        Assert.Equal(
            updateRequest.Name.FirstName,
            updatedUser.Name.FirstName);
        Assert.Equal(
            updateRequest.Email.Value,
            updatedUser.Email.Value);
        Assert.Equal(
            updateRequest.Address.City,
            updatedUser.Address.City);

        // An update must generate a new RowVersion.
        Assert.Equal(8, updatedUser.RowVersion.Length);
        Assert.False(
            createdUser.RowVersion.SequenceEqual(
                updatedUser.RowVersion));

        var getResponse = await _client.GetAsync(
            $"/api/users/{createdUser.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedUser = await getResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(savedUser);
        Assert.Equal(updateRequest.Username, savedUser.Username);
        Assert.Equal(
            updateRequest.Address.Street,
            savedUser.Address.Street);
        Assert.Equal(
            updatedUser.RowVersion,
            savedUser.RowVersion);
    }

    [Fact]
    public async Task UpdateUser_WithoutRowVersion_ShouldReturnBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        var createdUser = await CreateUserAsync();

        var updateRequest = CreateUpdateRequest(
            createdUser,
            "Updated Without Version") with
        {
            RowVersion = []
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/users/{createdUser.Id}",
            updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WithStaleRowVersion_ShouldReturnConflict()
    {
        await _factory.InitializeDatabaseAsync();

        var originalUser = await CreateUserAsync();

        // Both requests use the same original RowVersion.
        var firstUpdate = CreateUpdateRequest(
            originalUser,
            $"first-{Guid.NewGuid():N}");

        var secondUpdate = CreateUpdateRequest(
            originalUser,
            $"second-{Guid.NewGuid():N}");

        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/users/{originalUser.Id}",
            firstUpdate);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await _client.PutAsJsonAsync(
            $"/api/users/{originalUser.Id}",
            secondUpdate);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        // The first update must remain in the database.
        var getResponse = await _client.GetAsync(
            $"/api/users/{originalUser.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedUser = await getResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(savedUser);
        Assert.Equal(firstUpdate.Username, savedUser.Username);
        Assert.NotEqual(secondUpdate.Username, savedUser.Username);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent()
    {
        await _factory.InitializeDatabaseAsync();

        var createdUser = await CreateUserAsync();

        using var deleteRequest = CreateDeleteRequest(createdUser);

        var deleteResponse = await _client.SendAsync(deleteRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/users/{createdUser.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithoutRowVersion_ShouldReturnBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        var createdUser = await CreateUserAsync();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/users/{createdUser.Id}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            deleteResponse.StatusCode);

        // The user must still exist.
        var getResponse = await _client.GetAsync(
            $"/api/users/{createdUser.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithStaleRowVersion_ShouldReturnConflict()
    {
        await _factory.InitializeDatabaseAsync();

        var originalUser = await CreateUserAsync();

        // Another request updates the user first.
        var updateRequest = CreateUpdateRequest(
            originalUser,
            $"updated-{Guid.NewGuid():N}");

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/users/{originalUser.Id}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Attempt deletion using the outdated RowVersion.
        using var deleteRequest = CreateDeleteRequest(originalUser);

        var deleteResponse = await _client.SendAsync(deleteRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            deleteResponse.StatusCode);

        // The updated user must still exist.
        var getResponse = await _client.GetAsync(
            $"/api/users/{originalUser.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedUser = await getResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(savedUser);
        Assert.Equal(updateRequest.Username, savedUser.Username);
    }

    // Create a valid user with unique credentials.
    private static CreateUserRequest CreateValidUserRequest()
    {
        return new CreateUserRequest
        {
            Username = $"testuser-{Guid.NewGuid():N}",
            Name = new NameDto
            {
                FirstName = "Test",
                LastName = "User"
            },
            Email = new EmailDto
            {
                Value = $"test-{Guid.NewGuid():N}@example.com"
            },
            DateOfBirth = new DateOnly(2000, 1, 1),
            Address = new AddressDto
            {
                Street = "Test Street",
                HouseNumber = "10",
                PostalCode = "5000",
                City = "Odense",
                Country = "Denmark"
            }
        };
    }

    // Create a user through the API and return its generated RowVersion.
    private async Task<UserResponse> CreateUserAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/users",
            CreateValidUserRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(user);
        Assert.Equal(8, user.RowVersion.Length);

        return user;
    }

    // Build an update request with the originally retrieved RowVersion.
    private static UpdateUserRequest CreateUpdateRequest(
        UserResponse user,
        string username)
    {
        return new UpdateUserRequest
        {
            Username = username,
            Name = new NameDto
            {
                FirstName = "Updated",
                LastName = "User"
            },
            Email = new EmailDto
            {
                Value = $"updated-{Guid.NewGuid():N}@example.com"
            },
            DateOfBirth = new DateOnly(1995, 5, 15),
            Address = new AddressDto
            {
                Street = "Updated Street",
                HouseNumber = "20",
                PostalCode = "8000",
                City = "Aarhus",
                Country = "Denmark"
            },
            RowVersion = user.RowVersion
        };
    }

    // Create a DELETE request containing the original RowVersion.
    private static HttpRequestMessage CreateDeleteRequest(
        UserResponse user)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/users/{user.Id}");

        var encodedVersion = Convert.ToBase64String(
            user.RowVersion);

        request.Headers.TryAddWithoutValidation(
            "If-Match",
            $"\"{encodedVersion}\"");

        return request;
    }
}
