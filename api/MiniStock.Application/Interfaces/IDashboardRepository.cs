using MiniStock.Application.DTOs.Dashboard;

namespace MiniStock.Application.Interfaces;

public interface IDashboardRepository
{
    Task<int> GetTotalActiveProductsAsync(CancellationToken ct = default);
    Task<int> GetTotalActiveCategoriesAsync(CancellationToken ct = default);
    Task<decimal> GetTotalInventoryValueAsync(CancellationToken ct = default);
    Task<int> GetLowStockProductsCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StockByCategoryResponse>> GetStockByCategoryAsync(CancellationToken ct = default);
}
