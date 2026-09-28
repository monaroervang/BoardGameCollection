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
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = CreateValidUserRequest();

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/users",
            request);

        // Assert
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

        // Verify that the user was actually persisted.
        var getResponse = await _client.GetAsync(
            $"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedUser = await getResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(savedUser);
        Assert.Equal(user.Id, savedUser.Id);
        Assert.Equal(request.Username, savedUser.Username);
    }

    [Fact]
    public async Task CreateUser_WhenUserIsTooYoung_ShouldReturnBadRequest()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var request = new CreateUserRequest
        {
            Username = $"young-{Guid.NewGuid():N}",

            Name = new NameDto
            {
                FirstName = "Young",
                LastName = "User"
            },

            Email = new EmailDto
            {
                Value = $"young-{Guid.NewGuid():N}@example.com"
            },

            DateOfBirth = DateOnly
                .FromDateTime(DateTime.Today)
                .AddYears(-10),

            Address = new AddressDto
            {
                Street = "Test Street",
                HouseNumber = "10",
                PostalCode = "5000",
                City = "Odense",
                Country = "Denmark"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/users",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/users",
            CreateValidUserRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUser = await createResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(createdUser);

        var updateRequest = new UpdateUserRequest
        {
            Username = $"updated-{Guid.NewGuid():N}",

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
            }
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/users/{createdUser.Id}",
            updateRequest);

        // Assert
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

        // Verify that the changes were persisted.
        var getResponse = await _client.GetAsync(
            $"/api/users/{createdUser.Id}");

        var savedUser = await getResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(savedUser);
        Assert.Equal(updateRequest.Username, savedUser.Username);
        Assert.Equal(
            updateRequest.Address.Street,
            savedUser.Address.Street);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/users",
            CreateValidUserRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUser = await createResponse.Content
            .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(createdUser);

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/api/users/{createdUser.Id}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        // Verify that the user was actually deleted.
        var getResponse = await _client.GetAsync(
            $"/api/users/{createdUser.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

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
}