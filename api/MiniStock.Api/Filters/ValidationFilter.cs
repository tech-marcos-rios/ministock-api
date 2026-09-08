using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiniStock.Api.Filters;

/// <summary>
/// Ejecuta el <see cref="IValidator{T}"/> de FluentValidation registrado para cada
/// argumento de la acción (si existe uno) antes de que el controller la reciba.
/// </summary>
/// <remarks>
/// FluentValidation no valida nada por sí solo: <c>AddValidatorsFromAssembly</c> solo
/// registra los validators en el contenedor de DI, pero alguien tiene que resolverlos y
/// llamarlos. Este filtro es ese "alguien", aplicado globalmente en <c>Program.cs</c>
/// para no repetir la llamada a mano en cada controller.
/// </remarks>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var problemDetails = new ValidationProblemDetails(result.ToDictionary())
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation error"
                };
                context.Result = new BadRequestObjectResult(problemDetails);
                return;
            }
        }

        await next();
    }
}
