using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions;

public class UserMinimumAgeException : Exception
{
    public UserMinimumAgeException()
        : base(ErrorMessages.UserMinimumAge)
    {
    }
}