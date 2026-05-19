using MiniStock.Domain.Entities;

namespace MiniStock.Application.DTOs.StockMovements;

public record RegisterMovementRequest(
    Guid ProductId,
    int Quantity,
    MovementType Type,
    string? Notes
);
