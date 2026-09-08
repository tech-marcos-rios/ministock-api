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
    /// Categoría del error de negocio. Permite que la capa Api elija el status code HTTP
    /// (vía <c>ResultExtensions.ToActionResult</c>) sin comparar strings de error ni repetir
    /// el mismo if/else en cada controller.
    /// </summary>
    public ErrorType Type { get; private init; }

    protected Result(bool isSuccess, string? error, ErrorType type)
    {
        IsSuccess = isSuccess;
        Error     = error;
        Type      = type;
    }

    /// <summary>Crea un resultado exitoso sin valor de retorno.</summary>
    public static Result Success() => new(true, null, ErrorType.Validation);

    /// <summary>Crea un resultado fallido con el mensaje de error de negocio.</summary>
    /// <param name="type">Categoría del error, usada para elegir el status code HTTP.</param>
    public static Result Failure(string error, ErrorType type = ErrorType.Validation) =>
        new(false, error, type);

    /// <summary>Crea un resultado exitoso con un valor de retorno tipado.</summary>
    public static Result<T> Success<T>(T value) => new(value, true, null, ErrorType.Validation);

    /// <summary>Crea un resultado fallido tipado (el valor será <c>null</c>/<c>default</c>).</summary>
    /// <param name="type">Categoría del error, usada para elegir el status code HTTP.</param>
    public static Result<T> Failure<T>(string error, ErrorType type = ErrorType.Validation) =>
        new(default, false, error, type);
}

/// <summary>
/// Categoría de un error de negocio devuelto por un <see cref="Result"/> fallido.
/// Determina el status code HTTP que le corresponde en la capa Api.
/// </summary>
public enum ErrorType
{
    /// <summary>Datos inválidos o regla de negocio violada de forma genérica → HTTP 400.</summary>
    Validation,

    /// <summary>El recurso solicitado no existe → HTTP 404.</summary>
    NotFound,

    /// <summary>El recurso ya existe / conflicto de estado → HTTP 409.</summary>
    Conflict,

    /// <summary>Credenciales inválidas o token expirado → HTTP 401.</summary>
    Unauthorized
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

    internal Result(T? value, bool isSuccess, string? error, ErrorType type) : base(isSuccess, error, type)
    {
        Value = value;
    }
}
