using FluentAssertions;
using MiniStock.Application.DTOs.Dashboard;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;
using MiniStock.Application.Services;
using MiniStock.Domain.Entities;
using Moq;

namespace MiniStock.Tests.Application;

public class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository>     _dashboard = new();
    private readonly Mock<IProductRepository>       _products  = new();
    private readonly Mock<IStockMovementRepository> _movements = new();
    private readonly DashboardService               _sut;

    private static readonly Guid CategoryId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_dashboard.Object, _products.Object, _movements.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsAllFourKpis()
    {
        _dashboard.Setup(r => r.GetTotalActiveProductsAsync(default)).ReturnsAsync(9);
        _dashboard.Setup(r => r.GetTotalActiveCategoriesAsync(default)).ReturnsAsync(4);
        _dashboard.Setup(r => r.GetLowStockProductsCountAsync(default)).ReturnsAsync(2);
        _dashboard.Setup(r => r.GetTotalInventoryValueAsync(default)).ReturnsAsync(15000m);

        var result = await _sut.GetSummaryAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProducts.Should().Be(9);
        result.Value.TotalCategories.Should().Be(4);
        result.Value.LowStockProducts.Should().Be(2);
        result.Value.TotalInventoryValue.Should().Be(15000m);
    }

    [Fact]
    public async Task GetStockByCategory_ReturnsMappedList()
    {
        var data = new List<StockByCategoryResponse>
        {
            new(CategoryId, "Electrónica", 25),
            new(Guid.NewGuid(), "Herramientas", 75)
        }.AsReadOnly();
        _dashboard.Setup(r => r.GetStockByCategoryAsync(default)).ReturnsAsync(data);

        var result = await _sut.GetStockByCategoryAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value![0].CategoryName.Should().Be("Electrónica");
    }

    [Fact]
    public async Task GetLowStockProducts_ReturnsMappedProducts()
    {
        var product  = Product.Create("TV", "TV-001", 500m, 1, 5, CategoryId);
        var category = Category.Create("Electrónica");
        SetProductCategory(product, category);

        _products.Setup(r => r.GetLowStockAsync(default)).ReturnsAsync([product]);

        var result = await _sut.GetLowStockProductsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value![0].IsLowStock.Should().BeTrue();
    }

    [Fact]
    public async Task GetRecentMovements_ClampsCountOutOfRange()
    {
        _movements.Setup(r => r.GetRecentAsync(10, default)).ReturnsAsync([]);

        await _sut.GetRecentMovementsAsync(count: 999);

        _movements.Verify(r => r.GetRecentAsync(10, default), Times.Once);
    }

    [Fact]
    public async Task GetRecentMovements_EmptyList_ReturnsEmptySuccess()
    {
        _movements.Setup(r => r.GetRecentAsync(5, default)).ReturnsAsync([]);

        var result = await _sut.GetRecentMovementsAsync(count: 5);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    private static void SetProductCategory(Product product, Category category)
    {
        typeof(Product).GetProperty(nameof(Product.Category))!.SetValue(product, category);
    }
}
