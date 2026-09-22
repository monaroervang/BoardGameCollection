using Domain.Exceptions.CustomExceptions;

namespace Domain.Models;


public class Loan // Represents a loan of an item to a borrower
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public DateOnly LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public Guid ItemId { get; set; }
    public Guid BorrowerId { get; set; }
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
}