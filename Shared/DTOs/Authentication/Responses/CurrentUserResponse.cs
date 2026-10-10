namespace Shared.DTOs.Authentication.Responses;

public sealed record CurrentUserResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string Role { get; init; } = ""; //No reference to Domain.Entities.UserRole to avoid circular dependency
}