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
            1,
            "annaj",
            name,
            "anna@example.com",
            dateOfBirth,
            address);

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("annaj", user.Username);
        Assert.Equal(name, user.Name);
        Assert.Equal("anna@example.com", user.Email);
        Assert.Equal(dateOfBirth, user.DateOfBirth);
        Assert.Equal(address, user.Address);
    }

    [Fact]
    public void Constructor_ShouldThrowUserMinimumAgeException_WhenUserIsUnder12()
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
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
                1,
                "annaj",
                name,
                "anna@example.com",
                dateOfBirth,
                address));
    }
    [Fact]
    public void Constructor_ShouldCreateUser_WhenUserIsExactly12()
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
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
            1,
            "annaj",
            name,
            "anna@example.com",
            dateOfBirth,
            address);

        // Assert
        Assert.Equal(dateOfBirth, user.DateOfBirth);
    }
    [Theory]
    [InlineData(nameof(User.Username))]
    [InlineData(nameof(User.Email))]
    public void Constructor_ShouldThrowRequiredFieldException_WhenRequiredFieldIsEmpty(
    string fieldName)
    {
        // Arrange
        var name = new Name("Anna", "Jensen");
        var address = new Address(
            "Testvej",
            "10",
            "9000",
            "Aalborg",
            "Denmark");

        var username = "annaj";
        var email = "anna@example.com";
        var dateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddYears(-20);

        switch (fieldName)
        {
            case nameof(User.Username):
                username = "";
                break;

            case nameof(User.Email):
                email = "";
                break;
        }

        // Act & Assert
        var exception = Assert.Throws<RequiredFieldException>(() =>
            new User(
                1,
                username,
                name,
                email,
                dateOfBirth,
                address));

        Assert.Equal($"{fieldName} is required.", exception.Message);
    }
}