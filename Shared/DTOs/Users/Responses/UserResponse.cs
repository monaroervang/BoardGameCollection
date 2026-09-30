
using Shared.DTOs.ValueObjects;

namespace Shared.DTOs.Users.Responses;

public sealed record UserResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public NameDto Name { get; init; } = new();
    public EmailDto Email { get; init; } = new();
    public DateOnly DateOfBirth { get; init; }
    public AddressDto Address { get; init; } = new();

    public byte[] RowVersion { get; init; } = [];
}
