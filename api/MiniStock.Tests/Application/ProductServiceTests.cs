using FluentAssertions;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.Interfaces;
using MiniStock.Application.Services;
using MiniStock.Application.Common;
using MiniStock.Domain.Entities;
using Moq;

namespace MiniStock.Tests.Application;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository>  _products    = new();
    private readonly Mock<ICategoryRepository> _categories  = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();
    private readonly ProductService            _sut;

    private static readonly Guid CategoryId = Guid.NewGuid();

    public ProductServiceTests()
    {
        _sut = new ProductService(_products.Object, _categories.Object, _uow.Object);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsSuccess()
    {
        var category = Category.Create("Electrónica");
        _products.Setup(r => r.ExistsBySkuAsync("LAP-001", default)).ReturnsAsync(false);
        _categories.Setup(r => r.GetByIdAsync(CategoryId, default)).ReturnsAsync(category);
        _products.Setup(r => r.AddAsync(It.IsAny<Product>(), default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var request = new CreateProductRequest("Laptop", "LAP-001", 999m, 10, 3, CategoryId, null);
        var result  = await _sut.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptop");
        result.Value.SKU.Should().Be("LAP-001");
        result.Value.CategoryName.Should().Be("Electrónica");
    }

    [Fact]
    public async Task Create_DuplicateSku_ReturnsFailure()
    {
        _products.Setup(r => r.ExistsBySkuAsync("LAP-001", default)).ReturnsAsync(true);

        var request = new CreateProductRequest("Laptop", "LAP-001", 999m, 10, 3, CategoryId, null);
        var result  = await _sut.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("LAP-001");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_CategoryNotFound_ReturnsFailure()
    {
        _products.Setup(r => r.ExistsBySkuAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _categories.Setup(r => r.GetByIdAsync(CategoryId, default)).ReturnsAsync((Category?)null);

        var request = new CreateProductRequest("Laptop", "SKU-X", 999m, 10, 3, CategoryId, null);
        var result  = await _sut.CreateAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Categoría");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsSuccess()
    {
        var product  = Product.Create("TV", "TV-001", 500m, 5, 1, CategoryId);
        var category = Category.Create("Electrónica");
        // Simular navegación Product.Category
        SetProductCategory(product, category);

        _products.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsFailure()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no encontrado");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidRequest_ReturnsUpdatedProduct()
    {
        var product  = Product.Create("Original", "SKU", 10m, 5, 1, CategoryId);
        var category = Category.Create("Categoría");
        SetProductCategory(product, category);

        _products.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);
        _categories.Setup(r => r.GetByIdAsync(CategoryId, default)).ReturnsAsync(category);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var request = new UpdateProductRequest("Editado", "Nueva desc", 99m, 3, CategoryId);
        var result  = await _sut.UpdateAsync(product.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Editado");
        result.Value.Price.Should().Be(99m);
    }

    [Fact]
    public async Task Update_ProductNotFound_ReturnsFailure()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var request = new UpdateProductRequest("X", null, 1m, 0, CategoryId);
        var result  = await _sut.UpdateAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deactivate_ExistingProduct_DeactivatesAndReturnsSuccess()
    {
        var product = Product.Create("TV", "TV-001", 500m, 5, 1, CategoryId);
        _products.Setup(r => r.GetByIdAsync(product.Id, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _sut.DeactivateAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_NonExistent_ReturnsFailure()
    {
        _products.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Product?)null);

        var result = await _sut.DeactivateAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetPaged ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_NormalizesPageBelowOne()
    {
        var paged = new PagedResult<Product>([], 0, 1, 20);
        _products.Setup(r => r.GetPagedAsync(1, 20, null, null, default)).ReturnsAsync(paged);

        var result = await _sut.GetPagedAsync(page: -5, pageSize: 20, null, null);

        result.IsSuccess.Should().BeTrue();
        _products.Verify(r => r.GetPagedAsync(1, 20, null, null, default), Times.Once);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static void SetProductCategory(Product product, Category category)
    {
        // Accedemos a la propiedad de navegación vía reflexión porque el setter es private
        typeof(Product)
            .GetProperty(nameof(Product.Category))!
            .SetValue(product, category);
    }
}
