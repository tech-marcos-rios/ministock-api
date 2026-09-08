using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Dashboard;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.DTOs.StockMovements;

namespace MiniStock.Application.Interfaces;

public interface IDashboardService
{
    Task<Result<DashboardSummaryResponse>> GetSummaryAsync(CancellationToken ct = default);

    Task<Result<IReadOnlyList<StockByCategoryResponse>>> GetStockByCategoryAsync(CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProductResponse>>> GetLowStockProductsAsync(CancellationToken ct = default);

    Task<Result<IReadOnlyList<StockMovementResponse>>> GetRecentMovementsAsync(int count = 10, CancellationToken ct = default);
}
