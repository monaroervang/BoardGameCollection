
using System.Security.Claims;
using Domain.Exceptions.CustomExceptions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.Loans.Requests;
using Shared.DTOs.Loans.Responses;

namespace API.Controllers;

[ApiController]
[Route("api/loans")]
public class LoanController : ControllerBase
{
    private readonly BoardGameDbContext _context;

    public LoanController(BoardGameDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(List<LoanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<LoanResponse>>> GetLoans()
    {
        var loans = await _context.Loans
            .AsNoTracking()
            .ToListAsync();

        return Ok(await ToResponsesAsync(loans));
    }

    [Authorize]
    [HttpGet("my-loans")]
    [ProducesResponseType(typeof(List<LoanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<LoanResponse>>> GetMyLoans()
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        // Return only loans where the authenticated user is the borrower.
        // Include both active and returned loans.
        var loans = await _context.Loans
            .AsNoTracking()
            .Where(loan => loan.BorrowerId == currentUserId.Value)
            .OrderByDescending(loan => loan.LoanDate)
            .ThenByDescending(loan => loan.Id)
            .ToListAsync();

        return Ok(await ToResponsesAsync(loans));
    }

    [Authorize]
    [HttpGet("my-lendings")]
    [ProducesResponseType(typeof(List<LoanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<LoanResponse>>> GetMyLendings()
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        // Return loans for physical copies owned by the authenticated user.
        // Include both active and returned loans.
        var loans = await _context.Loans
            .AsNoTracking()
            .Where(loan => _context.Items.Any(item =>
                item.Id == loan.ItemId &&
                item.OwnerId == currentUserId.Value))
            .OrderByDescending(loan => loan.LoanDate)
            .ThenByDescending(loan => loan.Id)
            .ToListAsync();

        return Ok(await ToResponsesAsync(loans));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanResponse>> GetLoan(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var loan = await _context.Loans
            .AsNoTracking()
            .FirstOrDefaultAsync(loan => loan.Id == id);

        if (loan is null)
        {
            return NotFound();
        }

        // Administrators and borrowers can view the loan.
        if (User.IsInRole("Admin") ||
            loan.BorrowerId == currentUserId.Value)
        {
            return Ok(await ToResponseAsync(loan));
        }

        // The owner of the physical copy can also view the loan.
        var isOwner = await _context.Items
            .AnyAsync(item =>
                item.Id == loan.ItemId &&
                item.OwnerId == currentUserId.Value);

        if (!isOwner)
        {
            return Forbid();
        }

        return Ok(await ToResponseAsync(loan));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> CreateLoan(
        CreateLoanRequest request)
    {
        // The borrower is always determined by the authenticated JWT.
        var borrowerId = GetCurrentUserId();

        if (borrowerId is null)
        {
            return Unauthorized();
        }

        // Validate dates before checking related entities.
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (request.LoanDate < today)
        {
            throw new LoanDateInPastException();
        }

        if (request.DueDate <= request.LoanDate)
        {
            throw new DueDateException();
        }

        // Find the physical copy.
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.ItemId);

        if (item is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Item not found"
            });
        }

        // Archived physical copies cannot be borrowed.
        if (item.IsArchived)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item is archived",
                Detail = "Archived items cannot be borrowed. The owner must reactivate the item first."
            });
        }

        // Find the authenticated borrower.
        var borrower = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == borrowerId.Value);

        if (borrower is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Borrower not found"
            });
        }

        // Find the board game to check its minimum age.
        var boardGame = await _context.BoardGames
            .AsNoTracking()
            .FirstOrDefaultAsync(
                boardGame => boardGame.Id == item.BoardGameId);

