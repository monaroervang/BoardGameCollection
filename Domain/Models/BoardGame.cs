using Domain.Exceptions.CustomExceptions;

namespace Domain.Models;

public class BoardGame // Represents a board game with its details
{
    public Guid Id { get; private set; } = Guid.CreateVersion7(); // Unique identifier for the board game, generated using Version 7 UUID
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Genre { get; private set; } = "";
    public string Publisher { get; private set; } = "";
    public string? Edition { get; private set; }
    public int ReleaseYear { get; private set; }
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }
    public int PlayingTimeMinutes { get; private set; }
    public int MinimumAge { get; private set; }
    public byte[] RowVersion { get; private set; } = []; // For concurrency control, used by Entity Framework Core. SQL Server uses this to detect if the record has been modified since it was last read.

    public BoardGame()
    {

    }

    public BoardGame(
    string title,
    string description,
    string genre,
    string publisher,
    string? edition,
    int releaseYear,
    int minPlayers,
    int maxPlayers,
    int playingTimeMinutes,
    int minimumAge)
    {
        Update(
            title, description, genre, publisher, edition,
            releaseYear, minPlayers, maxPlayers,
            playingTimeMinutes, minimumAge);
    }

    public void Update(
        string title,
        string description,
        string genre,
        string publisher,
        string? edition,
        int releaseYear,
        int minPlayers,
        int maxPlayers,
        int playingTimeMinutes,
        int minimumAge)
    {
        // Validate required text fields
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new RequiredFieldException(nameof(Title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new RequiredFieldException(nameof(Description));
        }

        if (string.IsNullOrWhiteSpace(genre))
        {
            throw new RequiredFieldException(nameof(Genre));
        }

        if (string.IsNullOrWhiteSpace(publisher))
        {
            throw new RequiredFieldException(nameof(Publisher));
        }
        if (title.Length > 200)
        {
            throw new MaxLengthExceededException(nameof(Title), 200);
        }

        if (genre.Length > 50)
        {
            throw new MaxLengthExceededException(nameof(Genre), 50);
        }

        if (publisher.Length > 100)
        {
            throw new MaxLengthExceededException(nameof(Publisher), 100);
        }

        if (edition?.Length > 50)
        {
            throw new MaxLengthExceededException(nameof(Edition), 50);
        }

        // Validate player count
        if (minPlayers <= 0 || maxPlayers < minPlayers)
        {
            throw new InvalidPlayerCountException();
        }

        // Validate release year
        if (releaseYear <= 0 || releaseYear > DateTime.Today.Year)
        {
            throw new InvalidReleaseYearException();
        }

        // Validate playing time
        if (playingTimeMinutes <= 0)
        {
            throw new InvalidPlayingTimeException();
        }

        // Validate minimum age
        if (minimumAge < 0)
        {
            throw new InvalidMinimumAgeException();
        }

        Title = title;
        Description = description;
        Genre = genre;
        Publisher = publisher;
        Edition = edition;
        ReleaseYear = releaseYear;
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        PlayingTimeMinutes = playingTimeMinutes;
        MinimumAge = minimumAge;
    }

}