namespace Shared.DTOs.ValueObjects;

public sealed record AddressDto
{
    public string Street { get; init; } = "";
    public string HouseNumber { get; init; } = "";
    public string PostalCode { get; init; } = "";
    public string City { get; init; } = "";
    public string Country { get; init; } = "";
}