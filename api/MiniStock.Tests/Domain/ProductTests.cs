using FluentAssertions;
using MiniStock.Domain.Entities;

namespace MiniStock.Tests.Domain;

public class ProductTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var product = Product.Create("Laptop", "LAP-001", 999.99m, 10, 3, CategoryId, "Descripción");

        product.Name.Should().Be("Laptop");
        product.SKU.Should().Be("LAP-001");
        product.Price.Should().Be(999.99m);
        product.Stock.Should().Be(10);
        product.MinStock.Should().Be(3);
        product.CategoryId.Should().Be(CategoryId);
        product.Description.Should().Be("Descripción");
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBeEmpty();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        product.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutDescription_DescriptionIsNull()
    {
        var product = Product.Create("Laptop", "LAP-001", 100m, 0, 0, CategoryId);
        product.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(3, 3, true)]
    [InlineData(1, 3, true)]
    [InlineData(0, 0, true)]
    public void IsLowStock_ReflectsStockVsMinStock(int stock, int minStock, bool expected)
    {
        var product = Product.Create("P", "S", 1m, stock, minStock, CategoryId);
        product.IsLowStock.Should().Be(expected);
    }

    [Fact]
    public void Update_ChangesEditableFields_AndSetsUpdatedAt()
    {
        var product = Product.Create("Original", "SKU", 10m, 5, 1, CategoryId);
        var newCategoryId = Guid.NewGuid();

        product.Update("Nuevo nombre", "Nueva desc", 99.99m, 5, newCategoryId);

        product.Name.Should().Be("Nuevo nombre");
        product.Description.Should().Be("Nueva desc");
        product.Price.Should().Be(99.99m);
        product.MinStock.Should().Be(5);
        product.CategoryId.Should().Be(newCategoryId);
        product.SKU.Should().Be("SKU"); // SKU no debe cambiar
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyStockMovement_PositiveDelta_IncreasesStock()
    {
        var product = Product.Create("P", "S", 1m, 10, 0, CategoryId);
        product.ApplyStockMovement(5);
        product.Stock.Should().Be(15);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyStockMovement_NegativeDelta_DecreasesStock()
    {
        var product = Product.Create("P", "S", 1m, 10, 0, CategoryId);
        product.ApplyStockMovement(-4);
        product.Stock.Should().Be(6);
    }

    [Fact]
    public void ApplyStockMovement_ToZero_IsAllowed()
    {
        var product = Product.Create("P", "S", 1m, 5, 0, CategoryId);
        product.ApplyStockMovement(-5);
        product.Stock.Should().Be(0);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse_AndSetsUpdatedAt()
    {
        var product = Product.Create("P", "S", 1m, 5, 0, CategoryId);
        product.Deactivate();
        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Movements_IsReadOnly_CannotBeModifiedExternally()
    {
        var product = Product.Create("P", "S", 1m, 5, 0, CategoryId);
        var action = () => ((System.Collections.Generic.ICollection<StockMovement>)product.Movements).Add(null!);
        action.Should().Throw<NotSupportedException>();
    }
}
