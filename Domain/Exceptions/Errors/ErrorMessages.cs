using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions.Errors;

public static class ErrorMessages
{
    public const string LoanDateInPast =
    "Loan date cannot be in the past.";
    public const string DueDateMustBeAfterLoanDate =
    "Due date must be after loan date.";
    public const string ItemAlreadyLoaned =
    "Item is already loaned for the selected period.";
    public const string ReturnDateBeforeLoanDate =
    "Return date cannot be before loan date.";
    public const string InvalidPlayerCount =
    "Player count must be greater than zero, and maximum players cannot be less than minimum players.";
    public const string BoardGameTitleRequired =
    "Board game title is required.";
    public const string InvalidReleaseYear =
    "Release year must be a valid year and cannot be in the future.";
    public const string InvalidPlayingTime =
    "Playing time must be greater than zero.";
    public const string InvalidMinimumAge =
    "Minimum age cannot be negative.";
    public const string RequiredField =
    "{0} is required."; // Placeholder for field name
}
