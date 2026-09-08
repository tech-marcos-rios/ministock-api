using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MiniStock.Api;

/// <summary>
/// Convierte cualquier excepción no controlada en un <see cref="ProblemDetails"/> 500 en vez
/// de dejar que el error por defecto de Kestrel (o el stack trace en Development) llegue al
/// cliente. Registrado con <c>AddExceptionHandler</c> / <c>app.UseExceptionHandler()</c> en
/// <c>Program.cs</c> — es el mecanismo nativo de .NET 8 para esto, sin necesidad de un
/// middleware custom.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title  = "An unexpected error occurred.",
            Detail = _environment.IsDevelopment() ? exception.ToString() : null
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }
}
