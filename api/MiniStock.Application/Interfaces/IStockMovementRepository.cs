using MiniStock.Application.Common;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Interfaces;

public interface IStockMovementRepository
{
    Task<PagedResult<StockMovement>> GetPagedAsync(int page, int pageSize, Guid? productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetRecentAsync(int count, CancellationToken ct = default);
    Task AddAsync(StockMovement movement, CancellationToken ct = default);
}
