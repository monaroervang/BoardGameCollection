using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class InvalidPlayerCountException : Exception
{
    public InvalidPlayerCountException()
        : base(ErrorMessages.InvalidPlayerCount)
    {
    }
}