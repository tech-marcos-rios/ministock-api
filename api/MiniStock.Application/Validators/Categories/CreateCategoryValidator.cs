using FluentValidation;
using MiniStock.Application.DTOs.Categories;

namespace MiniStock.Application.Validators.Categories;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300).When(x => x.Description is not null);
    }
}
