using Microsoft.AspNetCore.Mvc;
using MiniStock.Application.Common;

namespace MiniStock.Api.Extensions;

/// <summary>
/// Traduce un <see cref="Result"/>/<see cref="Result{T}"/> de Application a un
/// <see cref="IActionResult"/>, centralizando el mapeo <see cref="ErrorType"/> → status code
/// + <see cref="ProblemDetails"/> en un solo lugar en vez de repetirlo en cada controller.
/// </summary>
public static class ResultExtensions
{
    /// <summary>200 OK con el valor si el resultado es exitoso; error mapeado si no.</summary>
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : result.ToProblemResult();

    /// <summary>204 No Content si el resultado es exitoso; error mapeado si no.</summary>
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : result.ToProblemResult();

    /// <summary>
    /// Si el resultado es exitoso, delega en <paramref name="onSuccess"/> la forma exacta de la
    /// respuesta (p. ej. <c>CreatedAtAction</c> con el id del recurso creado); si no, error mapeado.
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : result.ToProblemResult();

    private static IActionResult ToProblemResult(this Result result)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodeFor(result.Type),
            Title  = TitleFor(result.Type),
            Detail = result.Error
        };
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.NotFound     => StatusCodes.Status404NotFound,
        ErrorType.Conflict     => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        _                      => StatusCodes.Status400BadRequest
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.NotFound     => "Resource not found",
        ErrorType.Conflict     => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        _                      => "Validation error"
    };
}
