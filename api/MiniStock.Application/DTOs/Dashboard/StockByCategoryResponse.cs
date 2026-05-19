namespace MiniStock.Application.DTOs.Dashboard;

public record StockByCategoryResponse(
    Guid CategoryId,
    string CategoryName,
    int TotalStock
);
