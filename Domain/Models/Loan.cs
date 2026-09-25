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

    public Loan() //Empty EF-core constructor
    {
    }

    public Loan(
    DateOnly loanDate,
    DateOnly dueDate,
    Guid itemId,
    Guid borrowerId)
    {
        if (loanDate < DateOnly.FromDateTime(DateTime.Today))
            throw new LoanDateInPastException();

        if (dueDate <= loanDate)
            throw new DueDateException();

        LoanDate = loanDate;
        DueDate = dueDate;
        ItemId = itemId;
        BorrowerId = borrowerId;
    }

    public void ReturnLoan(DateOnly returnDate) // Method to set the return date of the loan
    {
        if (returnDate < LoanDate)
        {
            throw new ReturnDateException();
        }

        ReturnDate = returnDate;
    }

    public override string ToString() // Override the ToString method to display the loan information
    {
        return $"Loan {Id}: Item {ItemId}, Borrower {BorrowerId}, " +
               $"From {LoanDate} to {DueDate}, Returned: {ReturnDate}";
    }

    // Create historical loans for development seed data.
    public static Loan CreateHistorical(
        DateOnly loanDate,
        DateOnly dueDate,
        Guid itemId,
        Guid borrowerId,
        DateOnly? returnDate = null)
    {
        if (dueDate <= loanDate)
            throw new DueDateException();

        if (returnDate.HasValue && returnDate.Value < loanDate)
            throw new ReturnDateException();

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