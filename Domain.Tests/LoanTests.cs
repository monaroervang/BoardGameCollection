
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
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        // Act
        var loan = new Loan(
            loanDate,
            dueDate,
            itemId,
            borrowerId);

        // Assert
        Assert.Equal(loanDate, loan.LoanDate);
        Assert.Equal(dueDate, loan.DueDate);
        Assert.Equal(itemId, loan.ItemId);
        Assert.Equal(borrowerId, loan.BorrowerId);
        Assert.Null(loan.ReturnDate);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(30)]
    public void Constructor_ShouldCreateLoan_WhenDurationIsAllowed(
        int durationDays)
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(durationDays);

        // Act
        var loan = new Loan(
            loanDate,
            dueDate,
            CreateItemId(),
            CreateBorrowerId());

        // Assert
        Assert.Equal(durationDays,
            loan.DueDate.DayNumber - loan.LoanDate.DayNumber);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(60)]
    public void Constructor_ShouldThrowArgumentException_WhenDurationIsNotAllowed(
        int durationDays)
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(durationDays);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Loan(
                loanDate,
                dueDate,
                CreateItemId(),
                CreateBorrowerId()));

        Assert.Equal("dueDate", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowLoanDateInPastException_WhenLoanDateIsInPast()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        // Act & Assert
        Assert.Throws<LoanDateInPastException>(() =>
            new Loan(loanDate, dueDate, itemId, borrowerId));
    }

    [Fact]
    public void Constructor_ShouldThrowDueDateException_WhenDueDateIsNotAfterLoanDate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate;
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        // Act & Assert
        Assert.Throws<DueDateException>(() =>
            new Loan(loanDate, dueDate, itemId, borrowerId));
    }

    [Fact]
    public void ReturnLoan_ShouldSetReturnDate_WhenDateIsValid()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        var loan = new Loan(
            loanDate,
            dueDate,
            itemId,
            borrowerId);

        var returnDate = loanDate.AddDays(5);

        // Act
        loan.ReturnLoan(returnDate);

        // Assert
        Assert.Equal(returnDate, loan.ReturnDate);
    }

    [Fact]
    public void ReturnLoan_ShouldThrowReturnDateException_WhenReturnDateIsBeforeLoanDate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        var loan = new Loan(
            loanDate,
            dueDate,
            itemId,
            borrowerId);

        var returnDate = loanDate.AddDays(-1);

        // Act & Assert
        Assert.Throws<ReturnDateException>(() =>
            loan.ReturnLoan(returnDate));
    }

    [Fact]
    public void IsReturned_ShouldBeTrue_WhenReturnDateIsSet()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        var loan = new Loan(
            loanDate,
            dueDate,
            itemId,
            borrowerId);

        // Act
        loan.ReturnLoan(loanDate.AddDays(3));

        // Assert
        Assert.True(loan.IsReturned);
    }

    [Fact]
    public void IsOverdue_ShouldBeTrue_WhenLoanIsReturnedLate()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        var loan = new Loan(
            loanDate,
            dueDate,
            itemId,
            borrowerId);

        // Act
        loan.ReturnLoan(dueDate.AddDays(2));

        // Assert
        Assert.True(loan.IsOverdue);
    }

    [Fact]
    public void IsOverdue_ShouldBeTrue_WhenLoanIsNotReturnedAndDueDateHasPassed()
    {
        // Arrange
        // Create a historical loan with a past due date.
        var loanDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-10);
        var dueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-3);

        var loan = Loan.CreateHistorical(
            loanDate,
            dueDate,
            CreateItemId(),
            CreateBorrowerId());

        // Act
        var result = loan.IsOverdue;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOverdue_ShouldBeFalse_WhenLoanIsReturnedOnTime()
    {
        // Arrange
        var loanDate = DateOnly.FromDateTime(DateTime.Today);
        var dueDate = loanDate.AddDays(14);
        var itemId = CreateItemId();
        var borrowerId = CreateBorrowerId();

        var loan = new Loan(
            loanDate,
            dueDate,
            itemId,
            borrowerId);

        // Act
        loan.ReturnLoan(dueDate);

        // Assert
        Assert.False(loan.IsOverdue);
    }
}
