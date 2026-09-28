namespace Shared.DTOs.ValueObjects;

public sealed record NameDto
{
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
}