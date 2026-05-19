namespace MiniStock.Application.DTOs.Categories;

public record UpdateCategoryRequest(
    string Name,
    string? Description
);
