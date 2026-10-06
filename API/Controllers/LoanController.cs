
using Domain.Exceptions.CustomExceptions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;
using Shared.DTOs.Loans.Requests;
using Shared.DTOs.Loans.Responses;
using Microsoft.Data.SqlClient;

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

    [HttpGet]
    [ProducesResponseType(typeof(List<LoanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LoanResponse>>> GetLoans()
    {
        var loans = await _context.Loans
            .AsNoTracking()
            .ToListAsync();

        return Ok(loans.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanResponse>> GetLoan(Guid id)
    {
        var loan = await _context.Loans
            .AsNoTracking()
            .FirstOrDefaultAsync(loan => loan.Id == id);

        if (loan is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(loan));
    }

    [HttpPost]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> CreateLoan(
        CreateLoanRequest request)
    {
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

        // Find the borrower.
        var borrower = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.BorrowerId);

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
            request.BorrowerId);

        _context.Loans.Add(loan); // Add the new loan to the context

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) // Catch database update exceptions, such as unique constraint violations
            when (ex.InnerException is SqlException sqlException // Check if the inner exception is a SQL exception
                  && sqlException.Number is 2601 or 2627 // Check for unique constraint violation error codes
                  && sqlException.Message.Contains( // Check if the error message contains the index name
                      "IX_Loans_ItemId", // Index name for the unique constraint
                      StringComparison.OrdinalIgnoreCase)) // Case-insensitive comparison
        {
            return Conflict(new ProblemDetails // Return a 409 Conflict response with details about the conflict
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Item is already borrowed",
                Detail = "This item currently has an active loan."
            });
        }

        return CreatedAtAction( // Return a 201 Created response with the location of the new loan
            nameof(GetLoan),
            new { id = loan.Id },
            ToResponse(loan));
    }


    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanResponse>> ReturnLoan(
        Guid id,
        ReturnLoanRequest request)
    {
        var loan = await _context.Loans.FindAsync(id);

        if (loan is null)
        {
            return NotFound();
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
        return Ok(ToResponse(loan));
    }



    private static LoanResponse ToResponse(Loan loan)
    {
        return new LoanResponse
        {
            Id = loan.Id,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnDate = loan.ReturnDate,
            ItemId = loan.ItemId,
            BorrowerId = loan.BorrowerId,
            IsReturned = loan.IsReturned,
            IsOverdue = loan.IsOverdue,
            RowVersion = loan.RowVersion
        };
    }

}
