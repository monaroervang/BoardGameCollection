using Domain.Exceptions.CustomExceptions;
using Domain.ValueObjects;
using Domain.Enums;

public class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public byte[] RowVersion { get; private set; } = []; // Used for optimistic concurrency control
    public string Username { get; private set; } = "";
    public Name Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public Address Address { get; private set; } = null!;
    public string PasswordHash { get; private set; } = ""; // Store the hashed password
    public UserRole Role { get; private set; } = UserRole.User; // Default role is User

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

    // Sets the hashed password used for authentication.
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new RequiredFieldException(nameof(PasswordHash));
        }

        PasswordHash = passwordHash;
    }

    // Change the authorization role assigned to the user.
    public void SetRole(UserRole role)
    {
        Role = role;
    }
}