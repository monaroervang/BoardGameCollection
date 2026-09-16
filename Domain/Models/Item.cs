namespace Domain.Models;

public class Item // Represents an item that can be loaned, such as a board game
{
    public int Id { get; set; }
    public int BoardGameId { get; set; }
    public int OwnerId { get; set; }
}