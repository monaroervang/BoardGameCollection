using Domain.ValueObjects;
using Domain.Exceptions.CustomExceptions;

namespace Domain.Tests;

public class AddressTests
{
    [Theory]
    [InlineData(nameof(Address.Street))]
    [InlineData(nameof(Address.HouseNumber))]
    [InlineData(nameof(Address.PostalCode))]
    [InlineData(nameof(Address.City))]
    [InlineData(nameof(Address.Country))]
    public void Constructor_ShouldThrowRequiredFieldException_WhenRequiredFieldIsEmpty(
        string fieldName)
    {
        // Arrange
        var street = "Testvej";
        var houseNumber = "10";
        var postalCode = "9000";
        var city = "Aalborg";
        var country = "Denmark";

        switch (fieldName)
        {
            case nameof(Address.Street):
                street = "";
                break;

            case nameof(Address.HouseNumber):
                houseNumber = "";
                break;

            case nameof(Address.PostalCode):
                postalCode = "";
                break;

            case nameof(Address.City):
                city = "";
                break;

            case nameof(Address.Country):
                country = "";
                break;
        }

        // Act & Assert
        var exception = Assert.Throws<RequiredFieldException>(() =>
            new Address(
                street,
                houseNumber,
                postalCode,
                city,
                country));

        Assert.Equal($"{fieldName} is required.", exception.Message);
    }

    [Theory]
    [InlineData("Street", 150)]
    [InlineData("HouseNumber", 20)]
    [InlineData("PostalCode", 20)]
    [InlineData("City", 100)]
    [InlineData("Country", 100)]
    public void Constructor_ShouldThrowMaxLengthExceededException_WhenFieldExceedsMaxLength(
    string fieldName,
    int maxLength)
    {
        // Arrange
        var value = new string('A', maxLength + 1);

        // Act & Assert
        var exception = Assert.Throws<MaxLengthExceededException>(() =>
            new Address(
                fieldName == "Street" ? value : "Test Street",
                fieldName == "HouseNumber" ? value : "1",
                fieldName == "PostalCode" ? value : "9000",
                fieldName == "City" ? value : "Aalborg",
                fieldName == "Country" ? value : "Denmark"));

        Assert.Equal(
            $"{fieldName} cannot exceed {maxLength} characters.",
            exception.Message);
    }
}