namespace MiniStock.Application.Common;

/// <summary>
/// Envuelve una página de resultados junto con los metadatos de paginación.
/// </summary>
/// <remarks>
/// <b>Por qué no devolver solo una lista:</b> el cliente necesita saber el total de
/// registros para renderizar el paginador. Devolver la lista sola obliga a hacer
/// una segunda llamada para el count, o a exponer endpoints separados.
///
/// <b>Propiedades calculadas</b>: <see cref="TotalPages"/>, <see cref="HasPreviousPage"/>
/// y <see cref="HasNextPage"/> se computan en memoria — no van a la BD — para
/// simplificar la lógica de paginación en el frontend.
/// </remarks>
/// <typeparam name="T">Tipo de los ítems de la página.</typeparam>
public class PagedResult<T>
{
    /// <summary>Ítems correspondientes a la página solicitada.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total de registros en la colección completa (no solo en esta página).</summary>
    public int TotalCount { get; }

    /// <summary>Número de página actual (base 1).</summary>
    public int Page { get; }

    /// <summary>Cantidad de ítems por página.</summary>
    public int PageSize { get; }

    /// <summary>Total de páginas calculado a partir de <see cref="TotalCount"/> y <see cref="PageSize"/>.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary><c>true</c> si existe una página anterior a la actual.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary><c>true</c> si existe al menos una página posterior a la actual.</summary>
    public bool HasNextPage => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items      = items;
        TotalCount = totalCount;
        Page       = page;
        PageSize   = pageSize;
    }
}
