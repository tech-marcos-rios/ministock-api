using MiniStock.Application.Common;
using MiniStock.Application.DTOs.StockMovements;

namespace MiniStock.Application.Interfaces;

public interface IStockMovementService
{
    Task<Result<StockMovementResponse>> RegisterAsync(
        RegisterMovementRequest request, Guid userId, CancellationToken ct = default);

    Task<Result<PagedResult<StockMovementResponse>>> GetPagedAsync(
        int page, int pageSize, Guid? productId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<StockMovementResponse>>> GetRecentAsync(int count, CancellationToken ct = default);
}
