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
public class UserControllers : ControllerBase
{
    private readonly BoardGameDbContext _context;

    public UserControllers(BoardGameDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
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
                    City = user.Address.City,
                    PostalCode = user.Address.PostalCode
                }
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
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
                }
            })
            .FirstOrDefaultAsync();

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
        // Create the domain value objects from the request.
        var name = new Name(
            request.Name.FirstName,
            request.Name.LastName);

        var email = new Email(
            request.Email.Value);

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
        await _context.SaveChangesAsync();

        var response = new UserResponse
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
            }
        };

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateUser(
    Guid id,
    UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        // Create the domain value objects from the request.
        var name = new Name(
            request.Name.FirstName,
            request.Name.LastName);

        var email = new Email(
            request.Email.Value);

        var address = new Address(
            request.Address.Street,
            request.Address.HouseNumber,
            request.Address.PostalCode,
            request.Address.City,
            request.Address.Country);

        // Let the domain entity handle its own update rules.
        user.Update(
            request.Username,
            name,
            email,
            request.DateOfBirth,
            address);

        await _context.SaveChangesAsync();

        var response = new UserResponse
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
            }
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

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

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}