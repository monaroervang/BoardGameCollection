using Domain.Exceptions.CustomExceptions;
using Domain.Models;

namespace Domain.Tests;

public class LoanTests
{
    private static Guid CreateItemId() => Guid.CreateVersion7();
    private static Guid CreateBorrowerId() => Guid.CreateVersion7();

    [Fact]
    public void Constructor_ShouldCreateLoan_WhenDatesAreValid()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today); 
        var dueDate = loanDate.AddDays(7);
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();

        // Act
        var loan = new Loan( loanDate, dueDate, itemId, borrowerId); // Create a new loan with valid dates

        // Assert
        Assert.Equal(loanDate, loan.LoanDate);
        Assert.Equal(dueDate, loan.DueDate);    
        Assert.Equal(itemId, loan.ItemId);
        Assert.Equal(borrowerId, loan.BorrowerId);
        Assert.Null(loan.ReturnDate);
    }

    [Fact]
    public void Constructor_ShouldThrowLoanDateInPastException_WhenLoanDateIsInPast()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var dueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7);
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();

        // Act & Assert
        Assert.Throws<LoanDateInPastException>(() => 
            new Loan(loanDate, dueDate, itemId, borrowerId)); // Expect a LoanDateInPastException to be thrown or failed test
    }

    [Fact]
    public void Constructor_ShouldThrowDueDateException_WhenDueDateIsNotAfterLoanDate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate; //  Set due date to be the same as loan date
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();


        // Act & Assert
        Assert.Throws<DueDateException>(() =>
            new Loan(loanDate, dueDate, itemId, borrowerId)); // Expect a DueDateException to be thrown or failed test
    }

    [Fact]
    public void ReturnLoan_ShouldSetReturnDate_WhenDateIsValid()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(7); //   Set due date to be 7 days after loan date
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();



        var loan = new Loan(loanDate, dueDate, itemId, borrowerId); //   Create a new loan with valid dates

        var returnDate = loanDate.AddDays(5); //   Set return date to be 5 days after loan date

        // Act
        loan.ReturnLoan(returnDate); //   Call the ReturnLoan method to set the return date

        // Assert
        Assert.Equal(returnDate, loan.ReturnDate); //   Check if the return date is set correctly
    }

    [Fact]
    public void ReturnLoan_ShouldThrowReturnDateException_WhenReturnDateIsBeforeLoanDate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(7);
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();

        var loan = new Loan(loanDate, dueDate, itemId, borrowerId); //   Create a new loan with valid dates

        var returnDate = loanDate.AddDays(-1); //   Set return date to be before loan date

        // Act & Assert
        Assert.Throws<ReturnDateException>(() =>
            loan.ReturnLoan(returnDate)); // Expect a ReturnDateException to be thrown or failed test
    }
    [Fact]
    public void IsReturned_ShouldBeTrue_WhenReturnDateIsSet()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(7);
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();
        var loan = new Loan(loanDate, dueDate, itemId, borrowerId); //   Create a new loan with valid dates

        // Act
        loan.ReturnLoan(loanDate.AddDays(3)); //  Call the ReturnLoan method to set the return date

        // Assert
        Assert.True(loan.IsReturned); //  Check if the IsReturned property is true when return date is set
    }

    [Fact]
    public void IsOverdue_ShouldBeTrue_WhenLoanIsReturnedLate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(7); //   Set due date to be 7 days after loan date
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();
        var loan = new Loan(loanDate, dueDate, itemId, borrowerId);

        // Act
        loan.ReturnLoan(dueDate.AddDays(2)); // Call the ReturnLoan method to set the return date after the due date

        // Assert
        Assert.True(loan.IsOverdue); // Check if the IsOverdue property is true when the loan is returned late
    }

    [Fact]
    public void IsOverdue_ShouldBeTrue_WhenLoanIsNotReturnedAndDueDateHasPassed()
    {
        // Arrange
        // Uses the parameterless constructor to simulate an existing loan from the database.
        // This avoids the validation that prevents creating new loans with a past LoanDate.
        // Create a historical loan to test an overdue, unreturned loan.
        var loanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-10);
        var dueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-3);

        var loan = Loan.CreateHistorical(
            loanDate,
            dueDate,
            CreateItemId(),
            CreateBorrowerId());

        // Act
        var result = loan.IsOverdue; // Check if the IsOverdue property is true when the loan is not returned and the due date has passed

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOverdue_ShouldBeFalse_WhenLoanIsReturnedOnTime()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(7); //   Set due date to be 7 days after loan date
        var itemId = Guid.CreateVersion7();
        var borrowerId = Guid.CreateVersion7();
        var loan = new Loan(loanDate, dueDate, itemId, borrowerId); //   Create a new loan with valid dates

        // Act
        loan.ReturnLoan(dueDate); // Call the ReturnLoan method to set the return date on the due date

        // Assert
        Assert.False(loan.IsOverdue); //    Check if the IsOverdue property is false when the loan is returned on time
    }
}