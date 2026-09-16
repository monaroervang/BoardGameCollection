using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class DueDateException : Exception
{
    public DueDateException()
        : base(ErrorMessages.DueDateMustBeAfterLoanDate)
    {
    }
}
