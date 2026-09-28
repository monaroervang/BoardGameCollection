namespace Shared.DTOs.Items.Responses;

public sealed record ItemResponse
{
    public Guid Id { get; init; }
    public Guid BoardGameId { get; init; }
    public Guid OwnerId { get; init; }
}