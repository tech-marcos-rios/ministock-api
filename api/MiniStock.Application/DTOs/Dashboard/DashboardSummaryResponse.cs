namespace MiniStock.Application.DTOs.Dashboard;

public record DashboardSummaryResponse(
    int TotalProducts,
    int TotalCategories,
    int LowStockProducts,
    decimal TotalInventoryValue
);
