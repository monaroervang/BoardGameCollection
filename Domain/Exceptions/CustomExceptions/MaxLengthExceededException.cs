using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class MaxLengthExceededException : Exception
{
    public MaxLengthExceededException(string fieldName, int maxLength)
        : base(string.Format(
            ErrorMessages.MaxLengthExceeded,
            fieldName,
            maxLength))
    {
    }
}