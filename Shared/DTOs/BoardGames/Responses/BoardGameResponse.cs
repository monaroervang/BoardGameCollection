namespace Shared.DTOs.BoardGames.Responses;

public sealed record BoardGameResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Genre { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string? Edition { get; init; }
    public int ReleaseYear { get; init; }
    public int MinPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public int PlayingTimeMinutes { get; init; }
    public int MinimumAge { get; init; }
}