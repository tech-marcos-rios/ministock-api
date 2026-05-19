namespace MiniStock.Application.DTOs.Categories;

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
