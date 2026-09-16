using Domain.Exceptions.CustomExceptions;

namespace Domain.Models;


public class Loan // Represents a loan of an item to a borrower
{
    public int Id { get; set; }
    public DateOnly LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public int ItemId { get; set; }
    public int BorrowerId { get; set; }
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

    public Loan(int id, DateOnly loanDate, DateOnly dueDate, int itemId, int borrowerId)
    {
        if (loanDate < DateOnly.FromDateTime(DateTime.Today)) // Check if the loan date is in the past
        {
            throw new LoanDateInPastException();
        }

        if (dueDate <= loanDate) // Check if the due date is after the loan date
        {
            throw new DueDateException();
        }

        if (itemId <= 0) // Check if the item is already loaned 
        { throw new ItemAlreadyLoanedException();
        }

        Id = id;
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