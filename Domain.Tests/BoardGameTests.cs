using Domain.Models;
using Domain.Exceptions.CustomExceptions;

namespace Domain.Tests;

public class BoardGameTests
{
    [Fact]
    public void Constructor_ShouldCreateBoardGame_WhenValuesAreValid()
    {
        // Arrange & Act
        var boardGame = new BoardGame(
            1,
            "Catan",
            "A strategy board game",
            "Strategy",
            "Kosmos",
            "6th Edition",
            2025,
            3,
            4,
            90,
            10);

        // Assert
        Assert.Equal(1, boardGame.Id);
        Assert.Equal("Catan", boardGame.Title);
        Assert.Equal("A strategy board game", boardGame.Description);
        Assert.Equal("Strategy", boardGame.Genre);
        Assert.Equal("Kosmos", boardGame.Publisher);
        Assert.Equal("6th Edition", boardGame.Edition);
        Assert.Equal(2025, boardGame.ReleaseYear);
        Assert.Equal(3, boardGame.MinPlayers);
        Assert.Equal(4, boardGame.MaxPlayers);
        Assert.Equal(90, boardGame.PlayingTimeMinutes);
        Assert.Equal(10, boardGame.MinimumAge);
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidPlayerCountException_WhenMinPlayersIsZero()
    {
        // Act & Assert. Assert.Throws verifies that the expected exception is thrown
        Assert.Throws<InvalidPlayerCountException>(() =>
            new BoardGame(
                1,
                "Catan",
                "A strategy board game",
                "Strategy",
                "Kosmos",
                "6th Edition",
                2025,
                0,
                4,
                90,
                10));
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidPlayerCountException_WhenMaxPlayersIsLessThanMinPlayers()
    {
        // Act & Assert
        Assert.Throws<InvalidPlayerCountException>(() =>
            new BoardGame(
                1,
                "Catan",
                "A strategy board game",
                "Strategy",
                "Kosmos",
                "6th Edition",
                2025,
                4,
                3,
                90,
                10));
    }
    [Fact]
    public void Constructor_ShouldThrowInvalidReleaseYearException_WhenReleaseYearIsInFuture()
    {
        // Arrange
        var futureYear = DateTime.Today.Year + 1; //+ 1 to ensure it's in the future

        // Act & Assert
        Assert.Throws<InvalidReleaseYearException>(() =>
            new BoardGame(
                1,
                "Catan",
                "A strategy board game",
                "Strategy",
                "Kosmos",
                "6th Edition",
                futureYear,
                3,
                4,
                90,
                10));
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidPlayingTimeException_WhenPlayingTimeIsZero()
    {
        // Act & Assert
        Assert.Throws<InvalidPlayingTimeException>(() =>
            new BoardGame(
                1,
                "Catan",
                "A strategy board game",
                "Strategy",
                "Kosmos",
                "6th Edition",
                2025,
                3,
                4,
                0,
                10));
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidMinimumAgeException_WhenMinimumAgeIsNegative()
    {
        // Act & Assert
        Assert.Throws<InvalidMinimumAgeException>(() => //If minimum age is negative, it should throw an InvalidMinimumAgeException, but 0 is valid.
            new BoardGame(
                1,
                "Catan",
                "A strategy board game",
                "Strategy",
                "Kosmos",
                "6th Edition",
                2025,
                3,
                4,
                90,
                -1));
    }

    [Theory] // Using Theory and InlineData to test multiple required fields. This approach allows us to run the same test logic for different input values, making our tests more concise and maintainable.
    [InlineData(nameof(BoardGame.Title))] //InlineData attribute is used to provide the input data for the test method.
    [InlineData(nameof(BoardGame.Description))]
    [InlineData(nameof(BoardGame.Genre))]
    [InlineData(nameof(BoardGame.Publisher))]
    [InlineData(nameof(BoardGame.Edition))]
    public void Constructor_ShouldThrowRequiredFieldException_WhenRequiredFieldIsEmpty(
    string fieldName)
    {
        // Arrange
        var title = "Catan";
        var description = "A strategy board game";
        var genre = "Strategy";
        var publisher = "Kosmos";
        var edition = "6th Edition";

        // Set the selected field to an empty string
        switch (fieldName)
        {
            case nameof(BoardGame.Title):
                title = "";
                break;

            case nameof(BoardGame.Description):
                description = "";
                break;

            case nameof(BoardGame.Genre):
                genre = "";
                break;

            case nameof(BoardGame.Publisher):
                publisher = "";
                break;

            case nameof(BoardGame.Edition):
                edition = "";
                break;
        }

        // Act & Assert. Assert.Throws verifies that the expected exception is thrown
        var exception = Assert.Throws<RequiredFieldException>(() =>
            new BoardGame(
                1,
                title,
                description,
                genre,
                publisher,
                edition,
                2025,
                3,
                4,
                90,
                10));

        // Assert
        Assert.Equal($"{fieldName} is required.", exception.Message);
    }
}