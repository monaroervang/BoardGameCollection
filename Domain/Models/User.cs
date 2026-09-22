using Domain.Exceptions.CustomExceptions;
using Domain.ValueObjects;

public class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Username { get; set; }
    public Name Name { get; set; }
    public Email Email { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public Address Address { get; set; }

    public User()
    {
    }

    public User(
    string username,
    Name name,
    Email email,
    DateOnly dateOfBirth,
    Address address)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new RequiredFieldException(nameof(Username));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var minimumBirthDate = today.AddYears(-12); // Minimum age of 12 years

        if (dateOfBirth > minimumBirthDate)
        {
            throw new UserMinimumAgeException();
        }

        if (username.Length > 50)
            throw new MaxLengthExceededException(nameof(Username), 50);

        Username = username;
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
        Address = address;
    }


}