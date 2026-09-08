using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Categories;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;

    public CategoryService(ICategoryRepository categories, IUnitOfWork uow)
    {
        _categories = categories;
        _uow = uow;
    }

    public async Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await _categories.ExistsByNameAsync(request.Name, ct))
            return Result.Failure<CategoryResponse>($"Ya existe una categoría con el nombre '{request.Name}'.", ErrorType.Conflict);

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
        var counts = await _categories.GetActiveProductCountsAsync(paged.Items.Select(c => c.Id), ct);
        var items = paged.Items.Select(c => MapToResponse(c, counts.GetValueOrDefault(c.Id))).ToList();

        return Result.Success(new PagedResult<CategoryResponse>(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    public async Task<Result<IReadOnlyList<CategoryResponse>>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var list = await _categories.GetAllActiveAsync(ct);
        var counts = await _categories.GetActiveProductCountsAsync(list.Select(c => c.Id), ct);
        return Result.Success<IReadOnlyList<CategoryResponse>>(
            list.Select(c => MapToResponse(c, counts.GetValueOrDefault(c.Id))).ToList());
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure<CategoryResponse>("Categoría no encontrada.", ErrorType.NotFound);

        var count = await _categories.GetActiveProductCountAsync(id, ct);
        return Result.Success(MapToResponse(category, count));
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure<CategoryResponse>("Categoría no encontrada.", ErrorType.NotFound);

        if (!string.Equals(category.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
            await _categories.ExistsByNameAsync(request.Name, ct))
            return Result.Failure<CategoryResponse>($"Ya existe una categoría con el nombre '{request.Name}'.", ErrorType.Conflict);

        category.Update(request.Name, request.Description);
        _categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        var count = await _categories.GetActiveProductCountAsync(id, ct);
        return Result.Success(MapToResponse(category, count));
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure("Categoría no encontrada.", ErrorType.NotFound);

        // Antes esto solo lo bloqueaba el frontend (botón deshabilitado); una llamada
        // directa a la API podía dejar productos activos apuntando a una categoría inactiva.
        var activeProducts = await _categories.GetActiveProductCountAsync(id, ct);
        if (activeProducts > 0)
            return Result.Failure(
                "No se puede dar de baja una categoría con productos activos.", ErrorType.Conflict);

        category.Deactivate();
        _categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static CategoryResponse MapToResponse(Category c, int productCount) =>
        new(c.Id, c.Name, c.Description, c.IsActive, productCount, c.CreatedAt, c.UpdatedAt);
}
