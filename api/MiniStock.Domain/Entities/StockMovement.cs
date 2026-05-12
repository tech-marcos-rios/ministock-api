namespace MiniStock.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;
    public int Quantity { get; private set; }
    public MovementType Type { get; private set; }
    public string? Notes { get; private set; }
    public Guid CreatedById { get; private set; }
    public User CreatedBy { get; private set; } = default!;

    private StockMovement() { }

    public static StockMovement Create(Guid productId, int quantity, MovementType type, Guid createdById, string? notes = null) =>
        new()
        {
            ProductId = productId,
            Quantity = quantity,
            Type = type,
            CreatedById = createdById,
            Notes = notes
        };
}

public enum MovementType
{
    Entry = 1,
    Exit = 2,
    Adjustment = 3
}
