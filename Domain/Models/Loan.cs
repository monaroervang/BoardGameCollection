
using Domain.Exceptions.CustomExceptions;

namespace Domain.Models;

public class Loan // Represents a loan of an item to a borrower
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public DateOnly LoanDate { get; private set; }

    public DateOnly DueDate { get; private set; }

    public DateOnly? ReturnDate { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid BorrowerId { get; private set; }

    public bool IsReturned => ReturnDate.HasValue;

    // Used by EF Core for optimistic concurrency control.
    public byte[] RowVersion { get; private set; } = [];

    public bool IsOverdue
    {
        get
        {
            if (ReturnDate.HasValue)
            {
                return ReturnDate.Value > DueDate;
            }

            return DateOnly.FromDateTime(DateTime.Today) > DueDate;
        }
    }

    // Required by EF Core.
    public Loan()
    {
    }

    public Loan(
        DateOnly loanDate,
        DateOnly dueDate,
        Guid itemId,
        Guid borrowerId)
    {
        if (loanDate < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new LoanDateInPastException();
        }

        if (dueDate <= loanDate)
        {
            throw new DueDateException();
        }

        // New loans must have a duration of exactly 14 or 30 days.
        var loanDuration = dueDate.DayNumber - loanDate.DayNumber;

        if (loanDuration != 14 && loanDuration != 30)
        {
            throw new ArgumentException(
                "Loan duration must be exactly 14 or 30 days.",
                nameof(dueDate));
        }

        LoanDate = loanDate;
        DueDate = dueDate;
        ItemId = itemId;
        BorrowerId = borrowerId;
    }

    // Set the return date of an existing loan.
    public void ReturnLoan(DateOnly returnDate)
    {
        if (returnDate < LoanDate)
        {
            throw new ReturnDateException();
        }

        ReturnDate = returnDate;
    }

    public override string ToString()
    {
        return $"Loan {Id}: Item {ItemId}, Borrower {BorrowerId}, " +
               $"From {LoanDate} to {DueDate}, Returned: {ReturnDate}";
    }

    // Create historical loans for development seed data.
    // Historical loans may have different durations.
    public static Loan CreateHistorical(
        DateOnly loanDate,
        DateOnly dueDate,
        Guid itemId,
        Guid borrowerId,
        DateOnly? returnDate = null)
    {
        if (dueDate <= loanDate)
        {
            throw new DueDateException();
        }

        if (returnDate.HasValue && returnDate.Value < loanDate)
        {
            throw new ReturnDateException();
        }

        return new Loan
        {
            LoanDate = loanDate,
            DueDate = dueDate,
            ItemId = itemId,
            BorrowerId = borrowerId,
            ReturnDate = returnDate
        };
    }
}
