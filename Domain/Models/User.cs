using Domain.Exceptions.CustomExceptions;
using Domain.ValueObjects;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public Name Name { get; set; }
    public string Email { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public Address Address { get; set; }

    public User()
    {
    }

    public User(
    int id,
    string username,
    Name name,
    string email,
    DateOnly dateOfBirth,
    Address address)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new RequiredFieldException(nameof(Username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new RequiredFieldException(nameof(Email));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var minimumBirthDate = today.AddYears(-12); // Minimum age of 12 years

        if (dateOfBirth > minimumBirthDate)
        {
            throw new UserMinimumAgeException();
        }

        Id = id;
        Username = username;
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
        Address = address;
    }


}