namespace Domain.Models;

public class Item // Represents an item (Boardgame) owned by a user and available for loan
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid BoardGameId { get; set; }

    public Guid OwnerId { get; set; }
}