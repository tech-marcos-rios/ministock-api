using MiniStock.Application.Common;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct = default);
    Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    void Update(Category category);
    void Delete(Category category);
}
