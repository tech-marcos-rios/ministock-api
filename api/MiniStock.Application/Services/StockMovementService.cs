using MiniStock.Application.Common;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Services;

public class StockMovementService
{
    private readonly IStockMovementRepository _movements;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public StockMovementService(IStockMovementRepository movements, IProductRepository products, IUnitOfWork uow)
    {
        _movements = movements;
        _products = products;
        _uow = uow;
    }

    public async Task<Result<StockMovementResponse>> RegisterAsync(
        RegisterMovementRequest request, Guid userId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Result.Failure<StockMovementResponse>("Producto no encontrado.");

        if (!product.IsActive)
            return Result.Failure<StockMovementResponse>("No se pueden registrar movimientos en un producto inactivo.");

        // Calcular el delta real según el tipo
        var delta = request.Type switch
        {
            MovementType.Entry      =>  request.Quantity,
            MovementType.Exit       => -request.Quantity,
            MovementType.Adjustment =>  request.Quantity,
            _ => throw new InvalidOperationException("Tipo de movimiento desconocido.")
        };

        if (product.Stock + delta < 0)
            return Result.Failure<StockMovementResponse>(
                $"Stock insuficiente. Stock actual: {product.Stock}, movimiento solicitado: {delta}.");

        var movement = StockMovement.Create(product.Id, delta, request.Type, userId, request.Notes);
        product.ApplyStockMovement(delta);

        await _movements.AddAsync(movement, ct);
        _products.Update(product);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(movement, product));
    }

    public async Task<Result<PagedResult<StockMovementResponse>>> GetPagedAsync(
        int page, int pageSize, Guid? productId, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var paged = await _movements.GetPagedAsync(page, pageSize, productId, ct);
        var items = paged.Items.Select(m => MapToResponse(m, m.Product)).ToList();

        return Result.Success(new PagedResult<StockMovementResponse>(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    public async Task<Result<IReadOnlyList<StockMovementResponse>>> GetRecentAsync(
        int count, CancellationToken ct = default)
    {
        if (count is < 1 or > 50) count = 10;
        var list = await _movements.GetRecentAsync(count, ct);
        return Result.Success<IReadOnlyList<StockMovementResponse>>(
            list.Select(m => MapToResponse(m, m.Product)).ToList());
    }

    private static StockMovementResponse MapToResponse(StockMovement m, Product p) =>
        new(m.Id, p.Id, p.Name, p.SKU, m.Quantity, m.Type, m.Notes,
            m.CreatedById, m.CreatedBy?.Name ?? "Sistema", m.CreatedAt);
}
