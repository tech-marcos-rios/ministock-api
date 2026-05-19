using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Categories;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Services;

public class CategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public CategoryService(ICategoryRepository categories, IProductRepository products, IUnitOfWork uow)
    {
        _categories = categories;
        _products = products;
        _uow = uow;
    }

    public async Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await _categories.ExistsByNameAsync(request.Name, ct))
            return Result.Failure<CategoryResponse>($"Ya existe una categoría con el nombre '{request.Name}'.");

        var category = Category.Create(request.Name, request.Description);
        await _categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(category, 0));
    }

    public async Task<Result<PagedResult<CategoryResponse>>> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var paged = await _categories.GetPagedAsync(page, pageSize, search, ct);
        var items = paged.Items.Select(c => MapToResponse(c, c.Products.Count)).ToList();

        return Result.Success(new PagedResult<CategoryResponse>(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    public async Task<Result<IReadOnlyList<CategoryResponse>>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var list = await _categories.GetAllActiveAsync(ct);
        return Result.Success<IReadOnlyList<CategoryResponse>>(
            list.Select(c => MapToResponse(c, c.Products.Count)).ToList());
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure<CategoryResponse>("Categoría no encontrada.");

        return Result.Success(MapToResponse(category, category.Products.Count));
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure<CategoryResponse>("Categoría no encontrada.");

        if (!string.Equals(category.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
            await _categories.ExistsByNameAsync(request.Name, ct))
            return Result.Failure<CategoryResponse>($"Ya existe una categoría con el nombre '{request.Name}'.");

        category.Update(request.Name, request.Description);
        _categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(category, category.Products.Count));
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure("Categoría no encontrada.");

        category.Deactivate();
        _categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static CategoryResponse MapToResponse(Category c, int productCount) =>
        new(c.Id, c.Name, c.Description, c.IsActive, productCount, c.CreatedAt, c.UpdatedAt);
}
