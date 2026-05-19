using FluentValidation;
using MiniStock.Application.DTOs.Products;

namespace MiniStock.Application.Validators.Products;

public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
