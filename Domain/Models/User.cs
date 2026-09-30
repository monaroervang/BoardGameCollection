using Domain.Exceptions.CustomExceptions;
using Domain.ValueObjects;

public class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public byte[] RowVersion { get; private set; } = []; // Used for optimistic concurrency control
    public string Username { get; private set; } = "";
    public Name Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public Address Address { get; private set; } = null!;

    // Used by EF Core when materializing users from the database.
    private User()
    {
    }

    public User(
        string username,
        Name name,
        Email email,
        DateOnly dateOfBirth,
        Address address)
    {
        Update(username, name, email, dateOfBirth, address);
    }

    public void Update(
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
        {
            throw new MaxLengthExceededException(nameof(Username), 50);
        }

        Username = username;
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
        Address = address;
    }
}