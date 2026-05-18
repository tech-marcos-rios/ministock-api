using Microsoft.EntityFrameworkCore;
using MiniStock.Application.Common;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) => _context = context;

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct)
    {
        var list = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        return list;
    }

    public async Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Category>(items, total, page, pageSize);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct) =>
        _context.Categories.AnyAsync(c => c.Name == name, ct);

    public async Task AddAsync(Category category, CancellationToken ct) =>
        await _context.Categories.AddAsync(category, ct);

    public void Update(Category category) =>
        _context.Categories.Update(category);

    public void Delete(Category category) =>
        _context.Categories.Remove(category);
}
