using Domain.Models;
using Domain.ValueObjects;
using Domain.Exceptions.CustomExceptions;

namespace Domain.Tests;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldCreateUser_WhenValuesAreValid()
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
        var email = new Email("anna@example.com");
        var address = new Address(
            "Testvej",
            "10",
            "9000",
            "Aalborg",
            "Denmark");

        var dateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddYears(-20);

        // Act
        var user = new User(
            "annaj",
            name,
            email,
            dateOfBirth,
            address);

        // Assert
        Assert.Equal("annaj", user.Username);
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(dateOfBirth, user.DateOfBirth);
        Assert.Equal(address, user.Address);
    }

    [Fact]
    public void Constructor_ShouldThrowUserMinimumAgeException_WhenUserIsUnder12()
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
        var email = new Email("anna@example.com");
        var address = new Address(
            "Testvej",
            "10",
            "9000",
            "Aalborg",
            "Denmark");

        var dateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddYears(-11);

        // Act & Assert
        Assert.Throws<UserMinimumAgeException>(() =>
            new User(
                "annaj",
                name,
                email,
                dateOfBirth,
                address));
    }

    [Fact]
    public void Constructor_ShouldCreateUser_WhenUserIsExactly12()
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
        var email = new Email("anna@example.com");
        var address = new Address(
            "Testvej",
            "10",
            "9000",
            "Aalborg",
            "Denmark");

        var dateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddYears(-12);

        // Act
        var user = new User(
            "annaj",
            name,
            email,
            dateOfBirth,
            address);

        // Assert
        Assert.Equal(dateOfBirth, user.DateOfBirth);
    }

    [Fact]
    public void Constructor_ShouldThrowRequiredFieldException_WhenUsernameIsEmpty()
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
        var email = new Email("anna@example.com");
        var address = new Address(
            "Testvej",
            "10",
            "9000",
            "Aalborg",
            "Denmark");

        var dateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddYears(-20);

        // Act & Assert
        var exception = Assert.Throws<RequiredFieldException>(() =>
            new User(
                "",
                name,
                email,
                dateOfBirth,
                address));

        Assert.Equal("Username is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowMaxLengthExceededException_WhenUsernameExceedsMaxLength()
    {
        // Arrange
        var username = new string('A', 51);
        var name = new Name("John", "Doe");
        var email = new Email("john@example.com");
        var address = new Address(
            "Test Street",
            "1",
            "9000",
            "Aalborg",
            "Denmark");

        var dateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddYears(-20);

        // Act & Assert
        var exception = Assert.Throws<MaxLengthExceededException>(() =>
            new User(
                username,
                name,
                email,
                dateOfBirth,
                address));

        Assert.Equal(
            "Username cannot exceed 50 characters.",
            exception.Message);
    }
}