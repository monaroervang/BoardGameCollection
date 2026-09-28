namespace Shared.DTOs.Items.Requests;

public sealed record CreateItemRequest
{
    public Guid BoardGameId { get; init; }
    public Guid OwnerId { get; init; }
}