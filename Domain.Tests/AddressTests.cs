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
}