namespace MiniStock.Domain.Entities;

/// <summary>
/// Representa un artículo del inventario.
/// </summary>
/// <remarks>
/// <b>Encapsulación del dominio:</b> todos los setters son <c>private</c>.
/// El único camino para cambiar el estado de un producto es a través de sus
/// métodos de mutación explícitos (<see cref="Update"/>, <see cref="ApplyStockMovement"/>,
/// <see cref="Deactivate"/>). Esto garantiza que las reglas de negocio
/// (ej. validar stock antes de restar) no puedan ser salteadas desde la capa de aplicación.
///
/// <b>Factory method</b> (<see cref="Create"/>): el constructor privado fuerza a usar
/// el método estático, que actúa como punto único de creación con nombre semántico.
/// Permite agregar validaciones de invariantes en un solo lugar si la entidad crece.
///
/// <b>Soft delete</b>: en lugar de eliminar el registro, <see cref="Deactivate"/> marca
/// <see cref="IsActive"/> como <c>false</c>. Esto preserva el historial de movimientos
/// y la integridad referencial — los movimientos pasados siguen apuntando al producto.
///
/// <b>Colección encapsulada</b>: <c>_movements</c> es una lista privada expuesta solo
/// como <see cref="IReadOnlyCollection{T}"/> para prevenir modificaciones externas
/// que bypaseen la lógica de negocio.
/// </remarks>
public class Product : BaseEntity
{
    /// <summary>Nombre descriptivo del producto.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Descripción opcional con detalles técnicos o comerciales.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Código de identificación de stock (Stock Keeping Unit). Único en el sistema.
    /// Inmutable después de la creación para garantizar trazabilidad en sistemas externos.
    /// </summary>
    public string SKU { get; private set; } = default!;

    /// <summary>Precio unitario de venta. Almacenado con precisión decimal (18,2).</summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Cantidad actual en stock. Solo modificable a través de <see cref="ApplyStockMovement"/>,
    /// que garantiza que siempre se registre un <see cref="StockMovement"/> asociado.
    /// </summary>
    public int Stock { get; private set; }

    /// <summary>Umbral mínimo de stock. Cuando <see cref="Stock"/> ≤ este valor, <see cref="IsLowStock"/> es <c>true</c>.</summary>
    public int MinStock { get; private set; }

    /// <summary>Indica si el producto está activo. Los productos eliminados se marcan como inactivos (soft delete).</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>FK a la categoría a la que pertenece este producto.</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Navegación a la categoría. Cargada con <c>Include</c> por los repositorios que la necesiten.</summary>
    public Category Category { get; private set; } = default!;

    private readonly List<StockMovement> _movements = [];

    /// <summary>Historial de movimientos de stock de este producto. Solo lectura desde afuera del dominio.</summary>
    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    /// <summary>
    /// <c>true</c> cuando el stock actual está en o por debajo del mínimo configurado.
    /// Calculado en memoria — no persiste en la base de datos.
    /// </summary>
    public bool IsLowStock => Stock <= MinStock;

    private Product() { }

    /// <summary>
    /// Crea un nuevo producto con su stock inicial.
    /// </summary>
    /// <param name="name">Nombre del producto.</param>
    /// <param name="sku">Código SKU único.</param>
    /// <param name="price">Precio unitario.</param>
    /// <param name="initialStock">Cantidad inicial en depósito.</param>
    /// <param name="minStock">Stock mínimo antes de alertar bajo stock.</param>
    /// <param name="categoryId">ID de la categoría a la que pertenece.</param>
    /// <param name="description">Descripción opcional.</param>
    public static Product Create(
        string name, string sku, decimal price,
        int initialStock, int minStock, Guid categoryId,
        string? description = null) =>
        new()
        {
            Name        = name,
            SKU         = sku,
            Price       = price,
            Stock       = initialStock,
            MinStock    = minStock,
            CategoryId  = categoryId,
            Description = description
        };

    /// <summary>
    /// Actualiza los datos editables del producto. El SKU no es editable
    /// para preservar la trazabilidad en sistemas externos que lo referencien.
    /// </summary>
    public void Update(string name, string? description, decimal price, int minStock, Guid categoryId)
    {
        Name        = name;
        Description = description;
        Price       = price;
        MinStock    = minStock;
        CategoryId  = categoryId;
        SetUpdatedAt();
    }

    /// <summary>
    /// Aplica un delta de stock. El signo del <paramref name="quantity"/> determina
    /// si es suma (positivo) o resta (negativo).
    /// La validación de suficiencia de stock debe hacerse en la capa de aplicación
    /// (<see cref="MiniStock.Application.Services.StockMovementService"/>) antes de llamar a este método.
    /// </summary>
    /// <param name="quantity">Delta a aplicar. Positivo = entrada, negativo = salida.</param>
    public void ApplyStockMovement(int quantity)
    {
        Stock += quantity;
        SetUpdatedAt();
    }

    /// <summary>
    /// Marca el producto como inactivo (soft delete).
    /// No elimina el registro para preservar el historial de movimientos.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
