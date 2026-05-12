namespace MiniStock.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string SKU { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public int MinStock { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = default!;

    private readonly List<StockMovement> _movements = [];
    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    public bool IsLowStock => Stock <= MinStock;

    private Product() { }

    public static Product Create(string name, string sku, decimal price, int initialStock, int minStock, Guid categoryId, string? description = null) =>
        new()
        {
            Name = name,
            SKU = sku,
            Price = price,
            Stock = initialStock,
            MinStock = minStock,
            CategoryId = categoryId,
            Description = description
        };

    public void Update(string name, string? description, decimal price, int minStock, Guid categoryId)
    {
        Name = name;
        Description = description;
        Price = price;
        MinStock = minStock;
        CategoryId = categoryId;
        SetUpdatedAt();
    }

    public void ApplyStockMovement(int quantity)
    {
        Stock += quantity;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
