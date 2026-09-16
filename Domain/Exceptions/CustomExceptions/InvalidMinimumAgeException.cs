using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class InvalidMinimumAgeException : Exception
{
    public InvalidMinimumAgeException()
        : base(ErrorMessages.InvalidMinimumAge)
    {
    }
}