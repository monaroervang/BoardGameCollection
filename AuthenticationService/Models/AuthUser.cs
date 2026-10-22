namespace AuthenticationService.Models;

public class AuthUser
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
}