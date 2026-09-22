using Domain.ValueObjects;
using Domain.Exceptions.CustomExceptions;

namespace Domain.Tests;

public class NameTests
{
    [Theory]
    [InlineData(nameof(Name.FirstName))]
    [InlineData(nameof(Name.LastName))]
    public void Constructor_ShouldThrowRequiredFieldException_WhenRequiredFieldIsEmpty(
        string fieldName)
    {
        // Arrange
        var firstName = "Anna";
        var lastName = "Jensen";

        switch (fieldName)
        {
            case nameof(Name.FirstName):
                firstName = "";
                break;

            case nameof(Name.LastName):
                lastName = "";
                break;
        }

        // Act & Assert
        var exception = Assert.Throws<RequiredFieldException>(() =>
            new Name(firstName, lastName));

        Assert.Equal($"{fieldName} is required.", exception.Message);
    }

    [Theory]
    [InlineData("FirstName", 100)]
    [InlineData("LastName", 100)]
    public void Constructor_ShouldThrowMaxLengthExceededException_WhenFieldExceedsMaxLength(
    string fieldName,
    int maxLength)
    {
        // Arrange
        var value = new string('A', maxLength + 1);

        // Act & Assert
        var exception = Assert.Throws<MaxLengthExceededException>(() =>
            new Name(
                fieldName == "FirstName" ? value : "John",
                fieldName == "LastName" ? value : "Doe"));

        Assert.Equal(
            $"{fieldName} cannot exceed {maxLength} characters.",
            exception.Message);
    }
}
