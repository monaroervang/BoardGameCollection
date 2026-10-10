namespace Domain.Models;

public class Item // Represents a physical board game owned by a user
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid BoardGameId { get; private set; }

    public Guid OwnerId { get; private set; }

    public bool IsArchived { get; private set; } = false;

    public Item() // Empty EF Core constructor
    {
    }

    public Item(Guid boardGameId, Guid ownerId) // Creates a physical board game owned by a user
    {
        BoardGameId = boardGameId;
        OwnerId = ownerId;
    }

    // Archives the item without deleting it or its loan history.
    public void Archive()
    {
        if (IsArchived)
        {
            throw new InvalidOperationException(
                "The item is already archived.");
        }

        IsArchived = true;
    }

    // Makes an archived item active again.
    public void Reactivate()
    {
        if (!IsArchived)
        {
            throw new InvalidOperationException(
                "The item is already active.");
        }

        IsArchived = false;
    }
}