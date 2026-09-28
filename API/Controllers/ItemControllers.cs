using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.Items.Requests;
using Shared.DTOs.Items.Responses;

namespace API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemControllers : ControllerBase
{
    private readonly BoardGameDbContext _context;

    public ItemControllers(BoardGameDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ItemResponse>>> GetItems()
    {
        var items = await _context.Items
            .AsNoTracking()
            .Select(item => new ItemResponse
            {
                Id = item.Id,
                BoardGameId = item.BoardGameId,
                OwnerId = item.OwnerId
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponse>> GetItem(Guid id)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ItemResponse
            {
                Id = item.Id,
                BoardGameId = item.BoardGameId,
                OwnerId = item.OwnerId
            })
            .FirstOrDefaultAsync();

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponse>> CreateItem(
        CreateItemRequest request)
    {
        // Verify that the board game exists.
        var boardGameExists = await _context.BoardGames
            .AnyAsync(boardGame => boardGame.Id == request.BoardGameId);

        if (!boardGameExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Board game not found",
                Detail = "The specified board game does not exist."
            });
        }

        // Verify that the owner exists.
        var ownerExists = await _context.Users
            .AnyAsync(user => user.Id == request.OwnerId);

        if (!ownerExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Owner not found",
                Detail = "The specified owner does not exist."
            });
        }

        var item = new Item(
            request.BoardGameId,
            request.OwnerId);

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var response = new ItemResponse
        {
            Id = item.Id,
            BoardGameId = item.BoardGameId,
            OwnerId = item.OwnerId
        };

        return CreatedAtAction(
            nameof(GetItem),
            new { id = item.Id },
            response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _context.Items.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        // Keep loan history intact.
        var hasLoans = await _context.Loans
            .AnyAsync(loan => loan.ItemId == id);

        if (hasLoans)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item cannot be deleted",
                Detail = "The item is associated with one or more loans."
            });
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}