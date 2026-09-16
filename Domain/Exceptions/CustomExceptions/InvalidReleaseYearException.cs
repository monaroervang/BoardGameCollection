using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class InvalidReleaseYearException : Exception
{
    public InvalidReleaseYearException()
        : base(ErrorMessages.InvalidReleaseYear)
    {
    }
}