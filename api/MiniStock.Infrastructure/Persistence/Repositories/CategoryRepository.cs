using Microsoft.EntityFrameworkCore;
using MiniStock.Application.Common;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) => _context = context;

    // AsNoTracking: los callers que mutan (Update/Deactivate) llaman explícitamente
    // a Update(category) más abajo, que adjunta y marca modificado sin depender del
    // change tracker — no hace falta que esta lectura trackee la entidad.
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct)
    {
        var list = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        return list;
    }

    public async Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _context.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Category>(items, total, page, pageSize);
    }

    // ILike sin wildcards = comparación exacta case-insensitive. Antes esto era `==`
    // (case-sensitive), inconsistente con el rename check case-insensitive de
    // CategoryService.UpdateAsync — "Oficina" y "OFICINA" no se detectaban como duplicados.
    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct) =>
        _context.Categories.AnyAsync(c => EF.Functions.ILike(c.Name, name), ct);

    public Task<int> GetActiveProductCountAsync(Guid categoryId, CancellationToken ct) =>
        _context.Products.CountAsync(p => p.CategoryId == categoryId && p.IsActive, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetActiveProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken ct)
    {
        var ids = categoryIds.ToList();
        return await _context.Products
            .Where(p => p.IsActive && ids.Contains(p.CategoryId))
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct) =>
        await _context.Categories.AddAsync(category, ct);

    public void Update(Category category) =>
        _context.Categories.Update(category);

    public void Delete(Category category) =>
        _context.Categories.Remove(category);
}
