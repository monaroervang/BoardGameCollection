using Domain.Exceptions.CustomExceptions;

namespace Domain.ValueObjects;

public class Name
{
    public string FirstName { get; }
    public string LastName { get; }

    public Name(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new RequiredFieldException(nameof(FirstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new RequiredFieldException(nameof(LastName));
        }

        if (firstName.Length > 100)
            throw new MaxLengthExceededException(nameof(FirstName), 100);

        if (lastName.Length > 100)
            throw new MaxLengthExceededException(nameof(LastName), 100);

        FirstName = firstName;
        LastName = lastName;
    }
}