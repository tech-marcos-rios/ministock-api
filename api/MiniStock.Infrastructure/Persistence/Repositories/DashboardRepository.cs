using Microsoft.EntityFrameworkCore;
using MiniStock.Application.DTOs.Dashboard;
using MiniStock.Application.Interfaces;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context) => _context = context;

    public Task<int> GetTotalActiveProductsAsync(CancellationToken ct) =>
        _context.Products.CountAsync(p => p.IsActive, ct);

    public Task<int> GetTotalActiveCategoriesAsync(CancellationToken ct) =>
        _context.Categories.CountAsync(c => c.IsActive, ct);

    public Task<decimal> GetTotalInventoryValueAsync(CancellationToken ct) =>
        _context.Products
            .Where(p => p.IsActive)
            .SumAsync(p => p.Price * p.Stock, ct);

    public Task<int> GetLowStockProductsCountAsync(CancellationToken ct) =>
        _context.Products
            .CountAsync(p => p.IsActive && p.Stock <= p.MinStock, ct);

    public async Task<IReadOnlyList<StockByCategoryResponse>> GetStockByCategoryAsync(CancellationToken ct)
    {
        var result = await _context.Categories
            .Where(c => c.IsActive)
            .Select(c => new StockByCategoryResponse(
                c.Id,
                c.Name,
                c.Products.Where(p => p.IsActive).Sum(p => p.Stock)
            ))
            .ToListAsync(ct);

        return result;
    }
}
