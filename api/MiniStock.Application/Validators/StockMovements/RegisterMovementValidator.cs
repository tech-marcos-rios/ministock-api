using FluentValidation;
using MiniStock.Application.DTOs.StockMovements;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Validators.StockMovements;

public class RegisterMovementValidator : AbstractValidator<RegisterMovementRequest>
{
    public RegisterMovementValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(300).When(x => x.Notes is not null);

        // Entry y Exit requieren cantidad positiva
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Type is MovementType.Entry or MovementType.Exit)
            .WithMessage("La cantidad debe ser mayor a 0 para entradas y salidas.");

        // Adjustment permite positivo o negativo, pero no cero
        RuleFor(x => x.Quantity)
            .NotEqual(0)
            .When(x => x.Type == MovementType.Adjustment)
            .WithMessage("La cantidad del ajuste no puede ser cero.");
    }
}