        if (boardGame is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Board game not found"
            });
        }

        // Check the borrower's age on the loan date.
        var latestAllowedBirthDate = request.LoanDate
            .AddYears(-boardGame.MinimumAge);

        if (borrower.DateOfBirth > latestAllowedBirthDate)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Borrower does not meet the minimum age",
                Detail = "The borrower is too young for this board game."
            });
        }

        // Check whether this copy already has an active loan.
        var isAlreadyBorrowed = await _context.Loans
            .AnyAsync(loan =>
                loan.ItemId == request.ItemId &&
                loan.ReturnDate == null);

        if (isAlreadyBorrowed)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item is already borrowed",
                Detail = "This item currently has an active loan."
            });
        }

        // Domain validates the loan dates as well.
        var loan = new Loan(
            request.LoanDate,
            request.DueDate,
            request.ItemId,
            borrowerId.Value);

        _context.Loans.Add(loan);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlException
                  && sqlException.Number is 2601 or 2627
                  && sqlException.Message.Contains(
                      "IX_Loans_ItemId",
                      StringComparison.OrdinalIgnoreCase))
        {
            // The unique filtered index prevents concurrent active loans.
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item is already borrowed",
                Detail = "This item currently has an active loan."
            });
        }

        return CreatedAtAction(
            nameof(GetLoan),
            new { id = loan.Id },
            ToResponse(loan, boardGame.Title));
    }

    [Authorize]
    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> ReturnLoan(
        Guid id,
        ReturnLoanRequest request)
    {
        // Get the authenticated user's ID from the JWT.
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var loan = await _context.Loans.FindAsync(id);

        if (loan is null)
        {
            return NotFound();
        }

        // Check whether the authenticated user is the borrower.
        var isBorrower = loan.BorrowerId == currentUserId.Value;

        // Check whether the authenticated user owns the physical copy.
        var isOwner = await _context.Items
            .AnyAsync(item =>
                item.Id == loan.ItemId &&
                item.OwnerId == currentUserId.Value);

        // Only the borrower, owner or administrator may return the loan.
        if (!User.IsInRole("Admin") && !isBorrower && !isOwner)
        {
            return Forbid();
        }

        // A returned loan cannot be returned again.
        if (loan.IsReturned)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Loan is already returned"
            });
        }

        // Require the version originally retrieved by the client.
        if (request.RowVersion is null || request.RowVersion.Length != 8)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid RowVersion",
                Detail = "A valid RowVersion is required when returning a loan."
            });
        }

        // Use the client's original version for the concurrency check.
        _context.Entry(loan)
            .Property(l => l.RowVersion)
            .OriginalValue = request.RowVersion;

        // Domain validates that the return date is not before the loan date.
        loan.ReturnLoan(request.ReturnDate);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Loan was modified",
                Detail = "The loan has been modified by another request. Retrieve the latest version before returning it."
            });
        }

        // Return the loan with its newly generated RowVersion.
        return Ok(await ToResponseAsync(loan));
    }

    // Read the user ID from the validated authentication token.
    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    // Load titles for all loans in one database query.
    private async Task<List<LoanResponse>> ToResponsesAsync(
        List<Loan> loans)
    {
        if (loans.Count == 0)
        {
            return [];
        }

        var itemIds = loans
            .Select(loan => loan.ItemId)
            .Distinct()
            .ToList();

        var titles = await (
            from item in _context.Items.AsNoTracking()
            join boardGame in _context.BoardGames.AsNoTracking()
                on item.BoardGameId equals boardGame.Id
            where itemIds.Contains(item.Id)
            select new
            {
                item.Id,
                boardGame.Title
            })
            .ToDictionaryAsync(
                entry => entry.Id,
                entry => entry.Title);

        return loans
            .Select(loan => ToResponse(
                loan,
                titles.GetValueOrDefault(loan.ItemId) ?? ""))
            .ToList();
    }

    // Load the title for a single loan.
    private async Task<LoanResponse> ToResponseAsync(Loan loan)
    {
        var title = await (
            from item in _context.Items.AsNoTracking()
            join boardGame in _context.BoardGames.AsNoTracking()
                on item.BoardGameId equals boardGame.Id
            where item.Id == loan.ItemId
            select boardGame.Title)
            .FirstOrDefaultAsync() ?? "";

        return ToResponse(loan, title);
    }

    // Map a loan to its API response.
    private static LoanResponse ToResponse(
        Loan loan,
        string boardGameTitle)
    {
        return new LoanResponse
        {
            Id = loan.Id,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnDate = loan.ReturnDate,
            ItemId = loan.ItemId,
            BoardGameTitle = boardGameTitle,
            BorrowerId = loan.BorrowerId,
            IsReturned = loan.IsReturned,
            IsOverdue = loan.IsOverdue,
            RowVersion = loan.RowVersion
        };
    }
}
