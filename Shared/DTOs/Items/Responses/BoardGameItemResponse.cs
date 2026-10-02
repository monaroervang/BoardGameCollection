namespace Shared.DTOs.Items.Responses;

public sealed record BoardGameItemResponse
{
    public Guid ItemId { get; init; }

    public Guid OwnerId { get; init; }
    public string OwnerUsername { get; init; } = "";

    public bool IsAvailable { get; init; }
}