namespace MiniStock.Application.DTOs.Products;

public record CreateProductRequest(
    string Name,
    string SKU,
    decimal Price,
    int InitialStock,
    int MinStock,
    Guid CategoryId,
    string? Description
);
