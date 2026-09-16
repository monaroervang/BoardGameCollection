using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class InvalidPlayingTimeException : Exception
{
    public InvalidPlayingTimeException()
        : base(ErrorMessages.InvalidPlayingTime)
    {
    }
}