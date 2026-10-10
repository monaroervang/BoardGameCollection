using API.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.Authentication.Requests;
using Shared.DTOs.Authentication.Responses;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly BoardGameDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        BoardGameDbContext context,
        IPasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    // Authenticate a user and return a JWT access token.
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(LoginResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(user =>
                user.Username == request.Username);

        if (user is null)
        {
            return Unauthorized(); // Return 401 Unauthorized if the user is not found
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(); // Return 401 Unauthorized if the password is incorrect
        }

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Token = token
        });
    }

    // Return the identity of the currently authenticated user.
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {// Read the authenticated user's identity and role directly from the validated JWT
     // to avoid an unnecessary database query.
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var username = User.FindFirstValue(
            ClaimTypes.Name);

        var role = User.FindFirstValue(
            ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse
        {
            UserId = userId,
            Username = username,
            Role = role
        });
    }

    // Verify that the current user has the administrator role.
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult GetAdminAccess()
    {
        return Ok();
    }

}