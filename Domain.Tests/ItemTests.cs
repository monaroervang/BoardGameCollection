using Domain.Models;

namespace Domain.Tests;

public class ItemTests
{
    [Fact]
    public void NewItem_ShouldNotBeArchived()
    {
        // Arrange
        var item = new Item(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(item.IsArchived);
    }

    [Fact]
    public void Archive_ShouldMarkItemAsArchived()
    {
        // Arrange
        var item = new Item(Guid.NewGuid(), Guid.NewGuid());

        // Act
        item.Archive();

        // Assert
        Assert.True(item.IsArchived);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrowException()
    {
        // Arrange
        var item = new Item(Guid.NewGuid(), Guid.NewGuid());
        item.Archive();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => item.Archive());

        // Assert
        Assert.Equal(
            "The item is already archived.",
            exception.Message);
    }

    [Fact]
    public void Reactivate_ShouldMarkItemAsActive()
    {
        // Arrange
        var item = new Item(Guid.NewGuid(), Guid.NewGuid());
        item.Archive();

        // Act
        item.Reactivate();

        // Assert
        Assert.False(item.IsArchived);
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ShouldThrowException()
    {
        // Arrange
        var item = new Item(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => item.Reactivate());

        // Assert
        Assert.Equal(
            "The item is already active.",
            exception.Message);
    }
}