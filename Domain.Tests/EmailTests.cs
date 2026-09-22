using Domain.Exceptions.CustomExceptions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Constructor_ShouldCreateEmail_WhenEmailIsValid()
    {
        // Arrange
        var value = "john@example.com";

        // Act
        var email = new Email(value);

        // Assert
        Assert.Equal(value, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrowRequiredFieldException_WhenEmailIsEmpty(
        string value)
    {
        // Act & Assert
        Assert.Throws<RequiredFieldException>(() => new Email(value));
    }

    [Fact]
    public void Constructor_ShouldThrowMaxLengthExceededException_WhenEmailExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', 250) + "@test.com";

        // Act & Assert
        Assert.Throws<MaxLengthExceededException>(() => new Email(value));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public void Constructor_ShouldThrowInvalidEmailException_WhenEmailFormatIsInvalid(
        string value)
    {
        // Act & Assert
        Assert.Throws<InvalidEmailException>(() => new Email(value));
    }
}