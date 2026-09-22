using System.Net.Mail; // For email validation that is more robust than regex
using Domain.Exceptions.CustomExceptions;

namespace Domain.ValueObjects;

public class Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new RequiredFieldException(nameof(Email));

        if (value.Length > 254)
            throw new MaxLengthExceededException(nameof(Email), 254);

        if (!MailAddress.TryCreate(value, out var mailAddress) ||
            mailAddress.Address != value)
        {
            throw new InvalidEmailException();
        }

        Value = value;
    }
}