namespace Shared.DTOs.Items.Responses;

public sealed record CollectionItemResponse
{
    public Guid ItemId { get; init; }
    public Guid BoardGameId { get; init; }

    public string Title { get; init; } = "";
    public string Genre { get; init; } = "";
    public string Publisher { get; init; } = "";

    public int MinPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public int PlayingTimeMinutes { get; init; }
    public int MinimumAge { get; init; }
    public bool IsArchived { get; init; }
}