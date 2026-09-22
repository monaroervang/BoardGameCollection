using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class InvalidEmailException : Exception
{
    public InvalidEmailException()
        : base(ErrorMessages.InvalidEmail)
    {
    }
}