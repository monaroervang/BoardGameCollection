namespace Shared.DTOs.Authentication.Responses;

public sealed record LoginResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string Token { get; init; } = "";
}