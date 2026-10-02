
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.BoardGames.Responses;
using Domain.Models;
using Shared.DTOs.BoardGames.Requests;
using Shared.DTOs.Common;

namespace API.Controllers;

[ApiController]
[Route("api/boardgames")]
public class BoardGamesController : ControllerBase
{
    private readonly BoardGameDbContext _context;

    public BoardGamesController(BoardGameDbContext context)
    {
        _context = context;
    }




    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<BoardGameResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<BoardGameResponse>>> GetBoardGames(
        [FromQuery] string? search,
        [FromQuery] string? genre,
        [FromQuery] int? players,
        [FromQuery] int? age,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters.
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid pagination parameters",
                Detail = "Page must be at least 1, and pageSize must be between 1 and 100."
            });
        }

        // Validate optional numeric filters.
        if (players.HasValue && players.Value < 1)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid player count",
                Detail = "Players must be greater than zero."
            });
        }

        if (age.HasValue && age.Value < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid age",
                Detail = "Age cannot be negative."
            });
        }

        // Build the query without EF Core change tracking.
        var query = _context.BoardGames
            .AsNoTracking()
            .AsQueryable();

        // Search by partial title.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();

            query = query.Where(game =>
                game.Title.Contains(searchTerm));
        }

        // Filter by genre.
        if (!string.IsNullOrWhiteSpace(genre))
        {
            var selectedGenre = genre.Trim();

            query = query.Where(game =>
                game.Genre == selectedGenre);
        }

        // Filter by supported player count.
        if (players.HasValue)
        {
            query = query.Where(game =>
                game.MinPlayers <= players.Value &&
                game.MaxPlayers >= players.Value);
        }

        // Filter by minimum age.
        if (age.HasValue)
        {
            query = query.Where(game =>
                game.MinimumAge <= age.Value);
        }

        // Count all matching games before pagination.
        var totalCount = await query.CountAsync();

        // Fetch only the requested page.
        var boardGames = await query
            .OrderBy(game => game.Title)
            .ThenBy(game => game.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(game => new BoardGameResponse
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Genre = game.Genre,
                Publisher = game.Publisher,
                Edition = game.Edition,
                ReleaseYear = game.ReleaseYear,
                MinPlayers = game.MinPlayers,
                MaxPlayers = game.MaxPlayers,
                PlayingTimeMinutes = game.PlayingTimeMinutes,
                MinimumAge = game.MinimumAge,
                RowVersion = game.RowVersion
            })
            .ToListAsync();

        // Return the games together with pagination metadata.
        return Ok(new PagedResponse<BoardGameResponse>
        {
            Items = boardGames,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }




    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardGameResponse>> GetBoardGame(Guid id)
    {
        // Find a single board game and map it to our response DTO.
        var boardGame = await _context.BoardGames
            .AsNoTracking()
            .Where(game => game.Id == id)
            .Select(game => new BoardGameResponse
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Genre = game.Genre,
                Publisher = game.Publisher,
                Edition = game.Edition,
                ReleaseYear = game.ReleaseYear,
                MinPlayers = game.MinPlayers,
                MaxPlayers = game.MaxPlayers,
                PlayingTimeMinutes = game.PlayingTimeMinutes,
                MinimumAge = game.MinimumAge,
                RowVersion = game.RowVersion
            })
            .SingleOrDefaultAsync();

        if (boardGame is null)
        {
            return NotFound();
        }

        return Ok(boardGame);
    }

    [HttpPost]
    public async Task<ActionResult<BoardGameResponse>> CreateBoardGame(
        CreateBoardGameRequest request)
    {
        // Create the board game through its domain constructor.
        var boardGame = new BoardGame(
            request.Title,
            request.Description,
            request.Genre,
            request.Publisher,
            request.Edition,
            request.ReleaseYear,
            request.MinPlayers,
            request.MaxPlayers,
            request.PlayingTimeMinutes,
            request.MinimumAge);

        _context.BoardGames.Add(boardGame);

        // SQL Server generates RowVersion when the entity is saved.
        await _context.SaveChangesAsync();

        var response = new BoardGameResponse
        {
            Id = boardGame.Id,
            Title = boardGame.Title,
            Description = boardGame.Description,
            Genre = boardGame.Genre,
            Publisher = boardGame.Publisher,
            Edition = boardGame.Edition,
            ReleaseYear = boardGame.ReleaseYear,
            MinPlayers = boardGame.MinPlayers,
            MaxPlayers = boardGame.MaxPlayers,
            PlayingTimeMinutes = boardGame.PlayingTimeMinutes,
            MinimumAge = boardGame.MinimumAge,
            RowVersion = boardGame.RowVersion
        };

        return CreatedAtAction(
            nameof(GetBoardGame),
            new { id = boardGame.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BoardGameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BoardGameResponse>> UpdateBoardGame(
        Guid id,
        UpdateBoardGameRequest request)
    {
        // Find the existing board game.
        var boardGame = await _context.BoardGames
            .SingleOrDefaultAsync(game => game.Id == id);

        if (boardGame is null)
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
                Detail = "A valid RowVersion is required when updating a board game."
            });
        }

        // Compare the client's version with the database version on save.
        _context.Entry(boardGame)
            .Property(game => game.RowVersion)
            .OriginalValue = request.RowVersion;

        // Update the board game through its domain validation.
        boardGame.Update(
            request.Title,
            request.Description,
            request.Genre,
            request.Publisher,
            request.Edition,
            request.ReleaseYear,
            request.MinPlayers,
            request.MaxPlayers,
            request.PlayingTimeMinutes,
            request.MinimumAge);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Board game was modified",
                Detail = "The board game has been modified or deleted by another request. Retrieve the latest version before updating."
            });
        }

        // Return the updated board game and its new RowVersion.
        return Ok(new BoardGameResponse
        {
            Id = boardGame.Id,
            Title = boardGame.Title,
            Description = boardGame.Description,
            Genre = boardGame.Genre,
            Publisher = boardGame.Publisher,
            Edition = boardGame.Edition,
            ReleaseYear = boardGame.ReleaseYear,
            MinPlayers = boardGame.MinPlayers,
            MaxPlayers = boardGame.MaxPlayers,
            PlayingTimeMinutes = boardGame.PlayingTimeMinutes,
            MinimumAge = boardGame.MinimumAge,
            RowVersion = boardGame.RowVersion
        });
    }


    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBoardGame(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch)
    {
        // Require the RowVersion originally retrieved by the client.
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing RowVersion",
                Detail = "The If-Match header is required when deleting a board game."
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

        // Find the existing board game.
        var boardGame = await _context.BoardGames
            .SingleOrDefaultAsync(game => game.Id == id);

        if (boardGame is null)
        {
            return NotFound();
        }

        // Prevent deletion when physical copies still exist.
        var hasItems = await _context.Items
            .AnyAsync(item => item.BoardGameId == id);

        if (hasItems)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Board game cannot be deleted",
                Detail = "This board game has physical copies registered."
            });
        }

        // Compare the client's original version with the database version.
        _context.Entry(boardGame)
            .Property(game => game.RowVersion)
            .OriginalValue = rowVersion;

        _context.BoardGames.Remove(boardGame);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Board game was modified",
                Detail = "The board game has been modified or deleted by another request. Retrieve the latest version before deleting."
            });
        }

        return NoContent();
    }

    // Get all distinct genres used by board games.
    [HttpGet("genres")]
    [ProducesResponseType(
        typeof(List<string>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetGenres()
    {
        var genres = await _context.BoardGames
            .AsNoTracking()
            .Select(boardGame => boardGame.Genre)
            .Distinct()
            .OrderBy(genre => genre)
            .ToListAsync();

        return Ok(genres);
    }

}
