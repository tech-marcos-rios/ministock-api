using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Categories;

namespace MiniStock.Application.Interfaces;

public interface ICategoryService
{
    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    Task<Result<PagedResult<CategoryResponse>>> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CategoryResponse>>> GetAllActiveAsync(CancellationToken ct = default);

    Task<Result<CategoryResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);

    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
}
