namespace MiniStock.Application.DTOs.Products;

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string SKU,
    decimal Price,
    int Stock,
    int MinStock,
    bool IsLowStock,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
