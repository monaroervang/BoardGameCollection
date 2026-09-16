using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class BoardGameTitleRequiredException : Exception
{
    public BoardGameTitleRequiredException()
        : base(ErrorMessages.BoardGameTitleRequired)
    {
    }
}