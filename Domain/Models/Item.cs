namespace Domain.Models;

public class Item // Represents an item (Boardgame) owned by a user and available for loan
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid BoardGameId { get; private set; }

    public Guid OwnerId { get; private set; }

    public Item() //Empty EF-core constructor
    {
    }

    public Item(Guid boardGameId, Guid ownerId) // Constructor to create a new item with the specified board game ID and owner ID
    {
        BoardGameId = boardGameId;
        OwnerId = ownerId;
    }
}