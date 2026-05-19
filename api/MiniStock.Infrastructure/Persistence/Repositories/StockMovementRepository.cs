using Microsoft.EntityFrameworkCore;
using MiniStock.Application.Common;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    public StockMovementRepository(AppDbContext context) => _context = context;

    public async Task<PagedResult<StockMovement>> GetPagedAsync(int page, int pageSize, Guid? productId, CancellationToken ct)
    {
        var query = _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.CreatedBy)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<StockMovement>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<StockMovement>> GetRecentAsync(int count, CancellationToken ct)
    {
        var list = await _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.CreatedBy)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
        return list;
    }

    public async Task AddAsync(StockMovement movement, CancellationToken ct) =>
        await _context.StockMovements.AddAsync(movement, ct);
}
