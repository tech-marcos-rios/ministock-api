using FluentAssertions;
using MiniStock.Domain.Entities;

namespace MiniStock.Tests.Domain;

public class StockMovementTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var productId = Guid.NewGuid();
        var userId    = Guid.NewGuid();

        var movement = StockMovement.Create(productId, 10, MovementType.Entry, userId, "Notas");

        movement.ProductId.Should().Be(productId);
        movement.Quantity.Should().Be(10);
        movement.Type.Should().Be(MovementType.Entry);
        movement.CreatedById.Should().Be(userId);
        movement.Notes.Should().Be("Notas");
        movement.Id.Should().NotBeEmpty();
        movement.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithoutNotes_NotesIsNull()
    {
        var movement = StockMovement.Create(Guid.NewGuid(), 5, MovementType.Exit, Guid.NewGuid());
        movement.Notes.Should().BeNull();
    }

    [Theory]
    [InlineData(MovementType.Entry)]
    [InlineData(MovementType.Exit)]
    [InlineData(MovementType.Adjustment)]
    public void Create_AllMovementTypesAreValid(MovementType type)
    {
        var movement = StockMovement.Create(Guid.NewGuid(), 1, type, Guid.NewGuid());
        movement.Type.Should().Be(type);
    }
}
