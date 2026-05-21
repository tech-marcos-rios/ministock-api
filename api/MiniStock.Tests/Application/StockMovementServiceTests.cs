using FluentAssertions;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;
using MiniStock.Application.Services;
using MiniStock.Domain.Entities;
using Moq;

namespace MiniStock.Tests.Application;

public class StockMovementServiceTests
{
    private readonly Mock<IStockMovementRepository> _movements = new();
    private readonly Mock<IProductRepository>       _products  = new();
    private readonly Mock<IUnitOfWork>              _uow       = new();
    private readonly StockMovementService           _sut;

    private static readonly Guid UserId     = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    public StockMovementServiceTests()
    {
        _sut = new StockMovementService(_movements.Object, _products.Object, _uow.Object);
    }

    // ── Entry ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Entry_IncreasesStockAndReturnsSuccess()
    {
        var product = Product.Create("TV", "TV-001", 500m, 10, 2, CategoryId);
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, 5, MovementType.Entry, null);
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(15);
        result.Value!.Quantity.Should().Be(5);
        result.Value.Type.Should().Be(MovementType.Entry);
    }

    // ── Exit ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Exit_DecreasesStockAndReturnsSuccess()
    {
        var product = Product.Create("TV", "TV-001", 500m, 10, 2, CategoryId);
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, 4, MovementType.Exit, "Venta");
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(6);
        result.Value!.Quantity.Should().Be(-4); // delta negativo para salida
    }

    [Fact]
    public async Task Register_ExitExactStock_LeavesZeroStock()
    {
        var product = Product.Create("TV", "TV-001", 500m, 5, 0, CategoryId);
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, 5, MovementType.Exit, null);
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(0);
    }

    [Fact]
    public async Task Register_ExitMoreThanStock_ReturnsFailure()
    {
        var product = Product.Create("TV", "TV-001", 500m, 3, 0, CategoryId);
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, 10, MovementType.Exit, null);
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Stock insuficiente");
        result.Error.Should().Contain("3"); // stock actual en el mensaje
        product.Stock.Should().Be(3);       // no debe haberse modificado
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Adjustment ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_PositiveAdjustment_IncreasesStock()
    {
        var product = Product.Create("TV", "TV-001", 500m, 10, 2, CategoryId);
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, 3, MovementType.Adjustment, "Conteo físico");
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(13);
    }

    [Fact]
    public async Task Register_NegativeAdjustment_DecreasesStock()
    {
        var product = Product.Create("TV", "TV-001", 500m, 10, 2, CategoryId);
        SetupProductFound(product);

        // Ajuste negativo: quantity = -2
        var request = new RegisterMovementRequest(product.Id, -2, MovementType.Adjustment, "Merma");
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(8);
    }

    [Fact]
    public async Task Register_AdjustmentResultingInNegativeStock_ReturnsFailure()
    {
        var product = Product.Create("TV", "TV-001", 500m, 2, 0, CategoryId);
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, -5, MovementType.Adjustment, null);
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Stock insuficiente");
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ProductNotFound_ReturnsFailure()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var request = new RegisterMovementRequest(Guid.NewGuid(), 1, MovementType.Entry, null);
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task Register_InactiveProduct_ReturnsFailure()
    {
        var product = Product.Create("TV", "TV-001", 500m, 5, 0, CategoryId);
        product.Deactivate();
        SetupProductFound(product);

        var request = new RegisterMovementRequest(product.Id, 1, MovementType.Entry, null);
        var result  = await _sut.RegisterAsync(request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inactivo");
    }

    // ── GetRecent ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecent_ClampsCountAbove50()
    {
        _movements.Setup(r => r.GetRecentAsync(10, default)).ReturnsAsync([]);

        var result = await _sut.GetRecentAsync(count: 200);

        result.IsSuccess.Should().BeTrue();
        _movements.Verify(r => r.GetRecentAsync(10, default), Times.Once);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetupProductFound(Product product)
    {
        _products.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);
        _movements.Setup(r => r.AddAsync(It.IsAny<StockMovement>(), default)).Returns(Task.CompletedTask);
        _products.Setup(r => r.Update(It.IsAny<Product>()));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
    }
}
