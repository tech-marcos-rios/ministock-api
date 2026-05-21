namespace MiniStock.Domain.Entities;

/// <summary>
/// Registra un cambio de stock de un producto en un momento dado.
/// </summary>
/// <remarks>
/// Los movimientos son <b>inmutables después de creados</b>: no tienen métodos de mutación.
/// Esto es intencional — el historial no se edita; si hay un error se crea un movimiento
/// de ajuste (<see cref="MovementType.Adjustment"/>) con el delta corrector.
///
/// El campo <see cref="Quantity"/> almacena el delta aplicado al stock, no la cantidad
/// solicitada por el usuario. Para salidas, se guarda como negativo. Esto simplifica
/// la reconstrucción del stock: <c>SUM(Quantity)</c> sobre los movimientos de un
/// producto da el stock actual.
///
/// <see cref="CreatedById"/> garantiza trazabilidad: siempre se sabe qué usuario
/// generó cada movimiento. Se extrae del JWT en el controller, no del body del request.
/// </remarks>
public class StockMovement : BaseEntity
{
    /// <summary>FK al producto afectado.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Navegación al producto. Cargada explícitamente por los repositorios.</summary>
    public Product Product { get; private set; } = default!;

    /// <summary>
    /// Delta aplicado al stock. Positivo para entradas y ajustes hacia arriba,
    /// negativo para salidas y ajustes hacia abajo.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>Tipo de movimiento que determina la semántica del <see cref="Quantity"/>.</summary>
    public MovementType Type { get; private set; }

    /// <summary>Nota opcional que documenta el motivo del movimiento (proveedor, cliente, etc.).</summary>
    public string? Notes { get; private set; }

    /// <summary>FK al usuario que registró el movimiento. Extraído del JWT, no del body.</summary>
    public Guid CreatedById { get; private set; }

    /// <summary>Navegación al usuario creador.</summary>
    public User CreatedBy { get; private set; } = default!;

    private StockMovement() { }

    /// <summary>Crea un nuevo registro de movimiento. Inmutable una vez creado.</summary>
    public static StockMovement Create(
        Guid productId, int quantity, MovementType type,
        Guid createdById, string? notes = null) =>
        new()
        {
            ProductId   = productId,
            Quantity    = quantity,
            Type        = type,
            CreatedById = createdById,
            Notes       = notes
        };
}

/// <summary>
/// Tipo de movimiento de stock.
/// Los valores enteros están fijados para compatibilidad con la base de datos
/// y para serialización predecible en la API.
/// </summary>
public enum MovementType
{
    /// <summary>Ingreso de mercancía al inventario (compra, devolución de cliente).</summary>
    Entry = 1,

    /// <summary>Salida de mercancía del inventario (venta, rotura, merma).</summary>
    Exit = 2,

    /// <summary>Corrección del stock por conteo físico u otro motivo. La cantidad puede ser positiva o negativa.</summary>
    Adjustment = 3
}
