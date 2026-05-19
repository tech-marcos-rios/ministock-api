using Microsoft.EntityFrameworkCore;
using MiniStock.Application.Common;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PagedResult<Product>> GetPagedAsync(int page, int pageSize, string? search, Guid? categoryId, CancellationToken ct)
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                EF.Functions.ILike(p.SKU,  $"%{search}%"));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct)
    {
        var list = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock <= p.MinStock)
            .OrderBy(p => p.Stock)
            .ToListAsync(ct);
        return list;
    }

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct) =>
        _context.Products.AnyAsync(p => p.SKU == sku, ct);

    public async Task AddAsync(Product product, CancellationToken ct) =>
        await _context.Products.AddAsync(product, ct);

    public void Update(Product product) =>
        _context.Products.Update(product);

    public void Delete(Product product) =>
        _context.Products.Remove(product);
}
