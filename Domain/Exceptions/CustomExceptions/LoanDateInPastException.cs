using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class LoanDateInPastException : Exception
{
    public LoanDateInPastException()
        : base(ErrorMessages.LoanDateInPast)
    {
    }
}