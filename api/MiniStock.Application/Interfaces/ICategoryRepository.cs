using MiniStock.Application.Common;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct = default);
    Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Cantidad de productos activos de una categoría, sin cargar las entidades.</summary>
    Task<int> GetActiveProductCountAsync(Guid categoryId, CancellationToken ct = default);

    /// <summary>
    /// Cantidad de productos activos por categoría para un lote de ids, en un solo query
    /// (evita N+1 al armar una página de resultados). Las categorías sin productos activos
    /// no aparecen en el diccionario devuelto.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetActiveProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken ct = default);

    Task AddAsync(Category category, CancellationToken ct = default);
    void Update(Category category);
    void Delete(Category category);
}
