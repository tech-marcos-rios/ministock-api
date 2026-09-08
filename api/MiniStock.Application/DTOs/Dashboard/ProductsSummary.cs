namespace MiniStock.Application.DTOs.Dashboard;

public record ProductsSummary(
    int TotalActiveProducts,
    decimal TotalInventoryValue,
    int LowStockProducts
);
