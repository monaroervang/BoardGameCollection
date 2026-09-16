using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class ReturnDateException : Exception
{
    public ReturnDateException()
        : base(ErrorMessages.ReturnDateBeforeLoanDate)
    {
    }
}