using Domain.Exceptions.CustomExceptions;

namespace Domain.ValueObjects;

public class Address
{
    public string Street { get; }
    public string HouseNumber { get; }
    public string PostalCode { get; }
    public string City { get; }
    public string Country { get; }

    public Address(
        string street,
        string houseNumber,
        string postalCode,
        string city,
        string country)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new RequiredFieldException(nameof(Street));
        }

        if (string.IsNullOrWhiteSpace(houseNumber))
        {
            throw new RequiredFieldException(nameof(HouseNumber));
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new RequiredFieldException(nameof(PostalCode));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new RequiredFieldException(nameof(City));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new RequiredFieldException(nameof(Country));
        }
        if (street.Length > 150)
            throw new MaxLengthExceededException(nameof(Street), 150);

        if (houseNumber.Length > 20)
            throw new MaxLengthExceededException(nameof(HouseNumber), 20);

        if (postalCode.Length > 20)
            throw new MaxLengthExceededException(nameof(PostalCode), 20);

        if (city.Length > 100)
            throw new MaxLengthExceededException(nameof(City), 100);

        if (country.Length > 100)
            throw new MaxLengthExceededException(nameof(Country), 100);

        Street = street;
        HouseNumber = houseNumber;
        PostalCode = postalCode;
        City = city;
        Country = country;
    }
}