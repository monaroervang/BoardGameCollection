
using System.Security.Claims;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.Items.Requests;
using Shared.DTOs.Items.Responses;

namespace API.Controllers;

[ApiController]
[Route("api/items")]
public class ItemController : ControllerBase
{
    private readonly BoardGameDbContext _context;

    public ItemController(BoardGameDbContext context)
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

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponse>> CreateItem(
        CreateItemRequest request)
    {
        // Get the authenticated user's ID from the validated JWT.
        var ownerId = GetCurrentUserId();

        if (ownerId is null)
        {
            return Unauthorized();
        }

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

        // Verify that the authenticated user still exists.
        var ownerExists = await _context.Users
            .AnyAsync(user => user.Id == ownerId.Value);

        if (!ownerExists)
        {
            return Unauthorized();
        }

        // The owner is determined by the authenticated user, not the request.
        var item = new Item(
            request.BoardGameId,
            ownerId.Value);

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

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        // Get the authenticated user's ID from the validated JWT.
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var item = await _context.Items.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        // Only the owner may delete their physical copy.
        if (item.OwnerId != currentUserId.Value)
        {
            return Forbid();
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

    // Archive a physical copy without deleting its loan history.
    [Authorize]
    [HttpPut("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveItem(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var item = await _context.Items.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        // Only the owner may archive the physical copy.
        if (item.OwnerId != currentUserId.Value)
        {
            return Forbid();
        }

        if (item.IsArchived)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item already archived",
                Detail = "The item is already archived."
            });
        }

        // An item with an active loan cannot be archived.
        var hasActiveLoan = await _context.Loans
            .AnyAsync(loan =>
                loan.ItemId == id &&
                loan.ReturnDate == null);

        if (hasActiveLoan)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item is currently on loan",
                Detail = "Return the item before archiving it."
            });
        }

        item.Archive();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Reactivate an archived physical copy.
    [Authorize]
    [HttpPut("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateItem(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var item = await _context.Items.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        // Only the owner may reactivate the physical copy.
        if (item.OwnerId != currentUserId.Value)
        {
            return Forbid();
        }

        if (!item.IsArchived)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item already active",
                Detail = "The item is already active."
            });
        }

        item.Reactivate();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Get the authenticated user's physical board game collection.
    [Authorize]
    [HttpGet("my-collection")]
    [ProducesResponseType(
        typeof(List<CollectionItemResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CollectionItemResponse>>>
        GetMyCollection()
    {
        // The user ID comes from the validated JWT, not a route parameter.
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        // Verify that the authenticated user still exists.
        var userExists = await _context.Users
            .AnyAsync(user => user.Id == currentUserId.Value);

        if (!userExists)
        {
            return Unauthorized();
        }

        // Get only the physical copies owned by the authenticated user.
        var items = await _context.Items
            .AsNoTracking()
            .Where(item => item.OwnerId == currentUserId.Value)
            .Join(
                _context.BoardGames,
                item => item.BoardGameId,
                boardGame => boardGame.Id,
                (item, boardGame) => new CollectionItemResponse
                {
                    ItemId = item.Id,
                    BoardGameId = boardGame.Id,
                    Title = boardGame.Title,
                    Genre = boardGame.Genre,
                    Publisher = boardGame.Publisher,
                    MinPlayers = boardGame.MinPlayers,
                    MaxPlayers = boardGame.MaxPlayers,
                    PlayingTimeMinutes = boardGame.PlayingTimeMinutes,
                    MinimumAge = boardGame.MinimumAge,
                    IsArchived = item.IsArchived
                })
            .OrderBy(item => item.Title)
            .ThenBy(item => item.ItemId)
            .ToListAsync();

        return Ok(items);
    }


    // Get all physical copies of a specific board game.
    [HttpGet("boardgame/{boardGameId:guid}")]
    [ProducesResponseType(
        typeof(List<BoardGameItemResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<BoardGameItemResponse>>>
        GetItemsByBoardGame(Guid boardGameId)
    {
        // Verify that the board game exists.
        var boardGameExists = await _context.BoardGames
            .AnyAsync(boardGame => boardGame.Id == boardGameId);

        if (!boardGameExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Board game not found",
                Detail = "The specified board game does not exist."
            });
        }

        // Get all physical copies together with their owner and availability.
        var items = await _context.Items
            .AsNoTracking()
            .Where(item => item.BoardGameId == boardGameId)
            .Join(
                _context.Users,
                item => item.OwnerId,
                user => user.Id,
                (item, user) => new BoardGameItemResponse
                {
                    ItemId = item.Id,
                    OwnerId = user.Id,
                    OwnerUsername = user.Username,

                    // Indicate whether the physical copy is archived.
                    IsArchived = item.IsArchived,

                    // Archived items and items with active loans are unavailable.
                    IsAvailable = !item.IsArchived &&
                        !_context.Loans.Any(loan =>
                            loan.ItemId == item.Id &&
                            loan.ReturnDate == null)
                })
            .OrderBy(item => item.OwnerUsername)
            .ToListAsync();

        return Ok(items);
    }


    // Read the user ID from the validated authentication claims.
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
