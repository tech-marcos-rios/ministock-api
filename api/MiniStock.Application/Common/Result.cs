namespace MiniStock.Application.Common;

/// <summary>
/// Representa el resultado de una operación que puede fallar por razones de negocio.
/// </summary>
/// <remarks>
/// <b>Por qué Result en lugar de excepciones:</b>
/// Las excepciones en .NET tienen overhead de stack trace y están semánticamente
/// reservadas para situaciones <i>inesperadas</i> (bugs, fallos de infraestructura).
/// Los errores de negocio —"el SKU ya existe", "stock insuficiente"— son flujos
/// esperados que deben manejarse explícitamente.
///
/// Con este patrón:
/// - El caller <i>está forzado</i> a verificar <see cref="IsSuccess"/> antes de usar el valor.
/// - Los controllers mapean <see cref="IsFailure"/> a respuestas HTTP apropiadas (400, 404, 409)
///   sin try/catch.
/// - Los servicios no tienen dependencia de ASP.NET Core (sin <c>IActionResult</c>),
///   lo que los hace más fáciles de testear unitariamente.
///
/// <b>Uso típico en un controller:</b>
/// <code>
/// var result = await _service.CreateAsync(request);
/// if (result.IsFailure) return BadRequest(result.Error);
/// return CreatedAtAction(..., result.Value);
/// </code>
/// </remarks>
public class Result
{
    /// <summary><c>true</c> si la operación completó sin errores de negocio.</summary>
    public bool IsSuccess { get; }

    /// <summary>Mensaje de error en español. Solo tiene valor cuando <see cref="IsFailure"/> es <c>true</c>.</summary>
    public string? Error { get; }

    /// <summary>Inverso de <see cref="IsSuccess"/>. Conveniente para condicionales.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// <c>true</c> cuando el fallo se debe a un recurso no encontrado (HTTP 404).
    /// Permite que los controllers elijan el status code sin comparar strings de error.
    /// </summary>
    public bool IsNotFound { get; private init; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error     = error;
    }

    /// <summary>Crea un resultado exitoso sin valor de retorno.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Crea un resultado fallido con el mensaje de error de negocio.</summary>
    /// <param name="notFound">Marca el fallo como "recurso no encontrado" para mapeo HTTP 404.</param>
    public static Result Failure(string error, bool notFound = false) =>
        new(false, error) { IsNotFound = notFound };

    /// <summary>Crea un resultado exitoso con un valor de retorno tipado.</summary>
    public static Result<T> Success<T>(T value) => new(value, true, null);

    /// <summary>Crea un resultado fallido tipado (el valor será <c>null</c>/<c>default</c>).</summary>
    /// <param name="notFound">Marca el fallo como "recurso no encontrado" para mapeo HTTP 404.</param>
    public static Result<T> Failure<T>(string error, bool notFound = false) =>
        new(default, false, error) { IsNotFound = notFound };
}

/// <summary>
/// Versión genérica de <see cref="Result"/> que lleva un valor de retorno en caso de éxito.
/// </summary>
/// <typeparam name="T">Tipo del valor retornado cuando la operación es exitosa.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Valor del resultado. Solo acceder cuando <see cref="Result.IsSuccess"/> es <c>true</c>;
    /// será <c>null</c> o <c>default(T)</c> en caso de fallo.
    /// </summary>
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error)
    {
        Value = value;
    }
}
