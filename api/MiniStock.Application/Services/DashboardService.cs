using Mapster;
using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Dashboard;
using MiniStock.Application.DTOs.Products;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;

namespace MiniStock.Application.Services;

public class DashboardService
{
    private readonly IDashboardRepository _dashboard;
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;

    public DashboardService(
        IDashboardRepository dashboard,
        IProductRepository products,
        IStockMovementRepository movements)
    {
        _dashboard = dashboard;
        _products = products;
        _movements = movements;
    }

    public async Task<Result<DashboardSummaryResponse>> GetSummaryAsync(CancellationToken ct = default)
    {
        var totalProducts = await _dashboard.GetTotalActiveProductsAsync(ct);
        var totalCategories = await _dashboard.GetTotalActiveCategoriesAsync(ct);
        var lowStock = await _dashboard.GetLowStockProductsCountAsync(ct);
        var totalValue = await _dashboard.GetTotalInventoryValueAsync(ct);

        return Result.Success(new DashboardSummaryResponse(
            totalProducts,
            totalCategories,
            lowStock,
            totalValue
        ));
    }

    public async Task<Result<IReadOnlyList<StockByCategoryResponse>>> GetStockByCategoryAsync(CancellationToken ct = default)
    {
        var data = await _dashboard.GetStockByCategoryAsync(ct);
        return Result.Success(data);
    }

    public async Task<Result<IReadOnlyList<ProductResponse>>> GetLowStockProductsAsync(CancellationToken ct = default)
    {
        var products = await _products.GetLowStockAsync(ct);
        var dtos = products.Adapt<IReadOnlyList<ProductResponse>>();
        return Result.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<StockMovementResponse>>> GetRecentMovementsAsync(int count = 10, CancellationToken ct = default)
    {
        if (count is < 1 or > 50) count = 10;
        var list = await _movements.GetRecentAsync(count, ct);
        
        var dtos = list.Select(m => new StockMovementResponse(
            m.Id, m.Product.Id, m.Product.Name, m.Product.SKU, 
            m.Quantity, m.Type, m.Notes,
            m.CreatedById, m.CreatedBy?.Name ?? "Sistema", m.CreatedAt
        )).ToList();

        return Result.Success<IReadOnlyList<StockMovementResponse>>(dtos);
    }
}
