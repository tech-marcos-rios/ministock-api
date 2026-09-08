using Microsoft.EntityFrameworkCore;
using MiniStock.Application.DTOs.Dashboard;
using MiniStock.Application.Interfaces;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context) => _context = context;

    // Antes eran 3 round-trips separados (count total, sum de valor, count de low-stock),
    // los tres sobre el mismo Where(IsActive). GroupBy(_ => true) fuerza a EF Core a traer
    // los tres agregados en un solo SELECT.
    public async Task<ProductsSummary> GetProductsSummaryAsync(CancellationToken ct)
    {
        var summary = await _context.Products
            .Where(p => p.IsActive)
            .GroupBy(_ => true)
            .Select(g => new ProductsSummary(
                g.Count(),
                g.Sum(p => p.Price * p.Stock),
                g.Count(p => p.Stock <= p.MinStock)
            ))
            .SingleOrDefaultAsync(ct);

        return summary ?? new ProductsSummary(0, 0m, 0);
    }

    public Task<int> GetTotalActiveCategoriesAsync(CancellationToken ct) =>
        _context.Categories.CountAsync(c => c.IsActive, ct);

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
