namespace Shared.DTOs.Authentication.Requests;

public sealed record LoginRequest
{
    public string Username { get; set; } = ""; //Be able to update while user is typing in the login form
    public string Password { get; set; } = ""; //Be able to update while user is typing in the login form
}