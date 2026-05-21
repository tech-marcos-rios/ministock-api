using MiniStock.Application.Common;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Services;

/// <summary>
/// Caso de uso para registrar y consultar movimientos de stock.
/// </summary>
/// <remarks>
/// Este servicio concentra la regla de negocio más crítica del sistema:
/// <b>nunca permitir stock negativo</b>. El flujo de <see cref="RegisterAsync"/> es:
/// <list type="number">
///   <item>Verificar que el producto existe y está activo.</item>
///   <item>Calcular el delta real según el <see cref="MovementType"/>.</item>
///   <item>Rechazar si el resultado deja el stock en negativo.</item>
///   <item>Crear el movimiento y aplicar el delta al producto en una sola transacción.</item>
/// </list>
///
/// <b>Por qué en un solo SaveChanges:</b> el movimiento y la actualización del stock
/// deben ser atómicos. Si se guardaran por separado, un fallo entre ambas escrituras
/// dejaría la BD en un estado inconsistente (stock actualizado sin movimiento, o viceversa).
/// EF Core garantiza esto porque ambos cambios se envían en la misma transacción implícita.
/// </remarks>
public class StockMovementService
{
    private readonly IStockMovementRepository _movements;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public StockMovementService(IStockMovementRepository movements, IProductRepository products, IUnitOfWork uow)
    {
        _movements = movements;
        _products  = products;
        _uow       = uow;
    }

    /// <summary>
    /// Registra un movimiento de stock y actualiza el stock del producto de forma atómica.
    /// </summary>
    /// <param name="request">Datos del movimiento (producto, cantidad, tipo, notas).</param>
    /// <param name="userId">
    /// ID del usuario que genera el movimiento. Se extrae del JWT en el controller
    /// para que el cliente nunca pueda falsificarlo enviándolo en el body.
    /// </param>
    public async Task<Result<StockMovementResponse>> RegisterAsync(
        RegisterMovementRequest request, Guid userId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return Result.Failure<StockMovementResponse>("Producto no encontrado.", notFound: true);

        if (!product.IsActive)
            return Result.Failure<StockMovementResponse>("No se pueden registrar movimientos en un producto inactivo.");

        // El delta real depende del tipo: Exit invierte el signo para restar del stock.
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
        await _uow.SaveChangesAsync(ct); // Movimiento + stock se persisten juntos

        return Result.Success(MapToResponse(movement, product));
    }

    /// <summary>
    /// Retorna el historial de movimientos paginado, opcionalmente filtrado por producto.
    /// </summary>
    public async Task<Result<PagedResult<StockMovementResponse>>> GetPagedAsync(
        int page, int pageSize, Guid? productId, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var paged = await _movements.GetPagedAsync(page, pageSize, productId, ct);
        var items = paged.Items.Select(m => MapToResponse(m, m.Product)).ToList();

        return Result.Success(new PagedResult<StockMovementResponse>(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    /// <summary>Retorna los últimos <paramref name="count"/> movimientos para el widget del dashboard.</summary>
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
