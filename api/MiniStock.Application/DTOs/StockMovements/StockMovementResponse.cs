using MiniStock.Domain.Entities;

namespace MiniStock.Application.DTOs.StockMovements;

public record StockMovementResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSKU,
    int Quantity,
    MovementType Type,
    string? Notes,
    Guid CreatedById,
    string CreatedByName,
    DateTime CreatedAt
);
