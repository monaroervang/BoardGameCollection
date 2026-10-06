namespace Shared.DTOs.Authentication.Responses;

public sealed record CurrentUserResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
}