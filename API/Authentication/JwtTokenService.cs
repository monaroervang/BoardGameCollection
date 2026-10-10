using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API.Authentication;

public sealed class JwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]!; // Validate that the key is not null or empty in production code.
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
    }

    // Create a signed access token for the authenticated user.
    public string CreateToken(User user)
    {
        var claims = new[]
{
    new Claim(
        ClaimTypes.NameIdentifier,
        user.Id.ToString()),

    new Claim(
        ClaimTypes.Name,
        user.Username),

    new Claim(
        ClaimTypes.Role,
        user.Role.ToString())
};

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_key));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}