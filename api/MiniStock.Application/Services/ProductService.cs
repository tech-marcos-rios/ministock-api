using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Services;

public class ProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;

    public ProductService(IProductRepository products, ICategoryRepository categories, IUnitOfWork uow)
    {
        _products = products;
        _categories = categories;
        _uow = uow;
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (await _products.ExistsBySkuAsync(request.SKU, ct))
            return Result.Failure<ProductResponse>($"Ya existe un producto con el SKU '{request.SKU}'.");

        var category = await _categories.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return Result.Failure<ProductResponse>("Categoría no encontrada.");

        var product = Product.Create(request.Name, request.SKU, request.Price, request.InitialStock, request.MinStock, request.CategoryId, request.Description);
        await _products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(product, category.Name));
    }

    public async Task<Result<PagedResult<ProductResponse>>> GetPagedAsync(
        int page, int pageSize, string? search, Guid? categoryId, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var paged = await _products.GetPagedAsync(page, pageSize, search, categoryId, ct);
        var items = paged.Items.Select(p => MapToResponse(p, p.Category.Name)).ToList();

        return Result.Success(new PagedResult<ProductResponse>(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    public async Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
            return Result.Failure<ProductResponse>("Producto no encontrado.", notFound: true);

        return Result.Success(MapToResponse(product, product.Category.Name));
    }

    public async Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
            return Result.Failure<ProductResponse>("Producto no encontrado.", notFound: true);

        var category = await _categories.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return Result.Failure<ProductResponse>("Categoría no encontrada.", notFound: true);

        product.Update(request.Name, request.Description, request.Price, request.MinStock, request.CategoryId);
        _products.Update(product);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(product, category.Name));
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
            return Result.Failure("Producto no encontrado.", notFound: true);

        product.Deactivate();
        _products.Update(product);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static ProductResponse MapToResponse(Product p, string categoryName) =>
        new(p.Id, p.Name, p.Description, p.SKU, p.Price, p.Stock, p.MinStock,
            p.IsLowStock, p.IsActive, p.CategoryId, categoryName, p.CreatedAt, p.UpdatedAt);
}
