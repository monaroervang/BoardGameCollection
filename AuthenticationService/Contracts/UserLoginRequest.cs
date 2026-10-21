namespace AuthenticationService.Contracts;

public sealed record UserLoginRequest(
    string Username,
    string Password);
