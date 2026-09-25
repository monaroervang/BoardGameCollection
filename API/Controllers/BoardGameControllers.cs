
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.BoardGames.Responses;
using Domain.Models;
using Shared.DTOs.BoardGames.Requests;

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
    [HttpGet]
    public async Task<ActionResult<List<BoardGameResponse>>> GetBoardGames()
    {
        // Return board games as DTOs without EF Core change tracking.
        var boardGames = await _context.BoardGames
            .AsNoTracking()
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
                MinimumAge = game.MinimumAge
            })
            .ToListAsync();

        return Ok(boardGames);
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
                MinimumAge = game.MinimumAge
            })
            .SingleOrDefaultAsync();

        // Return 404 if the requested board game does not exist.
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
        // Create a domain entity using the validated request data.
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

        // Save the new board game to the database.
        _context.BoardGames.Add(boardGame);
        await _context.SaveChangesAsync();

        // Return the created board game using our shared response DTO.
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
            MinimumAge = boardGame.MinimumAge
        };

        // Return 201 Created and the URL of the new resource.
        return CreatedAtAction(
            nameof(GetBoardGame),
            new { id = boardGame.Id },
            response);
    }


    [HttpPut("{id:guid}")]
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

        await _context.SaveChangesAsync();

        // Return the updated board game.
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
            MinimumAge = boardGame.MinimumAge
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBoardGame(Guid id)
    {
        // Find the board game to delete.
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

        // Delete the board game when no physical copies exist.
        _context.BoardGames.Remove(boardGame);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}