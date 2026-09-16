using Domain.Exceptions.CustomExceptions;
using Domain.Models;

namespace Domain.Tests;

public class LoanTests
{
    [Fact]
    public void Constructor_ShouldCreateLoan_WhenDatesAreValid()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today); 
        var dueDate = loanDate.AddDays(7);

        // Act
        var loan = new Loan(1, loanDate, dueDate, 10, 20); // Create a new loan with valid dates

        // Assert
        Assert.Equal(1, loan.Id); // Check if the loan ID is set correctly
        Assert.Equal(loanDate, loan.LoanDate);
        Assert.Equal(dueDate, loan.DueDate);    
        Assert.Equal(10, loan.ItemId);
        Assert.Equal(20, loan.BorrowerId);
        Assert.Null(loan.ReturnDate);
    }

    [Fact]
    public void Constructor_ShouldThrowLoanDateInPastException_WhenLoanDateIsInPast()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var dueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7);

        // Act & Assert
        Assert.Throws<LoanDateInPastException>(() => 
            new Loan(1, loanDate, dueDate, 10, 20)); // Expect a LoanDateInPastException to be thrown or failed test
    }

    [Fact]
    public void Constructor_ShouldThrowDueDateException_WhenDueDateIsNotAfterLoanDate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate; //  Set due date to be the same as loan date

        // Act & Assert
        Assert.Throws<DueDateException>(() =>
            new Loan(1, loanDate, dueDate, 10, 20)); // Expect a DueDateException to be thrown or failed test
    }

    [Fact]
    public void ReturnLoan_ShouldSetReturnDate_WhenDateIsValid()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(7); //   Set due date to be 7 days after loan date
        var loan = new Loan(1, loanDate, dueDate, 10, 20); //   Create a new loan with valid dates

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
        var loan = new Loan(1, loanDate, dueDate, 10, 20); //   Create a new loan with valid dates

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
        var loan = new Loan(1, loanDate, dueDate, 10, 20); //   Create a new loan with valid dates

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
        var loan = new Loan(1, loanDate, dueDate, 10, 20);

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
        var loan = new Loan
        {
            LoanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-10), // Set loan date to be 10 days in the past
            DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-3) // Set due date to be 3 days in the past
        };

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
        var loan = new Loan(1, loanDate, dueDate, 10, 20); //   Create a new loan with valid dates

        // Act
        loan.ReturnLoan(dueDate); // Call the ReturnLoan method to set the return date on the due date

        // Assert
        Assert.False(loan.IsOverdue); //    Check if the IsOverdue property is false when the loan is returned on time
    }
}