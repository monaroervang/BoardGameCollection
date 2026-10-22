namespace AuthenticationService.Contracts;

public sealed record LoginResponse(
    Guid UserId,
    string Username,
    string Role,
    string Token
);