namespace MiniStock.Application.DTOs.Products;

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int MinStock,
    Guid CategoryId
);
