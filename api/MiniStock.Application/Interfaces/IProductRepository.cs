using MiniStock.Application.Common;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Product>> GetPagedAsync(int page, int pageSize, string? search, Guid? categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);
    void Delete(Product product);
}
