using MiniStock.Application.DTOs.Dashboard;

namespace MiniStock.Application.Interfaces;

public interface IDashboardRepository
{
    Task<ProductsSummary> GetProductsSummaryAsync(CancellationToken ct = default);
    Task<int> GetTotalActiveCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StockByCategoryResponse>> GetStockByCategoryAsync(CancellationToken ct = default);
}
