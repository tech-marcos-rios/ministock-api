namespace MiniStock.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Product> _products = [];
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }

    public static Category Create(string name, string? description = null) =>
        new() { Name = name, Description = description };

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
