using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Products;

namespace MiniStock.Application.Interfaces;

public interface IProductService
{
    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    Task<Result<PagedResult<ProductResponse>>> GetPagedAsync(
        int page, int pageSize, string? search, Guid? categoryId, CancellationToken ct = default);

    Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);

    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
}
