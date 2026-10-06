
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.Users.Responses;
using Domain.Models;
using Domain.ValueObjects;
using Shared.DTOs.Users.Requests;
using Shared.DTOs.ValueObjects;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly BoardGameDbContext _context;

    public UserController(BoardGameDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
        // Return users without EF Core change tracking.
        var users = await _context.Users
            .AsNoTracking()
            .Select(user => new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Name = new NameDto
                {
                    FirstName = user.Name.FirstName,
                    LastName = user.Name.LastName
                },
                Email = new EmailDto
                {
                    Value = user.Email.Value
                },
                DateOfBirth = user.DateOfBirth,
                Address = new AddressDto
                {
                    Street = user.Address.Street,
                    HouseNumber = user.Address.HouseNumber,
                    PostalCode = user.Address.PostalCode,
                    City = user.Address.City,
                    Country = user.Address.Country
                },
                RowVersion = user.RowVersion
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        // Retrieve a single user without change tracking.
        var user = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Name = new NameDto
                {
                    FirstName = user.Name.FirstName,
                    LastName = user.Name.LastName
                },
                Email = new EmailDto
                {
                    Value = user.Email.Value
                },
                DateOfBirth = user.DateOfBirth,
                Address = new AddressDto
                {
                    Street = user.Address.Street,
                    HouseNumber = user.Address.HouseNumber,
                    PostalCode = user.Address.PostalCode,
                    City = user.Address.City,
                    Country = user.Address.Country
                },
                RowVersion = user.RowVersion
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request)
    {
        // Create domain value objects from the request.
        var name = new Name(
            request.Name.FirstName,
            request.Name.LastName);

        var email = new Email(request.Email.Value);

        var address = new Address(
            request.Address.Street,
            request.Address.HouseNumber,
            request.Address.PostalCode,
            request.Address.City,
            request.Address.Country);

        // Create the domain entity.
        var user = new User(
            request.Username,
            name,
            email,
            request.DateOfBirth,
            address);

        _context.Users.Add(user);

        // SQL Server generates RowVersion when the user is saved.
        await _context.SaveChangesAsync();

        var response = MapToResponse(user);

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        Guid id,
        UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        // Require the version originally retrieved by the client.
        if (request.RowVersion is null || request.RowVersion.Length != 8)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid RowVersion",
                Detail = "A valid RowVersion is required when updating a user."
            });
        }

        // Use the client's original version for the concurrency check.
        _context.Entry(user)
            .Property(u => u.RowVersion)
            .OriginalValue = request.RowVersion;

        // Create domain value objects from the request.
        var name = new Name(
            request.Name.FirstName,
            request.Name.LastName);

        var email = new Email(request.Email.Value);

        var address = new Address(
            request.Address.Street,
            request.Address.HouseNumber,
            request.Address.PostalCode,
            request.Address.City,
            request.Address.Country);

        // Let the domain entity enforce its validation rules.
        user.Update(
            request.Username,
            name,
            email,
            request.DateOfBirth,
            address);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User was modified",
                Detail = "The user has been modified or deleted by another request. Retrieve the latest version before updating."
            });
        }

        // Return the updated user with the newly generated RowVersion.
        return Ok(MapToResponse(user));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        // Require the version originally retrieved by the client.
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing RowVersion",
                Detail = "The If-Match header is required when deleting a user."
            });
        }

        byte[] rowVersion;

        try
        {
            // Decode the Base64-encoded RowVersion from the ETag.
            var encodedVersion = ifMatch.Trim().Trim('"');
            rowVersion = Convert.FromBase64String(encodedVersion);

            if (rowVersion.Length != 8)
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid RowVersion",
                Detail = "If-Match must contain a valid Base64-encoded RowVersion."
            });
        }

        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        // Prevent deletion when the user owns items or has loan history.
        var ownsItems = await _context.Items
            .AnyAsync(item => item.OwnerId == id);

        var hasLoans = await _context.Loans
            .AnyAsync(loan => loan.BorrowerId == id);

        if (ownsItems || hasLoans)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User cannot be deleted",
                Detail = "The user is associated with items or loans."
            });
        }

        // Compare the client's original version with the database version.
        _context.Entry(user)
            .Property(u => u.RowVersion)
            .OriginalValue = rowVersion;

        _context.Users.Remove(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User was modified",
                Detail = "The user has been modified or deleted by another request. Retrieve the latest version before deleting."
            });
        }

        return NoContent();
    }

    // Map a domain user to the response DTO.
    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Name = new NameDto
            {
                FirstName = user.Name.FirstName,
                LastName = user.Name.LastName
            },
            Email = new EmailDto
            {
                Value = user.Email.Value
            },
            DateOfBirth = user.DateOfBirth,
            Address = new AddressDto
            {
                Street = user.Address.Street,
                HouseNumber = user.Address.HouseNumber,
                PostalCode = user.Address.PostalCode,
                City = user.Address.City,
                Country = user.Address.Country
            },
            RowVersion = user.RowVersion
        };
    }
}
