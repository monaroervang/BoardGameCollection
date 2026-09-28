using Shared.DTOs.ValueObjects;

namespace Shared.DTOs.Users.Requests;

public sealed record CreateUserRequest
{
    public string Username { get; init; } = "";
    public NameDto Name { get; init; } = new();
    public EmailDto Email { get; init; } = new();
    public DateOnly DateOfBirth { get; init; }
    public AddressDto Address { get; init; } = new();
}